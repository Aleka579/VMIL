using System.Text.Json;
using System.Text.Json.Serialization;
using VMIdentityLab.Core.Experiments;
using VMIdentityLab.Core.Reports;

namespace VMIdentityLab.VMware.Configuration;

// An offline, non-executable plan. No proposed VMX bytes or user-supplied identity values exist here.
public sealed class IdentityExperimentPlan
{
    private readonly string targetPath;
    private readonly string contentHash;
    private readonly Version? version;
    private readonly PowerStateEvidence state;

    public Guid Id { get; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    public IdentityExperimentDefinition Definition { get; }
    public DeclarationState Before { get; }
    public int? HardwareVersion { get; }
    public IReadOnlyList<IdentityExperimentGap> Gaps { get; }
    public bool CanExecute => false;
    public string Status => "notExecuted";
    public string TargetVersion => version?.ToString() ?? "unknown";
    public VmPowerState PowerState => state.State;
    public DateTimeOffset PowerObservedAt => state.ObservedAt;

    internal IdentityExperimentPlan(string path, VmxDocument document, Version? targetVersion,
        PowerStateEvidence power, IdentityExperimentDefinition definition, DeclarationState before,
        int? hardwareVersion, List<IdentityExperimentGap> gaps)
    {
        targetPath = Path.GetFullPath(path);
        contentHash = document.ContentHash;
        version = targetVersion;
        state = power;
        Definition = definition;
        Before = before;
        HardwareVersion = hardwareVersion;
        Gaps = gaps.AsReadOnly();
    }

    public bool IsCurrent(string path, VmxDocument document, Version? targetVersion,
        PowerStateEvidence power, IdentityExperimentKind kind) =>
        string.Equals(targetPath, Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase)
        && contentHash == document.ContentHash && Equals(version, targetVersion)
        && state == power && Definition.Kind == kind;

    // A separate report format, not a DiagnosticReport or an importable transaction.
    // Only closed metadata, declaration states and typed version numbers are exported.
    public byte[] SerializeReport() => JsonSerializer.SerializeToUtf8Bytes(new
    {
        schemaVersion = "vmil.identity-plan/1", planId = Id, createdAt = CreatedAt,
        status = Status, canExecute = CanExecute, privacyMode = "redacted",
        target = new { product = "VMware Workstation", version = TargetVersion, hardwareVersion = HardwareVersion,
            powerState = state.State, stateObservedAt = state.ObservedAt, reliablePowerObservation = state.IsReliable },
        researchBaseline = IdentityExperimentCatalog.ResearchBaseline.ToString(),
        evidenceReviewedOn = IdentityExperimentCatalog.EvidenceReviewedOn,
        experiment = Definition, difference = new { fieldId = Definition.FieldId, before = Before, proposedIntent = Definition.Intent },
        gaps = Gaps.Select(gap => new { code = gap, description = IdentityExperimentCatalog.Describe(gap) })
    }, new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    });

    public Task SaveReportAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Select a JSON plan report path.", nameof(path));
        return ReportFiles.SaveBytesAsync(path, SerializeReport(), cancellationToken: cancellationToken);
    }
}

public static class IdentityExperimentPlanner
{
    public static IdentityExperimentPlan Create(string path, VmxDocument document, Version? version,
        PowerStateEvidence state, IdentityExperimentKind kind)
    {
        var definition = IdentityExperimentCatalog.Get(kind);
        var gaps = new List<IdentityExperimentGap>
        {
            IdentityExperimentGap.TargetVersionEvidenceMissing, IdentityExperimentGap.ValueConstraintsUnverified,
            IdentityExperimentGap.OfficialChannelUnverified, IdentityExperimentGap.RecoveryUnverified,
            IdentityExperimentGap.IndependentCopyTestMissing, IdentityExperimentGap.WriteNotApproved
        };
        if (version is null) gaps.Add(IdentityExperimentGap.TargetVersionUnknown);
        else if (version != IdentityExperimentCatalog.ResearchBaseline) gaps.Add(IdentityExperimentGap.OutsideResearchBaseline);
        if (state.State != VmPowerState.PoweredOff || !state.IsReliable) gaps.Add(IdentityExperimentGap.PowerOffUnconfirmed);
        if (document.HasAmbiguity) gaps.Add(IdentityExperimentGap.AmbiguousConfiguration);
        if (document.Limitations.Count != 0 || document.Entries.Any(item => item.Key.StartsWith("encryption.", StringComparison.OrdinalIgnoreCase)))
            gaps.Add(IdentityExperimentGap.UnsupportedConfiguration);

        var before = VmxIdentityReader.Read(document)[definition.FieldId].State;
        var entries = document.Entries.Where(item => kind == IdentityExperimentKind.UuidOnCopy
            ? item.Key.Equals("uuid.bios", StringComparison.OrdinalIgnoreCase) : VmxIdentityReader.IsMacKey(item.Key)).ToArray();
        if (entries.Length == 0) gaps.Add(IdentityExperimentGap.MissingDeclaration);
        else if (entries.Any(item => !HasIdentifierSyntax(item.Value, kind)))
        {
            before = DeclarationState.Invalid;
            gaps.Add(IdentityExperimentGap.InvalidDeclaration);
        }
        if (kind == IdentityExperimentKind.MacUniqueness && entries.Length > 1)
            gaps.Add(IdentityExperimentGap.AdapterScopeUnconfirmed);
        var hardware = document.Entries.Where(item => item.Key.Equals("virtualHW.version", StringComparison.OrdinalIgnoreCase)).ToArray();
        int? hardwareVersion = hardware.Length == 1 && int.TryParse(hardware[0].Value, out var parsed) && parsed > 0 ? parsed : null;
        return new(path, document, version, state, definition, before, hardwareVersion, gaps);
    }

    // Syntax/presence only. Passing this check does not validate vendor value constraints or an effective identity.
    private static bool HasIdentifierSyntax(string raw, IdentityExperimentKind kind)
    {
        var bytes = kind == IdentityExperimentKind.MacUniqueness ? 6 : 16;
        if (raw.Length != bytes * 3 - 1) return false;
        for (var index = 0; index < raw.Length; index++)
        {
            if (index % 3 == 2)
            {
                var expected = kind == IdentityExperimentKind.MacUniqueness ? ':' : index == 23 ? '-' : ' ';
                if (raw[index] != expected) return false;
            }
            else if (!Uri.IsHexDigit(raw[index])) return false;
        }
        var compact = string.Concat(raw.Where(Uri.IsHexDigit));
        if (compact.All(character => character == '0')) return false;
        return kind != IdentityExperimentKind.MacUniqueness || (Convert.ToByte(compact[..2], 16) & 1) == 0;
    }
}
