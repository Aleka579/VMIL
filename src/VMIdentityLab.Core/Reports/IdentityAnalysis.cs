using System.Globalization;

namespace VMIdentityLab.Core.Reports;

public enum IdentityWriteSupport { NotSupported, ExistingLimitedPolicy, ResearchCandidate }
public enum IdentityBasis { NoReviewedWriteEvidence, V02ExactRange, MacHistoricalKb, UuidWorkflowKb }
public enum DeclarationState { NotApplicable, NoSnapshot, NotIncluded, Present, Redacted, Ambiguous, Invalid }
public sealed record IdentityDeclaration(DeclarationState State, FindingValue Value, string Source = "");
public sealed record IdentityAnalysisRow(FindingDefinition Definition, Finding? Observation,
    IdentityDeclaration Declaration, IdentityWriteSupport WriteSupport, IdentityBasis Basis);

public static class IdentityAnalyzer
{
    // These are related declarations, not proof that the report describes the selected VM.
    private static readonly (string Id, string Key, ValueUnit Unit)[] Mappings =
    [
        ("cpu.logicalProcessors", "numvcpus", ValueUnit.Count),
        ("system.memoryBytes", "memsize", ValueUnit.Mebibytes),
        ("system.os", "guestOS", ValueUnit.None)
    ];

    public static IReadOnlyList<IdentityAnalysisRow> Analyze(DiagnosticReport? report,
        IReadOnlyDictionary<string, IdentityDeclaration>? selectedDeclarations = null)
    {
        if (report is not null) ReportJson.Validate(report);
        var safe = report is null ? null : ReportPrivacy.Apply(report);
        var observations = safe?.Findings.ToDictionary(item => item.Id, StringComparer.Ordinal) ?? [];
        var declarations = selectedDeclarations ?? ReadDeclarations(safe?.VmxSummary);
        return FindingCatalog.ById.Values.OrderBy(item => item.Category, StringComparer.Ordinal)
            .ThenBy(item => item.Id, StringComparer.Ordinal).Select(definition =>
            {
                var declaration = declarations.GetValueOrDefault(definition.Id,
                    new(DeclarationState.NotApplicable, new()));
                if (definition.Sensitivity != DataSensitivity.Public)
                    declaration = declaration with
                    {
                        Value = declaration.State is DeclarationState.Present or DeclarationState.Redacted ? FindingValue.Redacted() : new(),
                        State = declaration.State == DeclarationState.Present ? DeclarationState.Redacted : declaration.State
                    };
                var (support, basis) = definition.Id switch
                {
                    "cpu.logicalProcessors" or "system.memoryBytes" => (IdentityWriteSupport.ExistingLimitedPolicy, IdentityBasis.V02ExactRange),
                    "network.macAddress" => (IdentityWriteSupport.ResearchCandidate, IdentityBasis.MacHistoricalKb),
                    "smbios.uuid" => (IdentityWriteSupport.ResearchCandidate, IdentityBasis.UuidWorkflowKb),
                    _ => (IdentityWriteSupport.NotSupported, IdentityBasis.NoReviewedWriteEvidence)
                };
                return new IdentityAnalysisRow(definition, observations.GetValueOrDefault(definition.Id), declaration, support, basis);
            }).ToArray();
    }

    public static IReadOnlyDictionary<string, IdentityDeclaration> ReadDeclarations(VmxSummary? summary)
    {
        var result = new Dictionary<string, IdentityDeclaration>(StringComparer.Ordinal);
        foreach (var (id, key, unit) in Mappings)
        {
            var state = summary is null ? DeclarationState.NoSnapshot : DeclarationState.NotIncluded;
            FindingValue value = new();
            var raw = summary?.Declarations.FirstOrDefault(pair => pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
            if (raw is not null)
            {
                state = DeclarationState.Present;
                if (unit == ValueUnit.None) value = FindingValue.FromText(ReportPrivacy.Text(raw));
                else if (decimal.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number > 0)
                    value = FindingValue.FromNumber(number, unit);
                else state = DeclarationState.Invalid;
            }
            result[id] = new(state, value, "VMX: " + key);
        }
        foreach (var id in new[] { "smbios.uuid", "network.macAddress" })
            result[id] = new(summary is null ? DeclarationState.NoSnapshot : DeclarationState.NotIncluded, new());
        return result;
    }
}
