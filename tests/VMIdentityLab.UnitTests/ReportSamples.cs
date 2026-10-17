using VMIdentityLab.Core.Reports;

namespace VMIdentityLab.UnitTests;

internal static class ReportSamples
{
    public static readonly DateTimeOffset Time = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
    public static Finding Finding(string id, FindingValue value, CollectionStatus status = CollectionStatus.Success,
        DataSensitivity sensitivity = DataSensitivity.Public) => new()
        {
            Id = id, Category = "synthetic", Source = "synthetic", ObservedAt = Time,
            Status = status, Value = value, Sensitivity = sensitivity
        };

    public static DiagnosticReport Report(params Finding[] findings) => new()
    {
        CollectedAt = Time,
        System = new() { Os = "Windows (synthetic)", Build = "00000", Architecture = "X64" },
        Findings = findings
    };
}
