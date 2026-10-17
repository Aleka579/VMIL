using VMIdentityLab.Core.Evidence;
using VMIdentityLab.Core.Reports;

namespace VMIdentityLab.UnitTests;

public sealed class EvidenceTests
{
    private static Finding F(string id, FindingValue value) => ReportSamples.Finding(id, value);
    private static Finding[] PhysicalSources() =>
    [
        F("smbios.manufacturer", FindingValue.FromText("Synthetic OEM")),
        F("cpu.hypervisorPresent", FindingValue.FromBoolean(true)),
        F("cpu.hypervisorVendor", FindingValue.FromText("Microsoft Hv")),
        F("pnp.vmwareDeviceCount", FindingValue.FromNumber(0, ValueUnit.Count))
    ];

    [Fact]
    public void HyperVSignalDoesNotIdentifyVmwareGuest()
    {
        var evidence = EvidenceEvaluator.Evaluate(PhysicalSources());
        Assert.True(evidence.HypervisorDetected);
        Assert.Equal("notObserved", evidence.VmwareGuestEvidence);
    }

    [Fact]
    public void HostVmwareDevicesAndMicrosoftHypervisorAreInsufficient()
    {
        var sources = PhysicalSources().Select(item => item.Id == "pnp.vmwareDeviceCount"
            ? item with { Value = FindingValue.FromNumber(3, ValueUnit.Count) } : item).ToArray();
        var evidence = EvidenceEvaluator.Evaluate(sources);
        Assert.Equal("inconclusive", evidence.VmwareGuestEvidence);
    }

    [Fact]
    public void ToolsAloneIsInsufficientGuestEvidence()
    {
        var evidence = EvidenceEvaluator.Evaluate([.. PhysicalSources(), F("tools.installed", FindingValue.FromBoolean(true))]);
        Assert.Equal("inconclusive", evidence.VmwareGuestEvidence);
    }

    [Fact]
    public void ManufacturerAndModelAreOneSourceFamily()
    {
        var evidence = EvidenceEvaluator.Evaluate([F("smbios.manufacturer", FindingValue.FromText("VMware, Inc.")), F("smbios.model", FindingValue.FromText("VMware Virtual Platform"))]);
        Assert.Equal("inconclusive", evidence.VmwareGuestEvidence);
    }

    [Fact]
    public void IndependentSmbiosAndCpuEvidenceIsConsistent()
    {
        var evidence = EvidenceEvaluator.Evaluate([F("smbios.model", FindingValue.FromText("VMware Virtual Platform")), F("cpu.hypervisorVendor", FindingValue.FromText("VMwareVMware"))]);
        Assert.Equal("consistent", evidence.VmwareGuestEvidence);
    }

    [Fact]
    public void IndependentCpuAndDeviceEvidenceIsConsistent()
    {
        var evidence = EvidenceEvaluator.Evaluate([F("cpu.hypervisorVendor", FindingValue.FromText("VMwareVMware")), F("pnp.vmwareDeviceCount", FindingValue.FromNumber(2, ValueUnit.Count))]);
        Assert.Equal("consistent", evidence.VmwareGuestEvidence);
    }

    [Fact]
    public void FailureIsNotNegativeOrPositiveEvidence()
    {
        var failed = F("cpu.hypervisorPresent", FindingValue.FromBoolean(true)) with { Status = CollectionStatus.Failed };
        var evidence = EvidenceEvaluator.Evaluate([failed]);
        Assert.Null(evidence.HypervisorDetected);
        Assert.Equal("inconclusive", evidence.VmwareGuestEvidence);
    }

    [Fact]
    public void ImportedInterpretationIsRecomputedFromEvidence()
    {
        var report = ReportSamples.Report() with { VirtualizationEvidence = new() { VmwareGuestEvidence = "consistent", HypervisorDetected = true } };
        var imported = ReportJson.Deserialize(ReportJson.Serialize(report));
        Assert.Equal("inconclusive", imported.VirtualizationEvidence.VmwareGuestEvidence);
        Assert.Null(imported.VirtualizationEvidence.HypervisorDetected);
    }
}
