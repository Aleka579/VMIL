using VMIdentityLab.Core.Reports;

namespace VMIdentityLab.Core.Evidence;

public static class EvidenceEvaluator
{
    public static EvidenceSummary Evaluate(IReadOnlyList<Finding> findings)
    {
        Finding? Find(string id) => findings.FirstOrDefault(item => item.Id == id && item.Status == CollectionStatus.Success);
        bool ContainsVmware(string id) => Find(id)?.Value.Text?.Contains("VMware", StringComparison.OrdinalIgnoreCase) == true;
        var hypervisor = Find("cpu.hypervisorPresent")?.Value.Boolean;
        var smbios = ContainsVmware("smbios.manufacturer") || ContainsVmware("smbios.model");
        var cpu = ContainsVmware("cpu.hypervisorVendor");
        var devices = Find("pnp.vmwareDeviceCount")?.Value.Number > 0;
        var tools = Find("tools.installed")?.Value.Boolean == true;
        var independentSources = (smbios ? 1 : 0) + (cpu ? 1 : 0) + (devices ? 1 : 0);
        var complete = (Find("smbios.manufacturer") is not null || Find("smbios.model") is not null)
            && hypervisor.HasValue && Find("pnp.vmwareDeviceCount") is not null
            && (hypervisor == false || Find("cpu.hypervisorVendor") is not null);
        var state = independentSources >= 2 ? "consistent"
            : independentSources != 0 || tools || !complete ? "inconclusive" : "notObserved";
        return new()
        {
            HypervisorDetected = hypervisor,
            VmwareGuestEvidence = state,
            Explanation = state switch
            {
                "consistent" => "Multiple independent hardware sources show VMware guest evidence; this is an observation, not an identity guarantee.",
                "notObserved" => "No VMware guest evidence was observed in the available hardware sources. This does not prove a physical machine.",
                _ => "Available evidence is incomplete or insufficient. Hypervisor presence and VMware Tools alone do not identify a VMware guest."
            }
        };
    }
}
