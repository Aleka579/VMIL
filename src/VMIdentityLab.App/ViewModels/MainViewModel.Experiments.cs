using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using VMIdentityLab.App.Infrastructure;
using VMIdentityLab.Core.Experiments;
using VMIdentityLab.VMware;
using VMIdentityLab.VMware.Configuration;
using VMIdentityLab.Windows;
using Localization = VMIdentityLab.App.Infrastructure.Localization;

namespace VMIdentityLab.App.ViewModels;

public sealed partial class MainViewModel
{
    private IdentityExperimentPlan? experimentPreview;
    private int selectedExperimentIndex;
    private bool experimentReviewed;
    private string experimentNoticeKey = "Experiment.Empty";

    public ICommand PreviewExperimentCommand { get; }
    public ICommand ExportExperimentCommand { get; }
    public ObservableCollection<ConfigDiffRow> ExperimentChanges { get; } = [];
    public bool CanChooseExperiment => !IsBusy;
    public int SelectedExperimentIndex
    {
        get => selectedExperimentIndex;
        set
        {
            if (selectedExperimentIndex == value) return;
            selectedExperimentIndex = value;
            InvalidateExperimentPreview();
            Notify();
        }
    }
    public bool CanReviewExperiment => !IsBusy && experimentPreview is not null;
    public bool ExperimentReviewed
    {
        get => experimentReviewed;
        set
        {
            experimentReviewed = value && experimentPreview is not null;
            Notify();
            CommandManager.InvalidateRequerySuggested();
        }
    }
    public string ExperimentNotice => Localization.Text(experimentNoticeKey);
    public string ExperimentTarget => experimentPreview is null ? "—"
        : $"VMware Workstation {experimentPreview.TargetVersion} · HW {experimentPreview.HardwareVersion?.ToString() ?? "?"}\n"
          + Localization.Text("Experiment.ResearchBaseline") + IdentityExperimentCatalog.ResearchBaseline + "\n"
          + Localization.Text("Experiment.StateSnapshot") + experimentPreview.PowerState + " · "
          + experimentPreview.PowerObservedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz");
    public string ExperimentBasis => experimentPreview is null ? "—"
        : experimentPreview.Definition.EvidenceScope.Get(Localization.English) + "\n" + experimentPreview.Definition.OfficialReference
          + "\n" + Localization.Text("Experiment.EvidenceDate") + IdentityExperimentCatalog.EvidenceReviewedOn;
    public string ExperimentConstraints => experimentPreview?.Definition.ValueConstraints.Get(Localization.English) ?? "—";
    public string ExperimentRisk => experimentPreview?.Definition.Risk.Get(Localization.English) ?? "—";
    public string ExperimentRecovery => experimentPreview?.Definition.Recovery.Get(Localization.English) ?? "—";
    public string ExperimentGaps => experimentPreview is null ? "—"
        : string.Join("\n", experimentPreview.Gaps.Select(gap => "• " + IdentityExperimentCatalog.Describe(gap).Get(Localization.English)));

    private void InvalidateExperimentPreview(bool stale = false)
    {
        experimentPreview = null;
        experimentNoticeKey = stale ? "Experiment.Stale" : "Experiment.Empty";
        ExperimentReviewed = false;
        RefreshExperimentPreview();
    }

    private void RefreshExperimentPreview()
    {
        if (experimentPreview is not null && (currentVmx is null || currentVmxPath is null
            || !experimentPreview.IsCurrent(currentVmxPath, currentVmx.Document,
                WorkstationEnvironment.ReadInstalledVersion(), currentVmx.PowerState, (IdentityExperimentKind)SelectedExperimentIndex)))
        {
            InvalidateExperimentPreview(stale: true);
            return;
        }
        ExperimentChanges.Clear();
        if (experimentPreview is { } plan)
            ExperimentChanges.Add(new(plan.Definition.Title.Get(Localization.English),
                Localization.Text("Declaration." + plan.Before), plan.Definition.Intent.Get(Localization.English),
                plan.Definition.Risk.Get(Localization.English)));
        foreach (var name in new[] { nameof(ExperimentNotice), nameof(ExperimentTarget), nameof(ExperimentBasis),
            nameof(ExperimentConstraints), nameof(ExperimentRisk), nameof(ExperimentRecovery), nameof(ExperimentGaps), nameof(CanReviewExperiment) })
            Notify(name);
        CommandManager.InvalidateRequerySuggested();
    }

    private async Task<bool> PreviewExperimentAsync(CancellationToken token)
    {
        InvalidateExperimentPreview();
        var path = currentVmxPath!;
        var read = await Task.Run(() => new VmxReader().ReadAsync(path, token), token);
        // A file re-read does not refresh the power observation. M11 would need a new provider query.
        var power = currentVmx!.PowerState;
        if (read.Document.ContentHash != currentVmx.Document.ContentHash) InvalidateConfigPreview();
        currentVmx = read with { PowerState = power };
        if (currentReport is not null) currentReport = currentReport with { VmxSummary = currentVmx.Document.ToSummary(power) };
        experimentPreview = IdentityExperimentPlanner.Create(path, read.Document,
            WorkstationEnvironment.ReadInstalledVersion(), power, (IdentityExperimentKind)SelectedExperimentIndex);
        experimentNoticeKey = "Experiment.NotExecuted";
        Refresh();
        return true;
    }

    private async Task<bool> ExportExperimentAsync(CancellationToken token)
    {
        var plan = experimentPreview!;
        var dialog = new SaveFileDialog
        {
            Filter = "Identity plan report (*.json)|*.json", DefaultExt = ".json", AddExtension = true,
            OverwritePrompt = false, FileName = "VMIdentityLab-identity-plan.json"
        };
        if (dialog.ShowDialog(Application.Current.MainWindow) != true) return false;
        var read = await Task.Run(() => new VmxReader().ReadAsync(currentVmxPath!, token), token);
        if (!plan.IsCurrent(currentVmxPath!, read.Document, WorkstationEnvironment.ReadInstalledVersion(),
            currentVmx!.PowerState, (IdentityExperimentKind)SelectedExperimentIndex))
        {
            currentVmx = read;
            InvalidateConfigPreview();
            if (currentReport is not null) currentReport = currentReport with { VmxSummary = read.Document.ToSummary(read.PowerState) };
            InvalidateExperimentPreview(stale: true);
            Refresh();
            return false;
        }
        await plan.SaveReportAsync(dialog.FileName, token);
        return true;
    }
}
