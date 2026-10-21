using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using VMIdentityLab.Core.Evidence;
using VMIdentityLab.Core.Reports;

namespace VMIdentityLab.Core.Comparison;

public enum ValidationPhase { Before, After, Restored }
public enum ValidationField { StaticMac, BiosUuid }
public enum ValidationDataKind { Captured, Synthetic, LegacyReport }
public enum ValidationState { Passed, Failed, NotComparable, NotProvided }
public enum ValidationReason
{
    Matches, ValueUnchanged, ValueDiffers, Missing, SourceMismatch, VersionMismatch, SessionMismatch,
    PhaseMismatch, TimeOrder, Unavailable, Redacted, InvalidIdentity, MultipleAdapters, OtherIdentityChanged,
    PublicFieldChanged, Elevated, ReceiptMismatch, RecordedEvidence, NotTested
}

// Explicit private input, never a diagnostic report or a public export model.
public sealed record PrivateValidationSnapshot
{
    public const string Schema = "vmil.private-identity-snapshot/1";
    [JsonRequired] public string SchemaVersion { get; init; } = Schema;
    [JsonRequired] public Guid SessionId { get; init; }
    [JsonRequired] public ValidationPhase Phase { get; init; }
    [JsonRequired] public ValidationDataKind DataKind { get; init; }
    [JsonRequired] public bool? Elevated { get; init; }
    // Optional private observation scope; never copied to public results.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Guid? NetworkAdapterId { get; init; }
    [JsonRequired] public DiagnosticReport Report { get; init; } = null!;
    [JsonRequired] public IReadOnlyList<Finding> Identities { get; init; } = [];
    public override string ToString() => "Private identity validation snapshot";
}

// Supplied by the VMware adapter after matching two existing recovery records.
// Values are private and normalized to the Windows observations, including SMBIOS UUID byte order.
public sealed record ValidationConfiguration(ValidationField Field, Version WorkstationVersion,
    DateTimeOffset AppliedAt, DateTimeOffset RestoredAt, string BeforeMac, string AfterMac,
    string BeforeUuid, string AfterUuid)
{
    public override string ToString() => "Private configuration validation evidence";
}

public sealed record ValidationCheck(string Check, ValidationState State, ValidationReason Reason,
    CollectionStatus? BeforeStatus = null, CollectionStatus? AfterStatus = null, CollectionStatus? RestoredStatus = null);
public sealed record ValidationProgramObservation(ValidationPhase Phase, DateTimeOffset CollectedAt,
    string VmwareGuestEvidence, bool? HypervisorDetected);
// No input reports, paths, raw identities, free-form messages or identity hashes in this model.
public sealed record IdentityValidationResult(string SchemaVersion, string PrivacyMode, string ToolVersion,
    ValidationField Field, ValidationDataKind DataKind, string? WorkstationVersion,
    IReadOnlyList<ValidationCheck> Configuration, IReadOnlyList<ValidationCheck> Guest,
    IReadOnlyList<ValidationProgramObservation> Program);

public static class IdentityValidationComparer
{
    public static IdentityValidationResult Compare(ValidationField field, PrivateValidationSnapshot before,
        PrivateValidationSnapshot after, PrivateValidationSnapshot restored, ValidationConfiguration? configuration = null)
    {
        if (!Enum.IsDefined(field)) throw new ArgumentException("Unknown validation field.");
        var stages = new[] { before, after, restored };
        foreach (var stage in stages) ValidationSnapshotJson.Validate(stage);
        var guest = new List<ValidationCheck>();
        ValidationReason? context = stages.Select(item => item.Phase).SequenceEqual(Enum.GetValues<ValidationPhase>()) ? null : ValidationReason.PhaseMismatch;
        if (context is null && (before.Report.CollectedAt >= after.Report.CollectedAt || after.Report.CollectedAt >= restored.Report.CollectedAt)) context = ValidationReason.TimeOrder;
        if (context is null && (stages.Any(item => !KnownVersion(item.Report.AppVersion))
            || stages.Select(item => item.Report.AppVersion).Distinct(StringComparer.Ordinal).Count() != 1)) context = ValidationReason.VersionMismatch;
        var reportContext = context;
        if (context is null && stages.Any(item => item.DataKind == ValidationDataKind.LegacyReport)) context = ValidationReason.Redacted;
        if (context is null && (before.SessionId == Guid.Empty || stages.Any(item => item.SessionId != before.SessionId || item.DataKind != before.DataKind))) context = ValidationReason.SessionMismatch;
        if (context is null && stages.Select(item => item.NetworkAdapterId).Distinct().Count() != 1) context = ValidationReason.SourceMismatch;
        if (context is null && stages.Select((stage, index) => stage.Identities.Any(item => item.ObservedAt < stage.Report.CollectedAt
            || index < 2 && item.ObservedAt >= stages[index + 1].Report.CollectedAt)).Any(value => value)) context = ValidationReason.TimeOrder;
        guest.Add(new("context", context is null ? ValidationState.Passed : ValidationState.NotComparable, context ?? ValidationReason.Matches));

        var selectedId = field == ValidationField.StaticMac ? "network.macAddress" : "smbios.uuid";
        var otherId = field == ValidationField.StaticMac ? "smbios.uuid" : "network.macAddress";
        var selected = Read(stages, selectedId, context);
        var other = Read(stages, otherId, context);
        ValidationCheck Check(string name, (string[]? Values, ValidationReason? Error, Finding?[] Findings) data, bool success, ValidationReason failure)
            => new(name, data.Error is not null ? ValidationState.NotComparable : success ? ValidationState.Passed : ValidationState.Failed,
                data.Error ?? (success ? ValidationReason.Matches : failure), data.Findings[0]?.Status, data.Findings[1]?.Status, data.Findings[2]?.Status);
        guest.Add(Check("changed", selected, selected.Values is { } s && s[0] != s[1], ValidationReason.ValueUnchanged));
        guest.Add(Check("restored", selected, selected.Values is { } r && r[0] == r[2], ValidationReason.ValueDiffers));
        guest.Add(Check("otherIdentity", other, other.Values is { } o && o[0] == o[1] && o[0] == o[2], ValidationReason.OtherIdentityChanged));
        guest.Add(new("ordinaryUser", stages.Any(item => item.Elevated is null) ? ValidationState.NotComparable
            : stages.Any(item => item.Elevated == true) ? ValidationState.Failed : ValidationState.Passed,
            stages.Any(item => item.Elevated is null) ? ValidationReason.Missing : stages.Any(item => item.Elevated == true) ? ValidationReason.Elevated : ValidationReason.Matches));
        var changedReport = ReportComparer.Compare(before.Report, after.Report).ToDictionary(item => item.Id, StringComparer.Ordinal);
        var restoredReport = ReportComparer.Compare(before.Report, restored.Report).ToDictionary(item => item.Id, StringComparer.Ordinal);
        foreach (var id in FindingCatalog.ById.Values.Where(item => item.Sensitivity == DataSensitivity.Public).Select(item => item.Id).Order(StringComparer.Ordinal))
        {
            var rows = new[] { changedReport.GetValueOrDefault(id), restoredReport.GetValueOrDefault(id) };
            var observations = stages.Select(stage => stage.Report.Findings.FirstOrDefault(item => item.Id == id)).ToArray();
            var missing = observations.Any(item => item is null);
            var sourceMatches = !missing && observations.Select(item => item!.Source).Distinct(StringComparer.Ordinal).Count() == 1;
            var state = reportContext is not null || !sourceMatches || rows.Any(item => item is null || item.Kind == ComparisonKind.NotComparable)
                ? ValidationState.NotComparable : rows.Any(item => item!.Kind != ComparisonKind.Same) ? ValidationState.Failed : ValidationState.Passed;
            guest.Add(new(id, state, reportContext ?? (missing ? ValidationReason.Missing : !sourceMatches ? ValidationReason.SourceMismatch : state == ValidationState.NotComparable
                ? ValidationReason.Unavailable : state == ValidationState.Failed ? ValidationReason.PublicFieldChanged : ValidationReason.Matches),
                observations[0]?.Status, observations[1]?.Status, observations[2]?.Status));
        }
        guest.Add(new("networkAvailability", ValidationState.NotProvided, ValidationReason.NotTested));

        var config = new List<ValidationCheck>();
        if (configuration is null) config.Add(new("configuration", ValidationState.NotProvided, ValidationReason.Missing));
        else
        {
            var supported = configuration.WorkstationVersion == new Version(17, 5, 2, 23775571) && configuration.Field == field;
            config.Add(new("configuration", supported ? ValidationState.Passed : ValidationState.Failed, supported ? ValidationReason.RecordedEvidence : ValidationReason.ReceiptMismatch));
            var chronological = before.Report.CollectedAt < configuration.AppliedAt && configuration.AppliedAt < after.Report.CollectedAt
                && after.Report.CollectedAt < configuration.RestoredAt && configuration.RestoredAt < restored.Report.CollectedAt
                && before.Identities.All(item => item.ObservedAt < configuration.AppliedAt)
                && after.Identities.All(item => item.ObservedAt < configuration.RestoredAt);
            config.Add(new("receiptTime", chronological ? ValidationState.Passed : ValidationState.Failed, chronological ? ValidationReason.Matches : ValidationReason.TimeOrder));
            var mac = Read(stages, "network.macAddress", context);
            var uuid = Read(stages, "smbios.uuid", context);
            var matches = mac.Values is { } m && uuid.Values is { } u
                && m.SequenceEqual(new[] { configuration.BeforeMac, configuration.AfterMac, configuration.BeforeMac }.Select(NormalizeMac))
                && u.SequenceEqual(new[] { configuration.BeforeUuid, configuration.AfterUuid, configuration.BeforeUuid }.Select(NormalizeUuid));
            config.Add(new("guestMatchesReceipts", mac.Error is not null || uuid.Error is not null ? ValidationState.NotComparable
                : matches && supported ? ValidationState.Passed : ValidationState.Failed, mac.Error ?? uuid.Error ?? (matches && supported ? ValidationReason.Matches : ValidationReason.ReceiptMismatch)));
        }
        var program = stages.Select(stage =>
        {
            var evidence = EvidenceEvaluator.Evaluate(ReportPrivacy.Apply(stage.Report).Findings);
            return new ValidationProgramObservation(stage.Phase, stage.Report.CollectedAt, evidence.VmwareGuestEvidence, evidence.HypervisorDetected);
        }).ToArray();
        return new("vmil.identity-validation/1", "redacted", BuildInfo.Version, field,
            stages.Any(item => item.DataKind == ValidationDataKind.Synthetic) ? ValidationDataKind.Synthetic
                : stages.Any(item => item.DataKind == ValidationDataKind.LegacyReport) ? ValidationDataKind.LegacyReport : ValidationDataKind.Captured,
            configuration?.WorkstationVersion == new Version(17, 5, 2, 23775571) ? configuration.WorkstationVersion.ToString() : null, config, guest, program);
    }

    private static (string[]? Values, ValidationReason? Error, Finding?[] Findings) Read(PrivateValidationSnapshot[] stages, string id, ValidationReason? context)
    {
        var findings = stages.Select(stage => stage.Identities.FirstOrDefault(item => item.Id == id)).ToArray();
        if (context is not null) return (null, context, findings);
        if (findings.Any(item => item is null)) return (null, ValidationReason.Missing, findings);
        if (findings.Any(item => item!.Source != FindingCatalog.ById[id].Source)) return (null, ValidationReason.SourceMismatch, findings);
        if (findings.Any(item => item!.Status != CollectionStatus.Success)) return (null, ValidationReason.Unavailable, findings);
        if (findings.Any(item => item!.Value.Kind == ValueKind.Unknown)) return (null, ValidationReason.Unavailable, findings);
        if (findings.Any(item => item!.Value.Kind == ValueKind.Redacted)) return (null, ValidationReason.Redacted, findings);
        if (findings.Any(item => item!.Value.Kind != ValueKind.Text)) return (null, ValidationReason.InvalidIdentity, findings);
        if (id == "network.macAddress" && findings.Any(item => item!.Value.Text!.Contains(';'))) return (null, ValidationReason.MultipleAdapters, findings);
        var values = findings.Select(item => id == "network.macAddress" ? NormalizeMac(item!.Value.Text!) : NormalizeUuid(item!.Value.Text!)).ToArray();
        return values.Any(string.IsNullOrEmpty) ? (null, ValidationReason.InvalidIdentity, findings) : (values, null, findings);
    }

    private static string NormalizeMac(string value)
    {
        var normalized = value.Replace(":", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal).ToUpperInvariant();
        return normalized.Length == 12 && normalized.All(Uri.IsHexDigit) && normalized.Any(c => c != '0') ? normalized : "";
    }
    private static string NormalizeUuid(string value) => Guid.TryParse(value, out var uuid) && uuid != Guid.Empty
        && uuid != Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff") ? uuid.ToString("D") : "";
    private static bool KnownVersion(string? value) => !string.IsNullOrWhiteSpace(value)
        && Version.TryParse(value.Split('-')[0], out var parsed) && parsed.Build >= 0;

    public static string RenderHtml(IdentityValidationResult result)
    {
        static string E(object? value) => WebUtility.HtmlEncode(Convert.ToString(value, CultureInfo.InvariantCulture) ?? "");
        var html = new StringBuilder("<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><title>VMIL identity validation</title><body><h1>VMIL identity validation</h1>");
        html.Append("<p>Redacted comparison of imported evidence. No VM operation or independent authentication of imported files. Network availability is not tested.</p>");
        html.Append("<p>").Append(E(result.DataKind)).Append(" · ").Append(E(result.Field)).Append(" · Workstation ").Append(E(result.WorkstationVersion ?? "unknown")).Append("</p>");
        foreach (var layer in new[] { ("Configuration records", result.Configuration), ("Windows guest observations", result.Guest) })
        {
            html.Append("<h2>").Append(E(layer.Item1)).Append("</h2><table><tr><th>Check</th><th>State</th><th>Reason</th><th>Before</th><th>After</th><th>Restored</th></tr>");
            foreach (var check in layer.Item2) html.Append("<tr><td>").Append(E(check.Check)).Append("</td><td>").Append(E(check.State)).Append("</td><td>").Append(E(check.Reason))
                .Append("</td><td>").Append(E(check.BeforeStatus)).Append("</td><td>").Append(E(check.AfterStatus)).Append("</td><td>").Append(E(check.RestoredStatus)).Append("</td></tr>");
            html.Append("</table>");
        }
        html.Append("<h2>Program behavior: current VMIL evidence evaluator</h2><p>The current evaluator is applied to imported observations. A result applies only to this program and evidence. It does not prove a physical machine.</p><table>");
        foreach (var item in result.Program) html.Append("<tr><td>").Append(E(item.Phase)).Append("</td><td>").Append(E(item.CollectedAt.ToString("O")))
            .Append("</td><td>").Append(E(item.VmwareGuestEvidence)).Append("</td><td>").Append(E(item.HypervisorDetected)).Append("</td></tr>");
        return html.Append("</table></body></html>").ToString();
    }
}
