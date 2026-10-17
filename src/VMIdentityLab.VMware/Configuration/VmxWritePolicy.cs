namespace VMIdentityLab.VMware.Configuration;

public sealed record VmxConfigCandidate(string Key, string Unit, string OfficialReference);

// Approval must cover the actual version and both original and proposed values.
// Test fixtures inject their own policy; the application always uses ReleasedVmxWritePolicy.
public interface IVmxWritePolicy
{
    bool IsApproved(Version workstationVersion, VmxConfigChange change);
}

public sealed class ReleasedVmxWritePolicy : IVmxWritePolicy
{
    public static IReadOnlyList<VmxConfigCandidate> Candidates { get; } = Array.AsReadOnly(new[]
    {
        new VmxConfigCandidate("memsize", "MiB", "https://developer.broadcom.com/xapis/vmware-workstation-pro-api/latest/data-structures/VMMemory/"),
        new VmxConfigCandidate("numvcpus", "count", "https://developer.broadcom.com/xapis/vmware-workstation-pro-api/latest/data-structures/VMProcessors/")
    });

    // API fields do not prove VMX mappings, limits or isolated-clone compatibility.
    // No parameter has the evidence required for production writes yet.
    public bool IsApproved(Version workstationVersion, VmxConfigChange change) => false;
}
