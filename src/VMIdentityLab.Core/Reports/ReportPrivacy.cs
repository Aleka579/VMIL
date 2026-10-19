using System.Text.RegularExpressions;
using VMIdentityLab.Core.Evidence;

namespace VMIdentityLab.Core.Reports;

public static partial class ReportPrivacy
{
    public static readonly IReadOnlySet<string> PublicVmxKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { "virtualHW.version", "guestOS", "firmware", "memsize", "numvcpus" };

    public static DiagnosticReport Apply(DiagnosticReport report)
    {
        var findings = new List<Finding>();
        foreach (var finding in report.Findings)
        {
            if (!FindingCatalog.ById.TryGetValue(finding.Id, out var definition))
                continue; // Unknown fields have no reviewed public-data contract.
            var sensitivity = (DataSensitivity)Math.Max((int)finding.Sensitivity, (int)definition.Sensitivity);
            var value = sensitivity != DataSensitivity.Public
                ? FindingValue.Redacted()
                : finding.Value.Kind == ValueKind.Text
                    ? FindingValue.FromText(Text(finding.Value.Text)) : finding.Value;
            findings.Add(finding with
            {
                Category = definition.Category,
                Source = ReviewedSource(finding, definition),
                Sensitivity = sensitivity,
                Value = value,
                Explanation = StatusExplanation(finding.Status)
            });
        }

        VmxSummary? vmx = report.VmxSummary is null ? null : report.VmxSummary with
        {
            Declarations = report.VmxSummary.Declarations
                .Where(pair => PublicVmxKeys.Contains(pair.Key))
                .ToDictionary(pair => pair.Key, pair => Text(pair.Value), StringComparer.OrdinalIgnoreCase),
            StateSource = StateSource(report.VmxSummary.StateSource),
            Limitations = report.VmxSummary.Limitations.Select(_ => "Configuration or state requires verification.").Distinct().ToArray()
        };

        var diagnostics = report.Diagnostics.Select(item => new ReportDiagnostic
        {
            Code = DiagnosticCode(item.Code),
            Message = "A collection or input limitation was recorded; inspect the item status."
        }).Distinct().ToList();
        if (findings.Count != report.Findings.Count)
            diagnostics.Add(new() { Code = "report.unknownFieldsOmitted", Message = "Unreviewed fields were omitted from the public report." });

        return report with
        {
            PrivacyMode = "redacted",
            AppVersion = Text(report.AppVersion),
            System = new() { Os = Text(report.System.Os), Build = Text(report.System.Build), Architecture = Text(report.System.Architecture) },
            Findings = findings,
            VmxSummary = vmx,
            VirtualizationEvidence = EvidenceEvaluator.Evaluate(findings),
            Diagnostics = diagnostics
        };
    }

    public static string Text(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var text = value.Length > 4096 ? value[..4096] : value;
        text = WindowsPath().Replace(text, "[path]");
        text = UncPath().Replace(text, "[path]");
        text = Identifier().Replace(text, "[identifier]");
        return text.Length > 512 ? text[..512] : text;
    }

    private static string StateSource(string value) => value is "notQueried" or "vmrun.list" or "vmrest.power" or "unavailable" ? value : "unverified";
    private static string ReviewedSource(Finding finding, FindingDefinition definition)
    {
        // Legacy identifier reports did not identify a collector. Do not relabel them as new WMI/.NET data.
        if (definition.Sensitivity == DataSensitivity.Identifier)
            return finding.Source == definition.Source || finding.Source == "identifier" ? finding.Source : "unverified";
        return finding.Id == "cpu.model" && finding.Source == "Win32_Processor.Name" ? finding.Source : definition.Source;
    }
    private static string DiagnosticCode(string value) => value is "collection.partial" or "vmx.unavailable"
        or "vmx.ambiguous" or "vmx.encoding" or "vmx.stateUnknown" or "report.unknownFieldsOmitted" ? value : "diagnostic.unknown";

    private static string StatusExplanation(CollectionStatus status) => status switch
    {
        CollectionStatus.Success => "接口采集成功；该值本身不构成单独的虚拟机身份结论。",
        CollectionStatus.AccessDenied => "访问被拒绝，结果不可用。",
        CollectionStatus.NotSupported => "当前接口或字段不可用。",
        CollectionStatus.Timeout => "接口超时，结果不可用。",
        _ => "接口失败，结果不可用。"
    };

    [GeneratedRegex(@"[A-Za-z]:[\\/][^\r\n\""<>]*", RegexOptions.CultureInvariant, 200)]
    private static partial Regex WindowsPath();
    [GeneratedRegex(@"\\\\[^\s\""<>]+", RegexOptions.CultureInvariant, 200)]
    private static partial Regex UncPath();
    [GeneratedRegex(@"\b(?:[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}|(?:[0-9a-f]{2}[:-]){5}[0-9a-f]{2})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 200)]
    private static partial Regex Identifier();
}
