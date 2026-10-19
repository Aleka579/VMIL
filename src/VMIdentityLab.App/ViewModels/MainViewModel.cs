using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using VMIdentityLab.App.Infrastructure;
using VMIdentityLab.Core.Comparison;
using VMIdentityLab.Core.Reports;
using VMIdentityLab.VMware;
using VMIdentityLab.VMware.Configuration;
using VMIdentityLab.Windows;
using Localization = VMIdentityLab.App.Infrastructure.Localization;

namespace VMIdentityLab.App.ViewModels;

public sealed record FindingRow(string Category, string Name, string Value, string Status, string Source, string Explanation);
public sealed record IdentityRow(string Name, string Declaration, string Observation, string Status,
    string Support, string Source, string Time, string Basis);
public sealed record VmxRow(string Key, string Value, string Support);
public sealed record DifferenceRow(string Name, string Left, string Right, string Result, string Reason);
public sealed record ConfigDiffRow(string Key, string Before, string After, string Impact);
public sealed record BackupHistoryRow(VmxBackupHistoryEntry Entry) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Time => Entry.RecordedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    public string Target => Path.GetFileName(Entry.Receipt.TargetPath);
    public string Operation => Localization.Text(Entry.Operation == "restore" ? "History.Restore" : "History.Apply");
    public string Backup => Entry.Receipt.BackupPath;
    internal void RefreshLanguage() => PropertyChanged?.Invoke(this, new(nameof(Operation)));
}

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly WindowsCollector collector = new();
    private CancellationTokenSource? operationCancellation;
    private DiagnosticReport? currentReport;
    private DiagnosticReport? rightReport;
    private VmxReadResult? currentVmx;
    private string? currentVmxPath;
    private int selectedPage;
    private bool isBusy;
    private bool includeIdentifiers;
    private IdentityRow? selectedIdentity;
    private string statusText = Localization.Text("Status.Ready");
    private string requestedMemory = "";
    private string requestedCpu = "";
    private string configNoticeKey = "Config.Pending";
    private VmxConfigPlan? configPlan;
    private VmxPreparedChange? configPreview;
    private bool restoring;
    private BackupHistoryRow? selectedBackup;
    private VmxBackupHistoryEntry? unsavedHistory;
    private string historyNoticeKey = "History.Empty";
    private string recoveryLocation = "";
    private string? configurationOutcome;
    private readonly IVmxBackupHistory backupHistory = new LocalVmxBackupHistory(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VMIdentityLab", "configuration-history"));

    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<FindingRow> Findings { get; } = [];
    public ObservableCollection<IdentityRow> IdentityRows { get; } = [];
    public ObservableCollection<VmxRow> VmxRows { get; } = [];
    public ObservableCollection<DifferenceRow> Differences { get; } = [];
    public ObservableCollection<ConfigDiffRow> ConfigChanges { get; } = [];
    public ObservableCollection<BackupHistoryRow> BackupHistory { get; } = [];
    public ICommand CollectCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand OpenVmxCommand { get; }
    public ICommand ReadPowerCommand { get; }
    public ICommand PreviewConfigCommand { get; }
    public ICommand ReviewConfigCommand { get; }
    public ICommand LoadHistoryCommand { get; }
    public ICommand PreviewRestoreCommand { get; }
    public ICommand SaveHistoryCommand { get; }
    public ICommand ImportLeftCommand { get; }
    public ICommand ImportRightCommand { get; }
    public ICommand ExportJsonCommand { get; }
    public ICommand ExportHtmlCommand { get; }

    public int SelectedPage { get => selectedPage; set { selectedPage = value; Notify(); Notify(nameof(PageTitle)); } }
    public bool IsBusy { get => isBusy; private set { isBusy = value; Notify(); Notify(nameof(CanChooseIdentifiers)); CommandManager.InvalidateRequerySuggested(); } }
    public bool CanChooseIdentifiers => !IsBusy;
    public bool IncludeIdentifiers { get => includeIdentifiers; set { includeIdentifiers = value; Notify(); } }
    public IdentityRow? SelectedIdentity { get => selectedIdentity; set { selectedIdentity = value; Notify(); } }
    public string StatusText { get => statusText; private set { statusText = value; Notify(); } }
    public string PageTitle => Localization.Text(new[] { "Nav.Overview", "Nav.Detection", "Nav.Configuration", "Nav.Comparison", "Nav.Reports" }[Math.Clamp(SelectedPage, 0, 4)]);
    public string OsText => currentReport?.System.Os is { Length: > 0 } text ? text : Localization.Text("State.NoReport");
    public string BuildText => currentReport is null ? "—" : $"Build {currentReport.System.Build} · {currentReport.System.Architecture}";
    public string CpuText => currentReport?.Findings.FirstOrDefault(item => item.Id == "cpu.model")?.Value.ToString() ?? "—";
    public string EvidenceText => Localization.Text("Evidence." + (currentReport?.VirtualizationEvidence.VmwareGuestEvidence ?? "inconclusive"));
    public string HypervisorText => currentReport?.VirtualizationEvidence.HypervisorDetected switch
    {
        true => Localization.Text("State.Detected"), false => Localization.Text("State.NotObserved"), _ => Localization.Text("State.Unknown")
    };
    public string CountsText => currentReport is null ? "—" : $"{currentReport.Findings.Count(item => item.Status == CollectionStatus.Success)} / {currentReport.Findings.Count}";
    public string TimeText => currentReport?.CollectedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz") ?? "—";
    public string LeftText => currentReport is null ? Localization.Text("State.NoReport") : $"{currentReport.System.Os} · {currentReport.CollectedAt.ToLocalTime():HH:mm:ss}";
    public string RightText => rightReport is null ? Localization.Text("State.NoReport") : $"{rightReport.System.Os} · {rightReport.CollectedAt.ToLocalTime():HH:mm:ss}";
    public string VmxStateText => Localization.Text("Power." + (currentVmx?.Document.ToSummary(currentVmx.PowerState).PowerState ?? "unknown"));
    public string VmxNotice => Localization.Text(currentVmx is null ? "Configuration.Empty"
        : !currentVmx.Document.Entries.Any() ? "Configuration.NoEntries"
        : currentVmx.PowerState.State == VmPowerState.Unknown ? "Configuration.StateUnverified" : "Configuration.StateRead");
    public string ReportDetails => currentReport is null ? Localization.Text("State.NoReport")
        : $"schema {currentReport.SchemaVersion} · {currentReport.AppVersion} · redacted · {TimeText}";
    public string DifferenceCount => Differences.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public string ApplicationVersion => VMIdentityLab.Core.BuildInfo.Version;
    public string RequestedMemory { get => requestedMemory; set { requestedMemory = value; InvalidateConfigPreview(); Notify(); } }
    public string RequestedCpu { get => requestedCpu; set { requestedCpu = value; InvalidateConfigPreview(); Notify(); } }
    public string ConfigNotice => Localization.Text(configNoticeKey);
    public string HistoryNotice => Localization.Text(historyNoticeKey);
    public string UnsavedHistoryWarning => unsavedHistory is null ? "" : Localization.Text("Config.HistoryNotSaved");
    public string RecoveryLocation { get => recoveryLocation; private set { recoveryLocation = value; Notify(); } }
    public BackupHistoryRow? SelectedBackup
    {
        get => selectedBackup;
        set { selectedBackup = value; if (restoring) InvalidateConfigPreview(); Notify(); CommandManager.InvalidateRequerySuggested(); }
    }

    public MainViewModel()
    {
        CollectCommand = new AsyncCommand(() => RunOperationAsync(async token =>
        {
            var progress = new Progress<CollectionProgress>(step => StatusText = $"{Localization.Text("Status.Collecting")} {step.Completed}/{step.Total}");
            var readIdentifiers = IncludeIdentifiers;
            currentReport = await Task.Run(() => collector.CollectAsync(token, progress, readIdentifiers), token);
            if (currentVmx is not null) currentReport = currentReport with { VmxSummary = currentVmx.Document.ToSummary(currentVmx.PowerState) };
            Refresh();
            return true;
        }), () => !IsBusy);
        CancelCommand = new RelayCommand(() => operationCancellation?.Cancel(), () => IsBusy);
        OpenVmxCommand = new AsyncCommand(() => RunOperationAsync(OpenVmxAsync), () => !IsBusy);
        ReadPowerCommand = new AsyncCommand(() => RunOperationAsync(ReadPowerAsync), () => !IsBusy && currentVmxPath is not null);
        PreviewConfigCommand = new AsyncCommand(RunPreviewAsync, () => !IsBusy && currentVmx is not null);
        ReviewConfigCommand = new AsyncCommand(() => RunConfigOperationAsync(ReviewConfigAsync),
            () => !IsBusy && configPreview is not null && unsavedHistory is null);
        LoadHistoryCommand = new AsyncCommand(() => RunOperationAsync(LoadHistoryAsync), () => !IsBusy && currentVmxPath is not null);
        PreviewRestoreCommand = new AsyncCommand(() => RunConfigOperationAsync(PreviewRestoreAsync),
            () => !IsBusy && currentVmxPath is not null && SelectedBackup is not null);
        SaveHistoryCommand = new AsyncCommand(() => RunConfigOperationAsync(SaveHistoryAsync), () => !IsBusy && unsavedHistory is not null);
        ImportLeftCommand = new AsyncCommand(() => RunOperationAsync(token => ImportAsync(false, token)), () => !IsBusy);
        ImportRightCommand = new AsyncCommand(() => RunOperationAsync(token => ImportAsync(true, token)), () => !IsBusy);
        ExportJsonCommand = new AsyncCommand(() => RunOperationAsync(token => ExportAsync(false, token)), () => !IsBusy && currentReport is not null);
        ExportHtmlCommand = new AsyncCommand(() => RunOperationAsync(token => ExportAsync(true, token)), () => !IsBusy && currentReport is not null);
        Refresh();
    }

    private async Task RunOperationAsync(Func<CancellationToken, Task<bool>> operation)
    {
        IsBusy = true;
        operationCancellation = new();
        StatusText = Localization.Text("Status.Working");
        try { StatusText = Localization.Text(await operation(operationCancellation.Token) ? "Status.Completed" : "Status.Cancelled"); }
        catch (OperationCanceledException) { StatusText = Localization.Text("Status.Cancelled"); }
        catch (ReportFormatException) { StatusText = Localization.Text("Error.Report"); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        { StatusText = Localization.Text("Error.File"); DiagnosticLog.Write("ui", "failed", error.GetType().Name); }
        catch (Exception error)
        { StatusText = Localization.Text("Error.General"); DiagnosticLog.Write("ui", "failed", error.GetType().Name); }
        finally { operationCancellation.Dispose(); operationCancellation = null; IsBusy = false; }
    }

    private async Task<bool> OpenVmxAsync(CancellationToken token)
    {
        var dialog = new OpenFileDialog { Filter = "VMware configuration (*.vmx)|*.vmx", CheckFileExists = true, Multiselect = false };
        if (dialog.ShowDialog(Application.Current.MainWindow) != true) return false;
        currentVmx = await Task.Run(() => new VmxReader().ReadAsync(dialog.FileName, token), token);
        currentVmxPath = dialog.FileName;
        if (unsavedHistory is null) RecoveryLocation = "";
        requestedMemory = currentVmx.Document.Entries.FirstOrDefault(item => item.Key.Equals("memsize", StringComparison.OrdinalIgnoreCase))?.Value ?? "";
        requestedCpu = currentVmx.Document.Entries.FirstOrDefault(item => item.Key.Equals("numvcpus", StringComparison.OrdinalIgnoreCase))?.Value ?? "";
        InvalidateConfigPreview();
        if (currentReport is not null) currentReport = currentReport with { VmxSummary = currentVmx.Document.ToSummary(currentVmx.PowerState) };
        SelectedPage = 2;
        await LoadHistoryAsync(token);
        Refresh();
        return true;
    }

    private async Task<bool> ReadPowerAsync(CancellationToken token)
    {
        var dialog = new VmwareConnectionDialog(Application.Current.MainWindow);
        if (dialog.ShowDialog() != true) return false;
        using var client = CreateVmwareClient(dialog);
        currentVmx = await new VmxReader(new WorkstationRestPowerStateProbe(client)).ReadAsync(currentVmxPath!, token);
        InvalidateConfigPreview();
        if (currentReport is not null) currentReport = currentReport with { VmxSummary = currentVmx.Document.ToSummary(currentVmx.PowerState) };
        Refresh();
        return true;
    }

    private async Task RunPreviewAsync()
    {
        await RunOperationAsync(PreviewConfigAsync);
        if (configNoticeKey is "Config.Invalid" or "Config.NoChange") StatusText = ConfigNotice;
    }

    private async Task<bool> PreviewConfigAsync(CancellationToken token)
    {
        InvalidateConfigPreview();
        if (currentVmxPath is not null)
        {
            currentVmx = await Task.Run(() => new VmxReader().ReadAsync(currentVmxPath, token), token);
            if (currentReport is not null) currentReport = currentReport with { VmxSummary = currentVmx.Document.ToSummary(currentVmx.PowerState) };
        }
        var document = currentVmx!.Document;
        var requested = CandidateRequest(document);
        configPlan = null;
        ConfigChanges.Clear();
        if (requested.Count == 0)
        {
            configNoticeKey = "Config.NoChange";
            Notify(nameof(ConfigNotice));
            return false;
        }
        try
        {
            // 0.0 represents unknown installed metadata; it never implies write approval.
            var version = WorkstationEnvironment.ReadInstalledVersion() ?? new Version(0, 0);
            var prepared = await Task.Run(() => new VmxConfigManager().PrepareAsync(currentVmxPath!, version, requested, token), token);
            if (prepared.Plan.OriginalHash != document.ContentHash)
                throw new InvalidDataException("Configuration changed while preparing the preview.");
            token.ThrowIfCancellationRequested();
            var plan = prepared.Plan;
            configPreview = prepared;
            configPlan = plan;
            configNoticeKey = plan.RequiresTopologyReview ? "Config.TopologyPending" : "Config.Pending";
            if (VmxApiConfigManager.HasPendingOperation(prepared.TargetPath))
            {
                configNoticeKey = "Config.ApiUnknown";
                RecoveryLocation = VmxApiConfigManager.PendingOperationPath(prepared.TargetPath);
            }
        }
        catch (Exception error) when (error is ArgumentException or InvalidDataException)
        {
            configNoticeKey = "Config.Invalid";
            RefreshConfigPreview();
            return false;
        }
        Refresh();
        return true;
    }

    private Dictionary<string, string> CandidateRequest(VmxDocument document)
    {
        var requested = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in new[] { ("memsize", requestedMemory), ("numvcpus", requestedCpu) })
            if (value.Length > 0 && document.Entries.FirstOrDefault(item => item.Key.Equals(key, StringComparison.OrdinalIgnoreCase))?.Value != value)
                requested.Add(key, value);
        return requested;
    }

    private void InvalidateConfigPreview()
    {
        configPlan = null;
        configPreview = null;
        restoring = false;
        var pending = currentVmxPath is not null && VmxApiConfigManager.HasPendingOperation(currentVmxPath);
        configNoticeKey = pending ? "Config.ApiUnknown" : "Config.Pending";
        if (pending && RecoveryLocation.Length == 0) RecoveryLocation = VmxApiConfigManager.PendingOperationPath(currentVmxPath!);
        RefreshConfigPreview();
    }

    private void RefreshConfigPreview()
    {
        ConfigChanges.Clear();
        if (configPlan is not null)
            foreach (var change in configPlan.Changes)
                ConfigChanges.Add(new(change.Key, change.Before + (change.Unit == "MiB" ? " MiB" : ""), change.After + (change.Unit == "MiB" ? " MiB" : ""),
                    Localization.Text(change.Key == "memsize" ? "Config.MemoryImpact" : "Config.CpuImpact")));
        Notify(nameof(ConfigNotice));
        CommandManager.InvalidateRequerySuggested();
    }

    private async Task RunConfigOperationAsync(Func<CancellationToken, Task<bool>> operation)
    {
        configurationOutcome = null;
        await RunOperationAsync(operation);
        if (configurationOutcome is not null) StatusText = Localization.Text(configurationOutcome);
    }

    private async Task<bool> LoadHistoryAsync(CancellationToken token)
    {
        var result = await Task.Run(() => backupHistory.ReadAsync(token), token);
        BackupHistory.Clear();
        SelectedBackup = null;
        foreach (var entry in result.Entries.Where(entry => string.Equals(entry.Receipt.TargetPath,
            Path.GetFullPath(currentVmxPath!), StringComparison.OrdinalIgnoreCase))) BackupHistory.Add(new(entry));
        historyNoticeKey = result.UnreadableFiles > 0 ? "History.Partial" : BackupHistory.Count > 0 ? "History.Ready" : "History.Empty";
        Notify(nameof(HistoryNotice));
        return true;
    }

    private async Task<bool> PreviewRestoreAsync(CancellationToken token)
    {
        var entry = SelectedBackup!.Entry;
        InvalidateConfigPreview();
        configPreview = entry.Receipt.Backend == VmxApiConfigManager.BackendName
            ? await VmxApiConfigManager.PrepareRestoreAsync(currentVmxPath!, entry.Receipt, token)
            : await new VmxConfigManager().PrepareRestoreAsync(currentVmxPath!, entry.Receipt, token);
        configPlan = configPreview.Plan;
        restoring = true;
        configNoticeKey = "Config.RestorePreview";
        RefreshConfigPreview();
        return true;
    }

    private async Task<bool> ReviewConfigAsync(CancellationToken token)
    {
        var preview = configPreview!;
        var isRestore = restoring;
        var approved = VmxApiConfigManager.IsProductionWriteApproved(preview)
            && WorkstationEnvironment.ReadInstalledVersion() == preview.WorkstationVersion
            && !VmxApiConfigManager.HasPendingOperation(preview.TargetPath);
        var confirmation = new ConfigConfirmationDialog(Application.Current.MainWindow, preview, isRestore, approved);
        if (confirmation.ShowDialog() != true)
        {
            if (!approved) configurationOutcome = "Config.ReviewOnly";
            return false;
        }
        if (WorkstationEnvironment.ReadInstalledVersion() != preview.WorkstationVersion)
            throw new InvalidOperationException("Installed version changed; create a new preview.");
        var connection = new VmwareConnectionDialog(Application.Current.MainWindow, forConfiguration: true);
        if (connection.ShowDialog() != true) return false;
        using var client = CreateVmwareClient(connection);
        var workflow = new VmxConfigWorkflow(new VmxApiConfigManager(new WorkstationRestClient(client),
            WorkstationEnvironment.ReadInstalledVersion), backupHistory);
        try
        {
            var entry = await workflow.ApplyAsync(preview, preview.Id, isRestore, token);
            RecoveryLocation = entry.Receipt.BackupPath;
            InvalidateConfigPreview();
            await LoadHistoryAsync(CancellationToken.None);
            configurationOutcome = "Config.Applied";
        }
        catch (VmxHistoryPersistenceException error)
        {
            unsavedHistory = error.Entry;
            RecoveryLocation = error.Entry.Receipt.BackupPath;
            InvalidateConfigPreview();
            configurationOutcome = "Config.HistoryNotSaved";
        }
        catch (VmxApiBusyException)
        {
            InvalidateConfigPreview();
            configurationOutcome = "Config.ApiBusy";
        }
        catch (VmxApiTransactionException error)
        {
            DiagnosticLog.Write("configuration", error.OutcomeUnknown ? "unresolved" : "stopped",
                error.InnerException is WorkstationApiException rejected
                    ? $"vmrest.http.{(int)rejected.StatusCode}" : "configuration.stopped");
            RecoveryLocation = string.Join(Environment.NewLine,
                new[] { error.BackupPath, error.ProposedPath, error.JournalPath }.Where(path => !string.IsNullOrWhiteSpace(path)));
            InvalidateConfigPreview();
            configurationOutcome = error.OutcomeUnknown ? "Config.ApiUnknown" : "Config.ApiStopped";
        }
        catch (VmxTransactionException error)
        {
            RecoveryLocation = string.Join(Environment.NewLine,
                new[] { error.BackupPath, error.TemporaryPath, error.ReplacedBackupPath }.Where(path => path is not null));
            InvalidateConfigPreview();
            configurationOutcome = "Config.Stopped";
        }
        try
        {
            currentVmx = await new VmxReader().ReadAsync(currentVmxPath!, CancellationToken.None);
            if (currentReport is not null) currentReport = currentReport with { VmxSummary = currentVmx.Document.ToSummary(currentVmx.PowerState) };
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException)
        { configurationOutcome = "Config.RecheckFailed"; }
        Refresh();
        return true;
    }

    private async Task<bool> SaveHistoryAsync(CancellationToken token)
    {
        await backupHistory.SaveAsync(unsavedHistory!, token);
        unsavedHistory = null;
        await LoadHistoryAsync(token);
        configurationOutcome = "History.Saved";
        Notify(nameof(UnsavedHistoryWarning));
        return true;
    }

    private static HttpClient CreateVmwareClient(VmwareConnectionDialog dialog)
    {
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false })
        {
            BaseAddress = new Uri($"http://127.0.0.1:{dialog.Port}/"), Timeout = TimeSpan.FromSeconds(5),
            MaxResponseContentBufferSize = VmxReader.MaxVmxBytes
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(dialog.UserName + ":" + dialog.TakePassword())));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.vmware.vmw.rest-v1+json"));
        return client;
    }

    private async Task<bool> ImportAsync(bool right, CancellationToken token)
    {
        var dialog = new OpenFileDialog { Filter = "JSON report (*.json)|*.json", CheckFileExists = true, Multiselect = false };
        if (dialog.ShowDialog(Application.Current.MainWindow) != true) return false;
        var report = await Task.Run(() => ReportJson.ReadAsync(dialog.FileName, token), token);
        if (right) rightReport = report; else currentReport = report;
        SelectedPage = right ? 3 : 1;
        Refresh();
        return true;
    }

    private async Task<bool> ExportAsync(bool html, CancellationToken token)
    {
        var dialog = new SaveFileDialog
        {
            Filter = html ? "HTML report (*.html)|*.html" : "JSON report (*.json)|*.json",
            DefaultExt = html ? ".html" : ".json", AddExtension = true, OverwritePrompt = false,
            FileName = "VMIdentityLab-report" + (html ? ".html" : ".json")
        };
        if (dialog.ShowDialog(Application.Current.MainWindow) != true) return false;
        var confirmedOverwrite = false;
        if (File.Exists(dialog.FileName))
        {
            confirmedOverwrite = MessageBox.Show(Application.Current.MainWindow,
                Localization.Text("Reports.Replace") + "\n" + dialog.FileName, "VM Identity Lab",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
            if (!confirmedOverwrite) return false;
        }
        var report = currentReport!;
        await Task.Run(() => html ? ReportFiles.SaveHtmlAsync(dialog.FileName, report, confirmedOverwrite, token)
            : ReportFiles.SaveJsonAsync(dialog.FileName, report, confirmedOverwrite, token), token);
        return true;
    }

    public void ChangeLanguage(bool english)
    {
        Localization.Set(english);
        StatusText = Localization.Text("Status.Ready");
        Refresh();
    }

    private void Refresh()
    {
        RefreshConfigPreview();
        foreach (var row in BackupHistory) row.RefreshLanguage();
        Findings.Clear();
        if (currentReport is not null)
            foreach (var item in currentReport.Findings)
                Findings.Add(new(Localization.Text("Category." + item.Category), FindingCatalog.Title(item.Id, Localization.English),
                    DisplayValue(item.Value), Localization.Text("Collection." + item.Status), item.Source, item.Explanation));
        IdentityRows.Clear();
        var declarations = currentVmx is null ? null : VmxIdentityReader.Read(currentVmx.Document);
        foreach (var item in IdentityAnalyzer.Analyze(currentReport, declarations))
        {
            var observation = item.Observation;
            var source = observation?.Source ?? Localization.Text("Identity.ExpectedSource") + item.Definition.Source;
            if (source is "identifier" or "unverified") source += " · " + Localization.Text("Identity.UnverifiedSource");
            if (item.Declaration.Source.Length != 0) source += " | " + item.Declaration.Source;
            IdentityRows.Add(new(FindingCatalog.Title(item.Definition.Id, Localization.English),
                item.Declaration.State == DeclarationState.Present ? DisplayValue(item.Declaration.Value)
                    : Localization.Text("Declaration." + item.Declaration.State),
                observation is null ? Localization.Text("Identity.NotCollected") : DisplayValue(observation.Value),
                observation is null ? Localization.Text("Identity.NotCollected") : Localization.Text("Collection." + observation.Status),
                Localization.Text("Identity.Support." + item.WriteSupport), source,
                observation?.ObservedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz") ?? "—",
                Localization.Text("Identity.Basis." + item.Basis)));
        }
        SelectedIdentity = IdentityRows.FirstOrDefault();
        VmxRows.Clear();
        if (currentVmx is not null)
        {
            var summary = currentVmx.Document.ToSummary(currentVmx.PowerState);
            var version = WorkstationEnvironment.ReadInstalledVersion();
            foreach (var item in currentVmx.Document.Entries)
            {
                var evidence = VmxSupportCatalog.Analyze(currentVmx.Document, item, version);
                var label = evidence.Status switch
                {
                    VmxSupportStatus.Confirmed => "Configuration.Confirmed",
                    VmxSupportStatus.Unsupported => "Configuration.Unsupported",
                    VmxSupportStatus.Ambiguous => "Configuration.Ambiguous",
                    _ => "Configuration.Unverified"
                };
                var support = Localization.Text(label);
                if (evidence.Source.Length != 0) support += " · Workstation 17.5.2 · KB315655";
                VmxRows.Add(new(item.Key, summary.Declarations.GetValueOrDefault(item.Key, Localization.Text("State.Redacted")),
                    support));
            }
        }
        Differences.Clear();
        if (currentReport is not null && rightReport is not null)
            foreach (var item in ReportComparer.Compare(currentReport, rightReport))
                Differences.Add(new(FindingCatalog.Title(item.Id, Localization.English), item.Left is null ? "—" : DisplayValue(item.Left.Value), item.Right is null ? "—" : DisplayValue(item.Right.Value),
                    Localization.Text("Comparison." + item.Kind), Localization.Text("Reason." + item.Reason)));
        Notify("");
        CommandManager.InvalidateRequerySuggested();
    }

    internal void LoadSyntheticPreview()
    {
        Finding F(string id, FindingValue value) => new()
        {
            Id = id, Category = FindingCatalog.ById[id].Category, Source = FindingCatalog.ById[id].Source,
            ObservedAt = DateTimeOffset.Parse("2026-10-09T12:00:00+08:00", System.Globalization.CultureInfo.InvariantCulture), Status = CollectionStatus.Success, Value = value
        };
        currentReport = ReportPrivacy.Apply(new()
        {
            CollectedAt = DateTimeOffset.Parse("2026-10-09T12:00:00+08:00", System.Globalization.CultureInfo.InvariantCulture),
            System = new() { Os = "Windows 11 · SYNTHETIC", Build = "00000", Architecture = "X64" },
            Findings = [F("smbios.manufacturer", FindingValue.FromText("Synthetic OEM")), F("cpu.model", FindingValue.FromText("Synthetic 8-core processor")),
                F("cpu.logicalProcessors", FindingValue.FromNumber(16, ValueUnit.Count)), F("cpu.hypervisorPresent", FindingValue.FromBoolean(true)),
                F("cpu.hypervisorVendor", FindingValue.FromText("Microsoft Hv")), F("pnp.vmwareDeviceCount", FindingValue.FromNumber(0, ValueUnit.Count)),
                F("system.memoryBytes", FindingValue.FromNumber(17179869184m, ValueUnit.Bytes)), F("tools.installed", FindingValue.FromBoolean(false)),
                F("smbios.uuid", FindingValue.FromText("11111111-2222-3333-4444-555555555555")),
                F("smbios.serialNumber", FindingValue.FromText("SYNTHETIC-SERIAL")), F("network.macAddress", FindingValue.FromText("020000000001"))]
        });
        rightReport = currentReport with { Findings = [.. currentReport.Findings.Select(item => item.Id == "system.memoryBytes" ? item with { Value = FindingValue.FromNumber(4294967296m, ValueUnit.Bytes) } : item)] };
        currentVmx = new(VmxDocument.Parse(System.Text.Encoding.UTF8.GetBytes("virtualHW.version = \"19\"\nmemsize = \"4096\"\nnumvcpus = \"2\"\ncpuid.coresPerSocket = \"1\"\nguestOS = \"windows11-64\"\n")), PowerStateEvidence.Unknown("Synthetic preview"));
        requestedMemory = "8192";
        requestedCpu = "4";
        configPlan = VmxConfigPlanner.Create(currentVmx.Document, CandidateRequest(currentVmx.Document));
        StatusText = Localization.Text("Status.Synthetic");
        Refresh();
    }

    private void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    private static string DisplayValue(FindingValue value) => value.Kind == ValueKind.Number && value.Unit == ValueUnit.Bytes
        ? (value.Number / 1073741824m)?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " GiB"
        : value.Kind == ValueKind.Number && value.Unit == ValueUnit.Count
            ? value.Number?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—" : value.ToString();
    public void Dispose() => operationCancellation?.Cancel();
}
