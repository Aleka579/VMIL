using VMIdentityLab.Core.Comparison;
using VMIdentityLab.Core.Reports;

namespace VMIdentityLab.UnitTests;

public sealed class ComparisonTests
{
    [Theory]
    [InlineData(ValueUnit.Mebibytes, 4096)]
    [InlineData(ValueUnit.Gibibytes, 4)]
    public void EquivalentMemoryUnitsCompareEqual(ValueUnit unit, int amount)
    {
        var left = ReportSamples.Report(ReportSamples.Finding("system.memoryBytes", FindingValue.FromNumber(4294967296m, ValueUnit.Bytes)));
        var right = ReportSamples.Report(ReportSamples.Finding("system.memoryBytes", FindingValue.FromNumber(amount, unit)));
        Assert.Equal(ComparisonKind.Same, Assert.Single(ReportComparer.Compare(left, right)).Kind);
    }

    [Fact]
    public void MissingAndRedactedValuesAreNotHardwareDifferences()
    {
        var item = ReportSamples.Finding("smbios.serialNumber", FindingValue.FromText("synthetic-secret"));
        var missing = Assert.Single(ReportComparer.Compare(ReportSamples.Report(item), ReportSamples.Report()));
        Assert.Equal(ComparisonReason.Missing, missing.Reason);
        var redacted = Assert.Single(ReportComparer.Compare(ReportSamples.Report(item), ReportSamples.Report(item)));
        Assert.Equal(ComparisonKind.NotComparable, redacted.Kind);
        Assert.Equal(ComparisonReason.Redacted, redacted.Reason);
    }

    [Fact]
    public void CollectionFailureIsNotAnEqualHealthyValue()
    {
        var item = ReportSamples.Finding("cpu.model", new(), CollectionStatus.Timeout);
        var row = Assert.Single(ReportComparer.Compare(ReportSamples.Report(item), ReportSamples.Report(item)));
        Assert.Equal(ComparisonKind.NotComparable, row.Kind);
        Assert.Equal(ComparisonReason.Unavailable, row.Reason);
    }

    [Fact]
    public void ChangedStatusIsReportedSeparately()
    {
        var left = ReportSamples.Finding("cpu.model", FindingValue.FromText("Synthetic CPU"));
        var right = left with { Status = CollectionStatus.AccessDenied, Value = new() };
        var row = Assert.Single(ReportComparer.Compare(ReportSamples.Report(left), ReportSamples.Report(right)));
        Assert.Equal(ComparisonKind.StatusChanged, row.Kind);
    }

    [Fact]
    public void ChangedBooleanValueIsARealValueDifference()
    {
        var left = ReportSamples.Report(ReportSamples.Finding("cpu.hypervisorPresent", FindingValue.FromBoolean(false)));
        var right = ReportSamples.Report(ReportSamples.Finding("cpu.hypervisorPresent", FindingValue.FromBoolean(true)));
        Assert.Equal(ComparisonKind.Different, Assert.Single(ReportComparer.Compare(left, right)).Kind);
    }

    [Fact]
    public void UnitAndTypeConflictsAreExplicit()
    {
        var left = ReportSamples.Finding("system.memoryBytes", FindingValue.FromNumber(4, ValueUnit.Bytes));
        var unitConflict = left with { Value = FindingValue.FromNumber(4, ValueUnit.Count) };
        var typeConflict = left with { Value = FindingValue.FromText("4") };
        Assert.Equal(ComparisonReason.UnitMismatch, Assert.Single(ReportComparer.Compare(ReportSamples.Report(left), ReportSamples.Report(unitConflict))).Reason);
        Assert.Equal(ComparisonReason.TypeMismatch, Assert.Single(ReportComparer.Compare(ReportSamples.Report(left), ReportSamples.Report(typeConflict))).Reason);
    }

    [Fact]
    public void NumericOverflowIsNotAnUnhandledImportError()
    {
        var item = ReportSamples.Finding("system.memoryBytes", FindingValue.FromNumber(decimal.MaxValue, ValueUnit.Gibibytes));
        var row = Assert.Single(ReportComparer.Compare(ReportSamples.Report(item), ReportSamples.Report(item)));
        Assert.Equal(ComparisonReason.NumericOverflow, row.Reason);
    }

    [Fact]
    public void UnsupportedReportVersionIsRejected()
    {
        var report = ReportSamples.Report() with { SchemaVersion = "99.0" };
        Assert.Throws<ReportFormatException>(() => ReportComparer.Compare(report, ReportSamples.Report()));
    }
}
