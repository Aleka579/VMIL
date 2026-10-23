using System.IO;
using System.Windows.Input;
using VMIdentityLab.App.Infrastructure;
using VMIdentityLab.VMware;
using VMIdentityLab.VMware.Configuration;
using VMIdentityLab.Windows;

namespace VMIdentityLab.App.ViewModels;

public sealed partial class MainViewModel
{
    private int selectedIdentityFieldIndex;
    public string IdentityConfigScope => Localization.Text(configurationTarget is null ? "IdentityConfig.Scope" : "IdentityConfig.AcceptanceScope");

    internal bool IsPreviewWriteApproved(VmxPreparedChange preview)
        => (configurationTarget is null || string.Equals(configurationTarget, preview.TargetPath, StringComparison.OrdinalIgnoreCase))
            && VmxApiConfigManager.IsWritePolicyApproved(preview, configurationPolicy);
    public ICommand PreviewIdentityConfigCommand { get; }
    public int SelectedIdentityFieldIndex
    {
        get => selectedIdentityFieldIndex;
        set
        {
            if (selectedIdentityFieldIndex == value) return;
            selectedIdentityFieldIndex = value;
            InvalidateConfigPreview();
            Notify();
        }
    }

    private async Task RunIdentityPreviewAsync()
    {
        await RunOperationAsync(PreviewIdentityConfigAsync);
        if (configNoticeKey == "IdentityConfig.Unsupported") StatusText = ConfigNotice;
    }

    private async Task<bool> PreviewIdentityConfigAsync(CancellationToken token)
    {
        InvalidateConfigPreview();
        try
        {
            var field = SelectedIdentityFieldIndex switch
            {
                0 => IdentityConfigField.StaticMac, 1 => IdentityConfigField.BiosUuid,
                _ => throw new ArgumentException("Unknown identity candidate.")
            };
            var version = WorkstationEnvironment.ReadInstalledVersion() ?? new Version(0, 0);
            var prepared = await Task.Run(() => VmxIdentityConfigPlanner.PrepareAsync(currentVmxPath!, version, field, token), token);
            currentVmx = await Task.Run(() => new VmxReader().ReadAsync(currentVmxPath!, token), token);
            if (prepared.Plan.OriginalHash != currentVmx.Document.ContentHash)
                throw new InvalidDataException("Configuration changed while preparing the identity preview.");
            token.ThrowIfCancellationRequested();
            if (currentReport is not null) currentReport = currentReport with { VmxSummary = currentVmx.Document.ToSummary(currentVmx.PowerState) };
            configPreview = prepared;
            configPlan = prepared.Plan;
            configNoticeKey = !IsPreviewWriteApproved(prepared) ? "IdentityConfig.ReviewOnly"
                : configurationTarget is null ? "IdentityConfig.Preview" : "IdentityConfig.AcceptancePreview";
            if (VmxApiConfigManager.HasPendingOperation(prepared.TargetPath))
            {
                configNoticeKey = "Config.ApiUnknown";
                RecoveryLocation = VmxApiConfigManager.PendingOperationPath(prepared.TargetPath);
            }
        }
        catch (Exception error) when (error is ArgumentException or InvalidDataException)
        {
            configNoticeKey = "IdentityConfig.Unsupported";
            RefreshConfigPreview();
            return false;
        }
        Refresh();
        return true;
    }
}
