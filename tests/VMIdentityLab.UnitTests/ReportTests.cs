using System.Text;
using System.Text.Json.Nodes;
using VMIdentityLab.Core.Reports;

namespace VMIdentityLab.UnitTests;

public sealed class ReportTests
{
    [Theory]
    [InlineData("Win32_Processor.Name")]
    [InlineData("CPUID.80000002..80000004")]
    public void CpuReportRoundTripRetainsItsReviewedObservationSource(string source)
    {
        var item = ReportSamples.Finding("cpu.model", FindingValue.FromText("Synthetic CPU")) with { Source = source };
        var parsed = ReportJson.Deserialize(ReportJson.Serialize(ReportSamples.Report(item)));
        Assert.Equal(source, Assert.Single(parsed.Findings).Source);
    }

    [Fact]
    public void CpuReportSourceDoesNotExposeUnreviewedInput()
    {
        var item = ReportSamples.Finding("cpu.model", FindingValue.FromText("Synthetic CPU")) with
        {
            Source = @"C:\Users\SyntheticPrivate\provider.dll"
        };
        var parsed = ReportJson.Deserialize(ReportJson.Serialize(ReportSamples.Report(item)));
        Assert.Equal("CPUID.80000002..80000004", Assert.Single(parsed.Findings).Source);
    }

    [Fact]
    public void JsonPreservesTypedValuesAndCollectionFailures()
    {
        var report = ReportSamples.Report(
            ReportSamples.Finding("system.memoryBytes", FindingValue.FromNumber(4096, ValueUnit.Bytes)),
            ReportSamples.Finding("cpu.hypervisorPresent", FindingValue.FromBoolean(true)),
            ReportSamples.Finding("smbios.model", new(), CollectionStatus.AccessDenied));
        var parsed = ReportJson.Deserialize(ReportJson.Serialize(report));
        Assert.Equal(4096, parsed.Findings[0].Value.Number);
        Assert.Equal(ValueUnit.Bytes, parsed.Findings[0].Value.Unit);
        Assert.True(parsed.Findings[1].Value.Boolean);
        Assert.Equal(CollectionStatus.AccessDenied, parsed.Findings[2].Status);
    }

    [Fact]
    public void IdentifierCatalogOverridesUntrustedPublicSensitivity()
    {
        var report = ReportSamples.Report(ReportSamples.Finding("smbios.serialNumber", FindingValue.FromText("SYNTHETIC-SECRET-42")));
        var bytes = ReportJson.Serialize(report);
        Assert.DoesNotContain("SYNTHETIC-SECRET-42", Encoding.UTF8.GetString(bytes));
        Assert.Equal(ValueKind.Redacted, Assert.Single(ReportJson.Deserialize(bytes).Findings).Value.Kind);
    }

    [Fact]
    public void UnknownFieldsAndRawDiagnosticsAreOmitted()
    {
        var report = ReportSamples.Report(ReportSamples.Finding("custom.secret", FindingValue.FromText("synthetic-password"))) with
        {
            Diagnostics = [new() { Code = "raw", Message = @"C:\Users\SyntheticUser\private.json" }],
            VmxSummary = new() { Declarations = new Dictionary<string, string> { ["memsize"] = "4096", ["uuid.bios"] = "private-vm-identity" } }
        };
        var json = Encoding.UTF8.GetString(ReportJson.Serialize(report));
        Assert.DoesNotContain("synthetic-password", json);
        Assert.DoesNotContain("SyntheticUser", json);
        Assert.DoesNotContain("private-vm-identity", json);
        Assert.Empty(ReportJson.Deserialize(ReportJson.Serialize(report)).Findings);
        Assert.Contains("unknownFieldsOmitted", json);
    }

    [Theory]
    [InlineData(@"C:\Users\SyntheticUser\report.json")]
    [InlineData(@"\\synthetic-server\private\report.json")]
    [InlineData("12345678-abcd-1234-abcd-123456789abc")]
    [InlineData("00:11:22:33:44:55")]
    public void TextRedactionRemovesPathsAndHardwareIdentifiers(string raw)
    {
        Assert.DoesNotContain(raw, ReportPrivacy.Text(raw));
    }

    [Fact]
    public void HtmlEncodesImportedMarkupAndLoadsNoRemoteResources()
    {
        var report = ReportSamples.Report(ReportSamples.Finding("cpu.model", FindingValue.FromText("<script>alert('synthetic')</script> & CPU")));
        var html = HtmlReportRenderer.Render(report);
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("&amp; CPU", html);
        Assert.DoesNotContain("src=", html);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{invalid}")]
    public void InvalidReportsProduceSafeErrors(string input)
    {
        Assert.Throws<ReportFormatException>(() => ReportJson.Deserialize(Encoding.UTF8.GetBytes(input)));
    }

    [Fact]
    public void UnsupportedVersionAndDuplicateFieldsAreRejected()
    {
        var root = JsonNode.Parse(ReportJson.Serialize(ReportSamples.Report()))!;
        root["schemaVersion"] = "99.0";
        Assert.Throws<ReportFormatException>(() => ReportJson.Deserialize(Encoding.UTF8.GetBytes(root.ToJsonString())));
        var item = ReportSamples.Finding("cpu.model", FindingValue.FromText("Synthetic CPU"));
        Assert.Throws<ReportFormatException>(() => ReportJson.Serialize(ReportSamples.Report(item, item)));
    }

    [Fact]
    public void CaseAmbiguousVmxDeclarationsAreRejectedAsReportErrors()
    {
        var report = ReportSamples.Report() with
        {
            VmxSummary = new() { Declarations = new Dictionary<string, string> { ["memsize"] = "4096", ["MEMSIZE"] = "8192" } }
        };
        Assert.Throws<ReportFormatException>(() => ReportJson.Serialize(report));
    }

    [Fact]
    public void ContradictoryValuePayloadIsRejected()
    {
        var value = new FindingValue { Kind = ValueKind.Boolean, Boolean = true, Text = "unexpected" };
        Assert.Throws<ReportFormatException>(() => ReportJson.Serialize(ReportSamples.Report(ReportSamples.Finding("cpu.hypervisorPresent", value))));
    }

    [Fact]
    public void OversizedInputIsRejectedBeforeParsing()
    {
        Assert.Throws<ReportFormatException>(() => ReportJson.Deserialize(new byte[ReportJson.MaxReportBytes + 1]));
    }
}
