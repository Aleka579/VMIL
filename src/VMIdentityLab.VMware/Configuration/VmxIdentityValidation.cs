using VMIdentityLab.Core.Comparison;

namespace VMIdentityLab.VMware.Configuration;

// Read-only interpretation of imported private records, not a fresh API query or restore operation.
public static class VmxIdentityValidation
{
    public static ValidationConfiguration FromRecords(VmxBackupHistoryEntry applied, VmxBackupHistoryEntry restored)
    {
        var a = applied.Receipt;
        var r = restored.Receipt;
        if (applied.Id == Guid.Empty || restored.Id == Guid.Empty || applied.Id == restored.Id
            || applied.Operation != "apply" || restored.Operation != "restore" || applied.RecordedAt == default || restored.RecordedAt <= applied.RecordedAt
            || a.Backend != VmxApiConfigManager.BackendName || r.Backend != VmxApiConfigManager.BackendName
            || a.WorkstationVersion != VmxIdentityConfigPlanner.VerifiedVersion || a.WorkstationVersion != r.WorkstationVersion
            || !string.Equals(Path.GetFullPath(a.TargetPath), Path.GetFullPath(r.TargetPath), StringComparison.OrdinalIgnoreCase)
            || a.BeforeIdentity is null || a.AfterIdentity is null || r.BeforeIdentity is null || r.AfterIdentity is null
            || a.BeforeSettings is not null || a.AfterSettings is not null || r.BeforeSettings is not null || r.AfterSettings is not null)
            throw new InvalidDataException("Matching finite identity apply and restore records are required.");
        var key = VmxIdentitySettings.ChangedKey(a.BeforeIdentity, a.AfterIdentity);
        if (key != VmxIdentitySettings.ChangedKey(r.BeforeIdentity, r.AfterIdentity)
            || a.BeforeIdentity != r.AfterIdentity || a.AfterIdentity != r.BeforeIdentity)
            throw new InvalidDataException("The restore does not match this identity application.");
        return new(key == "ethernet0.address" ? ValidationField.StaticMac : ValidationField.BiosUuid, a.WorkstationVersion,
            applied.RecordedAt, restored.RecordedAt, a.BeforeIdentity.MacAddress, a.AfterIdentity.MacAddress,
            WindowsUuid(a.BeforeIdentity.BiosUuid), WindowsUuid(a.AfterIdentity.BiosUuid));
    }

    private static string WindowsUuid(string vmx)
    {
        // SMBIOS exposes the first 4/2/2 bytes in little-endian order.
        var bytes = Convert.FromHexString(vmx.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal));
        return new Guid(bytes).ToString("D");
    }
}
