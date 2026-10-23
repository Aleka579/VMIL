using System.Text;
using VMIdentityLab.Core.Comparison;
using VMIdentityLab.Core.Reports;
using VMIdentityLab.VMware;

namespace VMIdentityLab.UnitTests;

public sealed class IdentityAnalysisTests
{
    [Fact]
    public void MissingObservationsStayUncollectedAndSupportDescribesFiniteProjectCapability()
    {
        var rows = IdentityAnalyzer.Analyze(null);
        Assert.Equal(17, rows.Count);
        Assert.All(rows, row => Assert.Null(row.Observation));
        Assert.All(rows.Where(row => row.Definition.Id is not ("network.macAddress" or "smbios.uuid" or "cpu.logicalProcessors" or "system.memoryBytes")),
            row => Assert.Equal(IdentityWriteSupport.NotSupported, row.WriteSupport));
        Assert.Equal(DeclarationState.NoSnapshot, rows.Single(row => row.Definition.Id == "system.memoryBytes").Declaration.State);
    }

    [Theory]
    [InlineData("network.macAddress", IdentityBasis.M11MacExactRange)]
    [InlineData("smbios.uuid", IdentityBasis.M11UuidExactRange)]
    public void IdentityCapabilityUsesM11EvidenceWithoutInventingAnObservationOrTargetApproval(string id, IdentityBasis basis)
    {
        var row = IdentityAnalyzer.Analyze(null).Single(row => row.Definition.Id == id);
        Assert.Equal(IdentityWriteSupport.ExistingLimitedPolicy, row.WriteSupport);
        Assert.Equal(basis, row.Basis);
        Assert.Null(row.Observation);
        Assert.Equal(DeclarationState.NoSnapshot, row.Declaration.State);
        Assert.Equal(ValueKind.Unknown, row.Declaration.Value.Kind);
    }

    [Fact]
    public void HtmlUsesCurrentFiniteIdentityEvidenceAndKeepsTargetPermissionSeparate()
    {
        var html = HtmlReportRenderer.Render(ReportSamples.Report());
        Assert.Contains("17.5.2.23775571/vmrest1.3.1.23775571/HW19", html);
        Assert.Contains("手册238页", html);
        Assert.Contains("手册160–161页", html);
        Assert.Contains("不能当作当前目标的写入许可", html);
        Assert.Contains("不支持缺失键或生成/复制流程", html);
        Assert.DoesNotContain("研究候选，未批准写入", html);
        Assert.DoesNotContain("目标版本与恢复未验证", html);
    }

    [Fact]
    public void DeclarationDoesNotReplaceObservationOrAssertTheyDescribeTheSameMachine()
    {
        var report = ReportSamples.Report(ReportSamples.Finding("system.memoryBytes", FindingValue.FromNumber(17179869184m, ValueUnit.Bytes))) with
        {
            VmxSummary = new() { Declarations = new Dictionary<string, string> { ["MEMSIZE"] = "4096" } }
        };
        var row = IdentityAnalyzer.Analyze(report).Single(row => row.Definition.Id == "system.memoryBytes");
        Assert.Equal(4096, row.Declaration.Value.Number);
        Assert.Equal(ValueUnit.Mebibytes, row.Declaration.Value.Unit);
        Assert.Equal(17179869184m, row.Observation!.Value.Number);
        Assert.Equal(ValueUnit.Bytes, row.Observation.Value.Unit);
        Assert.Equal(IdentityWriteSupport.ExistingLimitedPolicy, row.WriteSupport);
    }

    [Theory]
    [InlineData("smbios.uuid")]
    [InlineData("smbios.serialNumber")]
    [InlineData("network.macAddress")]
    public void NewAndLegacyIdentifierSourcesRemainAccurateAndAlwaysRedacted(string id)
    {
        foreach (var source in new[] { "identifier", FindingCatalog.ById[id].Source })
        {
            var item = ReportSamples.Finding(id, FindingValue.FromText("SYNTHETIC-PRIVATE-VALUE")) with { Source = source };
            var original = ReportSamples.Report(item);
            var roundTrip = ReportJson.Deserialize(ReportJson.Serialize(original));
            Assert.Equal("1.0", roundTrip.SchemaVersion);
            Assert.Equal(source, Assert.Single(roundTrip.Findings).Source);
            Assert.Equal(ValueKind.Redacted, Assert.Single(roundTrip.Findings).Value.Kind);
            Assert.Equal(ValueKind.Redacted, IdentityAnalyzer.Analyze(original).Single(row => row.Definition.Id == id).Observation!.Value.Kind);
            Assert.Equal(ComparisonReason.Redacted, Assert.Single(ReportComparer.Compare(roundTrip, roundTrip)).Reason);
            Assert.DoesNotContain("SYNTHETIC-PRIVATE-VALUE", HtmlReportRenderer.Render(original));
        }
    }

    [Fact]
    public void UnreviewedIdentifierSourceIsNotLabeledAsTheNewCollector()
    {
        var item = ReportSamples.Finding("smbios.uuid", FindingValue.FromText("SYNTHETIC-PRIVATE-VALUE")) with
        {
            Source = @"C:\Users\SyntheticPrivate\provider.dll"
        };
        var bytes = ReportJson.Serialize(ReportSamples.Report(item));
        Assert.Equal("unverified", Assert.Single(ReportJson.Deserialize(bytes).Findings).Source);
        Assert.DoesNotContain("SyntheticPrivate", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void InvalidDeclarationIsNotReportedAsAValue()
    {
        var report = ReportSamples.Report() with { VmxSummary = new() { Declarations = new Dictionary<string, string> { ["numvcpus"] = "-2" } } };
        var row = IdentityAnalyzer.Analyze(report).Single(row => row.Definition.Id == "cpu.logicalProcessors");
        Assert.Equal(DeclarationState.Invalid, row.Declaration.State);
        Assert.Equal(ValueKind.Unknown, row.Declaration.Value.Kind);
    }

    [Fact]
    public void VmxIdentityInspectionRetainsBytesAndNeverReturnsRawIdentifiers()
    {
        const string uuid = "11111111-2222-3333-4444-555555555555";
        const string mac = "02:00:00:00:00:01";
        var bytes = Encoding.UTF8.GetBytes($"memsize = \"4096\"\nuuid.bios = \"{uuid}\"\nethernet0.generatedAddress = \"{mac}\"\n");
        var document = VmxDocument.Parse(bytes);
        var declarations = VmxIdentityReader.Read(document);
        Assert.Equal(DeclarationState.Redacted, declarations["smbios.uuid"].State);
        Assert.Equal(DeclarationState.Redacted, declarations["network.macAddress"].State);
        Assert.All(declarations.Values, declaration =>
        {
            Assert.DoesNotContain(uuid, declaration.ToString());
            Assert.DoesNotContain(mac, declaration.ToString());
        });
        Assert.Equal(bytes, document.GetOriginalBytes());
    }

    [Fact]
    public void DuplicateAndInvalidVmxDeclarationsKeepExplicitStates()
    {
        var document = VmxDocument.Parse(Encoding.UTF8.GetBytes("numvcpus = \"2\"\nNUMVCPUS = \"4\"\nmemsize = \"invalid\"\nuuid.bios = \"first\"\nUUID.BIOS = \"second\"\n"));
        var declarations = VmxIdentityReader.Read(document);
        Assert.Equal(DeclarationState.Ambiguous, declarations["cpu.logicalProcessors"].State);
        Assert.Equal(DeclarationState.Invalid, declarations["system.memoryBytes"].State);
        Assert.Equal(DeclarationState.Ambiguous, declarations["smbios.uuid"].State);
        Assert.Equal(DeclarationState.NotIncluded, declarations["network.macAddress"].State);
    }

    [Fact]
    public void IdentifierDeclarationCannotLeakThroughAnalysisOrPublicReportSummary()
    {
        var declarations = new Dictionary<string, IdentityDeclaration>
        {
            ["smbios.uuid"] = new(DeclarationState.Present, FindingValue.FromText("SYNTHETIC-PRIVATE-VALUE"), "VMX: uuid.bios")
        };
        var row = IdentityAnalyzer.Analyze(ReportSamples.Report(), declarations).Single(row => row.Definition.Id == "smbios.uuid");
        Assert.Equal(DeclarationState.Redacted, row.Declaration.State);
        Assert.Equal(ValueKind.Redacted, row.Declaration.Value.Kind);
        var report = ReportSamples.Report() with { VmxSummary = new() { Declarations = new Dictionary<string, string> { ["uuid.bios"] = "SYNTHETIC-PRIVATE-VALUE" } } };
        Assert.DoesNotContain("SYNTHETIC-PRIVATE-VALUE", Encoding.UTF8.GetString(ReportJson.Serialize(report)));
        Assert.Contains("未采集", HtmlReportRenderer.Render(report));
    }
}
