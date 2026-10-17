using System.Globalization;

namespace VMIdentityLab.VMware;

public enum VmxSupportStatus { Confirmed, Unknown, Unsupported, Ambiguous }

public sealed record VmxSupportEvidence(VmxSupportStatus Status, string Source, string Scope);

// Read-only compatibility evidence; no configuration-write permission is granted here.
public static class VmxSupportCatalog
{
    public const string HardwareSource = "https://knowledge.broadcom.com/external/article/315655/virtual-machine-hardware-versions.html";

    public static VmxSupportEvidence Analyze(VmxDocument document, VmxEntry entry, Version? workstation)
    {
        if (document.DuplicateKeys.Contains(entry.Key, StringComparer.OrdinalIgnoreCase))
            return new(VmxSupportStatus.Ambiguous, "", "Duplicate declaration");
        if (!entry.Key.Equals("virtualHW.version", StringComparison.OrdinalIgnoreCase)
            || workstation is not { Major: 17, Minor: 5, Build: 2 })
            return new(VmxSupportStatus.Unknown, "", "No reviewed target-version mapping");
        if (!int.TryParse(entry.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var hardware)
            || hardware < 3 || hardware == 5)
            return new(VmxSupportStatus.Unknown, HardwareSource, "Unconfirmed hardware version value");
        return new(hardware <= 20 ? VmxSupportStatus.Confirmed : VmxSupportStatus.Unsupported,
            HardwareSource, "Workstation 17.5.2 hardware-version ceiling: 20; device compatibility remains separate");
    }
}
