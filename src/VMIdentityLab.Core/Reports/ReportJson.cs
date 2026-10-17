using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace VMIdentityLab.Core.Reports;

public sealed class ReportFormatException(string message) : Exception(message);

public static partial class ReportJson
{
    public const int MaxReportBytes = 5 * 1024 * 1024;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        MaxDepth = 32,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };

    public static byte[] Serialize(DiagnosticReport report)
    {
        Validate(report);
        return JsonSerializer.SerializeToUtf8Bytes(ReportPrivacy.Apply(report), Options);
    }

    public static DiagnosticReport Deserialize(ReadOnlySpan<byte> utf8)
    {
        if (utf8.Length > MaxReportBytes) throw new ReportFormatException("Report exceeds the 5 MiB limit.");
        try
        {
            var report = JsonSerializer.Deserialize<DiagnosticReport>(utf8, Options)
                ?? throw new ReportFormatException("Report must be an object.");
            Validate(report);
            return ReportPrivacy.Apply(report);
        }
        catch (JsonException)
        {
            throw new ReportFormatException("Invalid report structure or value; raw input is not included in this error.");
        }
    }

    public static async Task<DiagnosticReport> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, true);
        if (file.Length > MaxReportBytes) throw new ReportFormatException("Report exceeds the 5 MiB limit.");
        using var memory = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await file.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (memory.Length + count > MaxReportBytes) throw new ReportFormatException("Report exceeds the 5 MiB limit.");
            memory.Write(buffer, 0, count);
        }
        return Deserialize(memory.ToArray());
    }

    internal static void Validate(DiagnosticReport report)
    {
        if (report.SchemaVersion != DiagnosticReport.CurrentSchemaVersion)
            throw new ReportFormatException("Unsupported report schema version.");
        if (report.PrivacyMode != "redacted" || report.CollectedAt == default || report.System is null
            || report.Findings is null || report.VirtualizationEvidence is null || report.Diagnostics is null)
            throw new ReportFormatException("Required report fields are missing or unsupported.");
        if (report.Findings.Count > 500 || report.Diagnostics.Count > 64)
            throw new ReportFormatException("Report contains too many items.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var finding in report.Findings)
        {
            if (finding is null || finding.Id is null || !FindingId().IsMatch(finding.Id) || !ids.Add(finding.Id)
                || finding.Category is null || finding.Source is null || finding.Explanation is null
                || finding.ObservedAt == default || finding.Value is null || !finding.Value.IsWellFormed()
                || !Enum.IsDefined(finding.Status) || !Enum.IsDefined(finding.Sensitivity) || !Enum.IsDefined(finding.Value.Unit)
                || finding.Value.Text?.Length > 4096 || finding.Source.Length > 512 || finding.Explanation.Length > 4096)
                throw new ReportFormatException("Invalid or duplicate finding.");
        }
        if (report.Diagnostics.Any(item => item is null || item.Code is null || item.Message is null))
            throw new ReportFormatException("Invalid diagnostic.");
        if (report.VmxSummary is { } vmx && (vmx.Declarations is null || vmx.Limitations is null
            || vmx.StateSource is null || vmx.Declarations.Count > 100 || vmx.Declarations.Any(pair => pair.Value is null)
            || vmx.Declarations.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != vmx.Declarations.Count
            || vmx.PowerState is not ("poweredOn" or "poweredOff" or "suspended" or "unknown")))
            throw new ReportFormatException("Invalid VMX summary.");
        if (report.VirtualizationEvidence.VmwareGuestEvidence is not ("consistent" or "inconclusive" or "notObserved"))
            throw new ReportFormatException("Invalid evidence interpretation.");
    }

    [GeneratedRegex(@"^[a-z][a-zA-Z0-9._-]{0,79}$", RegexOptions.CultureInvariant, 200)]
    private static partial Regex FindingId();
}
