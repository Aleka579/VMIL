using System.Globalization;
using System.Text.Json.Serialization;

namespace VMIdentityLab.Core.Reports;

public enum CollectionStatus { Success, AccessDenied, NotSupported, Timeout, Failed }
public enum ValueKind { Unknown, Text, Number, Boolean, Redacted }
public enum ValueUnit { None, Count, Bytes, Mebibytes, Gibibytes, Megahertz }
public enum DataSensitivity { Public, Identifier, Private }

public sealed record FindingValue
{
    public ValueKind Kind { get; init; } = ValueKind.Unknown;
    public string? Text { get; init; }
    public decimal? Number { get; init; }
    public bool? Boolean { get; init; }
    public ValueUnit Unit { get; init; }

    public static FindingValue FromText(string value) => new() { Kind = ValueKind.Text, Text = value };
    public static FindingValue FromNumber(decimal value, ValueUnit unit = ValueUnit.None) =>
        new() { Kind = ValueKind.Number, Number = value, Unit = unit };
    public static FindingValue FromBoolean(bool value) => new() { Kind = ValueKind.Boolean, Boolean = value };
    public static FindingValue Redacted() => new() { Kind = ValueKind.Redacted };

    public bool IsWellFormed() => Kind switch
    {
        ValueKind.Text => Text is not null && Number is null && Boolean is null && Unit == ValueUnit.None,
        ValueKind.Number => Number.HasValue && Text is null && Boolean is null,
        ValueKind.Boolean => Boolean.HasValue && Text is null && Number is null && Unit == ValueUnit.None,
        ValueKind.Unknown or ValueKind.Redacted => Text is null && Number is null && Boolean is null && Unit == ValueUnit.None,
        _ => false
    };

    public override string ToString() => Kind switch
    {
        ValueKind.Text => Text ?? "",
        ValueKind.Number => Number?.ToString("0.##", CultureInfo.InvariantCulture) +
            (Unit == ValueUnit.None ? "" : $" {Unit}"),
        ValueKind.Boolean => Boolean == true ? "true" : "false",
        ValueKind.Redacted => "[redacted]",
        _ => "[unknown]"
    };
}

public sealed record Finding
{
    public required string Id { get; init; }
    public required string Category { get; init; }
    public required string Source { get; init; }
    public DateTimeOffset ObservedAt { get; init; }
    public CollectionStatus Status { get; init; }
    public FindingValue Value { get; init; } = new();
    public DataSensitivity Sensitivity { get; init; }
    public string Explanation { get; init; } = "";
}

public sealed record SystemSummary
{
    public string? Os { get; init; }
    public string? Build { get; init; }
    public string? Architecture { get; init; }
}

public sealed record EvidenceSummary
{
    public bool? HypervisorDetected { get; init; }
    public string VmwareGuestEvidence { get; init; } = "inconclusive";
    public string Explanation { get; init; } = "Evidence has not been evaluated.";
}

public sealed record VmxSummary
{
    public IReadOnlyDictionary<string, string> Declarations { get; init; } = new Dictionary<string, string>();
    public string PowerState { get; init; } = "unknown";
    public string StateSource { get; init; } = "notQueried";
    public IReadOnlyList<string> Limitations { get; init; } = [];
}

public sealed record ReportDiagnostic
{
    public required string Code { get; init; }
    public string Message { get; init; } = "";
}

public sealed record DiagnosticReport
{
    public const string CurrentSchemaVersion = "1.0";
    [JsonRequired]
    public string SchemaVersion { get; init; } = CurrentSchemaVersion;
    public string AppVersion { get; init; } = BuildInfo.Version;
    [JsonRequired]
    public DateTimeOffset CollectedAt { get; init; }
    [JsonRequired]
    public string PrivacyMode { get; init; } = "redacted";
    [JsonRequired]
    public SystemSummary System { get; init; } = new();
    [JsonRequired]
    public IReadOnlyList<Finding> Findings { get; init; } = [];
    public EvidenceSummary VirtualizationEvidence { get; init; } = new();
    public VmxSummary? VmxSummary { get; init; }
    public IReadOnlyList<ReportDiagnostic> Diagnostics { get; init; } = [];
}
