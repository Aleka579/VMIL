using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using VMIdentityLab.App.Infrastructure;
using VMIdentityLab.Core.Comparison;
using VMIdentityLab.VMware.Configuration;
using VMIdentityLab.Windows;
using Localization = VMIdentityLab.App.Infrastructure.Localization;

namespace VMIdentityLab.App.ViewModels;

public sealed record ValidationDisplayRow(string Layer, string Check, string State, string Reason, string Before, string After, string Restored);
public sealed record ValidationProgramRow(string Phase, string Time, string Evidence, string Hypervisor);

public sealed partial class MainViewModel
{
    private readonly PrivateValidationSnapshot?[] validationSnapshots = new PrivateValidationSnapshot?[3];
    private readonly WindowsValidationCollector validationCollector = new();
    private readonly string[] validationInputs = new string[3];
    private readonly bool[] validationInputErrors = new bool[3];
    private readonly bool[] validationRecordErrors = new bool[2];
    private VmxBackupHistoryEntry? validationApplied;
    private VmxBackupHistoryEntry? validationRestored;
    private IdentityValidationResult? validationResult;
    private string? validationError;
    private int selectedValidationFieldIndex;
    private int selectedValidationCapturePhase;
    private string validationSession = Guid.NewGuid().ToString();
    private string validationAdapter = "";
    public ObservableCollection<ValidationDisplayRow> ValidationChecks { get; } = [];
    public ObservableCollection<ValidationProgramRow> ValidationProgram { get; } = [];
    public ICommand ImportValidationBeforeCommand { get; private set; } = null!;
    public ICommand ImportValidationAfterCommand { get; private set; } = null!;
    public ICommand ImportValidationRestoredCommand { get; private set; } = null!;
    public ICommand ImportValidationApplyCommand { get; private set; } = null!;
    public ICommand ImportValidationRestoreCommand { get; private set; } = null!;
    public ICommand CapturePrivateValidationCommand { get; private set; } = null!;
    public ICommand ExportValidationJsonCommand { get; private set; } = null!;
    public ICommand ExportValidationHtmlCommand { get; private set; } = null!;
    public ICommand ClearValidationCommand { get; private set; } = null!;
    public int SelectedValidationFieldIndex { get => selectedValidationFieldIndex; set { selectedValidationFieldIndex = value; RefreshValidation(); Notify(); } }
    public int SelectedValidationCapturePhase { get => selectedValidationCapturePhase; set { selectedValidationCapturePhase = value; Notify(); CommandManager.InvalidateRequerySuggested(); } }
    public string ValidationSession { get => validationSession; set { validationSession = value; Notify(); CommandManager.InvalidateRequerySuggested(); } }
    public string ValidationAdapter { get => validationAdapter; set { validationAdapter = value; Notify(); CommandManager.InvalidateRequerySuggested(); } }
    public string ValidationBeforeText => ValidationInputText(0);
    public string ValidationAfterText => ValidationInputText(1);
    public string ValidationRestoredText => ValidationInputText(2);
    public string ValidationRecordsText => $"{Localization.Text("Validation.ApplyRecord")}: {validationApplied?.RecordedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "—"} · "
        + $"{Localization.Text("Validation.RestoreRecord")}: {validationRestored?.RecordedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "—"}";
    public string ValidationNotice => Localization.Text(validationError ?? (validationResult is null ? "Validation.Empty" : "Validation.Ready"));

    private void InitializeValidation()
    {
        ImportValidationBeforeCommand = ImportValidationCommand(0);
        ImportValidationAfterCommand = ImportValidationCommand(1);
        ImportValidationRestoredCommand = ImportValidationCommand(2);
        ImportValidationApplyCommand = new AsyncCommand(() => RunOperationAsync(token => ImportValidationRecordAsync(false, token)), () => !IsBusy);
        ImportValidationRestoreCommand = new AsyncCommand(() => RunOperationAsync(token => ImportValidationRecordAsync(true, token)), () => !IsBusy);
        CapturePrivateValidationCommand = new AsyncCommand(() => RunOperationAsync(CapturePrivateValidationAsync),
            () => !IsBusy && Guid.TryParse(ValidationSession, out var id) && id != Guid.Empty && SelectedValidationCapturePhase is >= 0 and <= 2
                && (string.IsNullOrWhiteSpace(ValidationAdapter) || Guid.TryParse(ValidationAdapter, out var adapter) && adapter != Guid.Empty));
        ExportValidationJsonCommand = new AsyncCommand(() => RunOperationAsync(token => ExportValidationAsync(false, token)), () => !IsBusy && validationResult is not null);
        ExportValidationHtmlCommand = new AsyncCommand(() => RunOperationAsync(token => ExportValidationAsync(true, token)), () => !IsBusy && validationResult is not null);
        ClearValidationCommand = new RelayCommand(() =>
        {
            Array.Clear(validationSnapshots); Array.Clear(validationInputs);
            Array.Clear(validationInputErrors); Array.Clear(validationRecordErrors);
            validationApplied = validationRestored = null;
            validationError = null;
            RefreshValidation();
        }, () => !IsBusy);
    }

    private ICommand ImportValidationCommand(int index) => new AsyncCommand(() => RunOperationAsync(async token =>
    {
        var dialog = new OpenFileDialog { Filter = "Validation snapshot / diagnostic report (*.json)|*.json", Multiselect = false, CheckFileExists = true };
        if (dialog.ShowDialog(Application.Current.MainWindow) != true) return false;
        validationSnapshots[index] = null;
        validationInputs[index] = "";
        validationInputErrors[index] = true;
        validationError = "Validation.InputRejected";
        RefreshValidation();
        var snapshot = await Task.Run(() => ValidationSnapshotJson.ReadAsync(dialog.FileName, (ValidationPhase)index, token), token);
        validationSnapshots[index] = snapshot;
        validationInputErrors[index] = false;
        validationInputs[index] = Path.GetFileName(dialog.FileName);
        validationError = null;
        RefreshValidation();
        return true;
    }), () => !IsBusy);

    private async Task<bool> ImportValidationRecordAsync(bool restore, CancellationToken token)
    {
        var dialog = new OpenFileDialog { Filter = "Private recovery record (*.json)|*.json", Multiselect = false, CheckFileExists = true };
        if (dialog.ShowDialog(Application.Current.MainWindow) != true) return false;
        if (restore) validationRestored = null; else validationApplied = null;
        validationRecordErrors[restore ? 1 : 0] = true;
        validationError = "Validation.RecordRejected";
        RefreshValidation();
        VmxBackupHistoryEntry entry;
        try { entry = await Task.Run(() => LocalVmxBackupHistory.ReadEntryAsync(dialog.FileName, token), token); }
        catch (System.Text.Json.JsonException) { throw new InvalidDataException("Invalid recovery record."); }
        if (entry.Operation != (restore ? "restore" : "apply") || entry.Receipt.BeforeIdentity is null)
            throw new InvalidDataException("Select the matching identity recovery record.");
        if (restore) validationRestored = entry; else validationApplied = entry;
        validationRecordErrors[restore ? 1 : 0] = false;
        validationError = null;
        RefreshValidation();
        return true;
    }

    private async Task<bool> CapturePrivateValidationAsync(CancellationToken token)
    {
        var dialog = new SaveFileDialog { Filter = "PRIVATE identity snapshot (*.json)|*.json", DefaultExt = ".json", AddExtension = true,
            OverwritePrompt = false, FileName = "VMIL-PRIVATE-" + ((ValidationPhase)SelectedValidationCapturePhase).ToString().ToLowerInvariant() + ".json" };
        if (dialog.ShowDialog(Application.Current.MainWindow) != true) return false;
        if (!Path.GetExtension(dialog.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase)
            || File.Exists(dialog.FileName) || Directory.Exists(dialog.FileName)) throw new IOException("Select a new private JSON output.");
        if (MessageBox.Show(Application.Current.MainWindow, Localization.Text("Validation.PrivateConfirm"), "VM Identity Lab",
            MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return false;
        // Capture and save only. Do not assume the local Windows system is the selected VM.
        Guid? adapterId = string.IsNullOrWhiteSpace(ValidationAdapter) ? null : Guid.Parse(ValidationAdapter);
        var snapshot = await Task.Run(() => validationCollector.CapturePrivateAsync(Guid.Parse(ValidationSession),
            (ValidationPhase)SelectedValidationCapturePhase, token, adapterId), token);
        await Task.Run(() => ValidationSnapshotJson.SavePrivateAsync(dialog.FileName, snapshot, token), token);
        return true;
    }

    private async Task<bool> ExportValidationAsync(bool html, CancellationToken token)
    {
        var result = validationResult!;
        var dialog = new SaveFileDialog { Filter = html ? "HTML validation (*.html)|*.html" : "JSON validation (*.json)|*.json",
            DefaultExt = html ? ".html" : ".json", AddExtension = true, OverwritePrompt = false,
            FileName = "VMIL-validation" + (html ? ".html" : ".json") };
        if (dialog.ShowDialog(Application.Current.MainWindow) != true) return false;
        await Task.Run(() => html ? ValidationSnapshotJson.SavePublicHtmlAsync(dialog.FileName, result, token)
            : ValidationSnapshotJson.SavePublicJsonAsync(dialog.FileName, result, token), token);
        return true;
    }

    private string ValidationInputText(int index) => validationSnapshots[index] is not { } item ? Localization.Text("Validation.NoInput")
        : VMIdentityLab.Core.Reports.ReportPrivacy.Text(validationInputs[index]) + " · " + Localization.Text("Validation.Data." + item.DataKind)
            + " · " + item.Report.CollectedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz");

    private void RefreshValidation()
    {
        validationResult = null;
        validationError = validationInputErrors.Any(value => value) ? "Validation.InputRejected"
            : validationRecordErrors.Any(value => value) ? "Validation.RecordRejected" : null;
        ValidationChecks.Clear(); ValidationProgram.Clear();
        ValidationConfiguration? configuration = null;
        if (validationApplied is not null && validationRestored is not null)
        {
            try { configuration = VmxIdentityValidation.FromRecords(validationApplied, validationRestored); }
            catch (Exception error) when (error is InvalidDataException or ArgumentException)
            { validationError = "Validation.RecordRejected"; }
        }
        if (validationError is null && validationSnapshots.All(item => item is not null) && SelectedValidationFieldIndex is 0 or 1)
        {
            validationResult = IdentityValidationComparer.Compare((ValidationField)SelectedValidationFieldIndex,
                validationSnapshots[0]!, validationSnapshots[1]!, validationSnapshots[2]!, configuration);
            if ((validationApplied is null) != (validationRestored is null))
                validationResult = validationResult with { Configuration = [new("configuration", ValidationState.NotProvided, ValidationReason.Missing)] };
            string Status(VMIdentityLab.Core.Reports.CollectionStatus? value) => value is null ? "—" : Localization.Text("Collection." + value);
            foreach (var layer in new[] { ("Validation.Configuration", validationResult.Configuration), ("Validation.Guest", validationResult.Guest) })
                foreach (var row in layer.Item2) ValidationChecks.Add(new(Localization.Text(layer.Item1),
                    VMIdentityLab.Core.Reports.FindingCatalog.ById.ContainsKey(row.Check)
                        ? VMIdentityLab.Core.Reports.FindingCatalog.Title(row.Check, Localization.English) : Localization.Text("Validation.Check." + row.Check),
                    Localization.Text("Validation.State." + row.State), Localization.Text("Validation.Reason." + row.Reason),
                    Status(row.BeforeStatus), Status(row.AfterStatus), Status(row.RestoredStatus)));
            foreach (var row in validationResult.Program) ValidationProgram.Add(new(Localization.Text("Validation.Phase." + row.Phase),
                row.CollectedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz"), Localization.Text("Evidence." + row.VmwareGuestEvidence),
                Localization.Text(row.HypervisorDetected == true ? "State.Detected" : row.HypervisorDetected == false ? "State.NotObserved" : "State.Unknown")));
        }
        Notify(nameof(ValidationBeforeText)); Notify(nameof(ValidationAfterText)); Notify(nameof(ValidationRestoredText));
        Notify(nameof(ValidationRecordsText)); Notify(nameof(ValidationNotice));
        CommandManager.InvalidateRequerySuggested();
    }
}
