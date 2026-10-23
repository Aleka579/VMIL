using System.Text;
using System.Text.Json;
using VMIdentityLab.Core.Experiments;
using VMIdentityLab.Core.Reports;
using VMIdentityLab.VMware;
using VMIdentityLab.VMware.Configuration;

namespace VMIdentityLab.UnitTests;

public sealed class IdentityExperimentTests
{
    private const string Mac = "00:50:56:00:00:01";
    private const string Uuid = "00 11 22 33 44 55 66 77-88 99 aa bb cc dd ee ff";
    private const string Text = "virtualHW.version = \"19\"\nethernet0.address = \"" + Mac + "\"\nuuid.bios = \"" + Uuid + "\"\nunknown.setting = \"keep\"\n";
    private static readonly string Target = Path.Combine(Path.GetTempPath(), "VMIL-private-synthetic-target.vmx");
    private static readonly Version Version = IdentityExperimentCatalog.ResearchBaseline;
    private static readonly PowerStateEvidence Off = new(VmPowerState.PoweredOff, "synthetic", DateTimeOffset.UnixEpoch, true, "Synthetic observation");
    private static VmxDocument Document(string text = Text) => VmxDocument.Parse(Encoding.UTF8.GetBytes(text));
    private static IdentityExperimentPlan Plan(IdentityExperimentKind kind = IdentityExperimentKind.MacUniqueness) =>
        IdentityExperimentPlanner.Create(Target, Document(), Version, Off, kind);

    [Theory]
    [InlineData(IdentityExperimentKind.MacUniqueness, "network.macAddress", "#page=238")]
    [InlineData(IdentityExperimentKind.UuidOnCopy, "smbios.uuid", "#page=160")]
    public void RegisteredCandidatesHaveTraceableEvidenceAndStayBlockedEvenWhenPoweredOff(
        IdentityExperimentKind kind, string field, string reference)
    {
        var plan = Plan(kind);
        Assert.Equal(field, plan.Definition.FieldId);
        Assert.Contains(reference, plan.Definition.OfficialReference.ToString(), StringComparison.Ordinal);
        Assert.Equal(DeclarationState.Redacted, plan.Before);
        Assert.Equal(19, plan.HardwareVersion);
        Assert.False(plan.CanExecute);
        Assert.Equal("notExecuted", plan.Status);
        Assert.Contains(IdentityExperimentGap.TargetVersionEvidenceMissing, plan.Gaps);
        Assert.Contains(IdentityExperimentGap.ValueConstraintsUnverified, plan.Gaps);
        Assert.Contains(IdentityExperimentGap.OfficialChannelUnverified, plan.Gaps);
        Assert.Contains(IdentityExperimentGap.RecoveryUnverified, plan.Gaps);
        Assert.Contains(IdentityExperimentGap.IndependentCopyTestMissing, plan.Gaps);
        Assert.Contains(IdentityExperimentGap.WriteNotApproved, plan.Gaps);
        foreach (var description in new[] { plan.Definition.Title, plan.Definition.Intent, plan.Definition.ValueConstraints,
            plan.Definition.Risk, plan.Definition.Recovery, plan.Definition.EvidenceScope })
        {
            Assert.NotEmpty(description.Chinese);
            Assert.NotEmpty(description.English);
        }
    }

    [Fact]
    public void UnknownProfilesCannotBecomeArbitraryParameterRequests()
    {
        Assert.Throws<ArgumentException>(() => IdentityExperimentPlanner.Create(Target, Document(), Version, Off, (IdentityExperimentKind)999));
        Assert.Throws<ArgumentException>(() => VmxConfigPlanner.Create(Document(), new Dictionary<string, string> { ["uuid.bios"] = Uuid }));
        Assert.Throws<ArgumentException>(() => VmxConfigPlanner.Create(Document(), new Dictionary<string, string> { ["ethernet0.address"] = Mac }));
        Assert.False(new ReleasedVmxApiWritePolicy().IsApproved(Version, new("uuid.bios", "old", "new", "")));
        Assert.False(new ReleasedVmxApiWritePolicy().IsApproved(Version, new("ethernet0.address", "old", "new", "")));
    }

    [Theory]
    [InlineData(null, IdentityExperimentGap.TargetVersionUnknown)]
    [InlineData("17.5.2", IdentityExperimentGap.OutsideResearchBaseline)]
    [InlineData("17.5.2.23775572", IdentityExperimentGap.OutsideResearchBaseline)]
    [InlineData("25.0.0.1", IdentityExperimentGap.OutsideResearchBaseline)]
    public void MissingOrMismatchedVersionsDoNotExpandTheResearchScope(string? version, IdentityExperimentGap gap)
    {
        var plan = IdentityExperimentPlanner.Create(Target, Document(), version is null ? null : System.Version.Parse(version), Off, IdentityExperimentKind.MacUniqueness);
        Assert.Contains(gap, plan.Gaps);
        Assert.False(plan.CanExecute);
    }

    [Theory]
    [InlineData(VmPowerState.Unknown, false)]
    [InlineData(VmPowerState.PoweredOn, true)]
    [InlineData(VmPowerState.Suspended, true)]
    [InlineData(VmPowerState.PoweredOff, false)]
    public void UnconfirmedOffStateIsAnExplicitGap(VmPowerState state, bool reliable)
    {
        var plan = IdentityExperimentPlanner.Create(Target, Document(), Version, Off with { State = state, IsReliable = reliable }, IdentityExperimentKind.UuidOnCopy);
        Assert.Contains(IdentityExperimentGap.PowerOffUnconfirmed, plan.Gaps);
        Assert.False(plan.CanExecute);
    }

    [Theory]
    [InlineData("ethernet0.address = \"garbage\"", IdentityExperimentKind.MacUniqueness)]
    [InlineData("ethernet0.address = \"00:00:00:00:00:00\"", IdentityExperimentKind.MacUniqueness)]
    [InlineData("ethernet0.address = \"ff:ff:ff:ff:ff:ff\"", IdentityExperimentKind.MacUniqueness)]
    [InlineData("ethernet0.address = \"00:50:56:00:00:0Z\"", IdentityExperimentKind.MacUniqueness)]
    [InlineData("uuid.bios = \"11111111-2222-3333-4444-555555555555\"", IdentityExperimentKind.UuidOnCopy)]
    [InlineData("uuid.bios = \"00 00 00 00 00 00 00 00-00 00 00 00 00 00 00 00\"", IdentityExperimentKind.UuidOnCopy)]
    public void UnconfirmedIdentifierSyntaxDoesNotProduceAValidDeclaration(string text, IdentityExperimentKind kind)
    {
        var plan = IdentityExperimentPlanner.Create(Target, Document(text + "\n"), Version, Off, kind);
        Assert.Equal(DeclarationState.Invalid, plan.Before);
        Assert.Contains(IdentityExperimentGap.InvalidDeclaration, plan.Gaps);
        Assert.False(plan.CanExecute);
    }

    [Fact]
    public void MissingAndMultipleAdapterDeclarationsAreNotInferredAsAnEffectiveAddress()
    {
        var missing = IdentityExperimentPlanner.Create(Target, Document("virtualHW.version = \"19\"\n"), Version, Off, IdentityExperimentKind.MacUniqueness);
        Assert.Equal(DeclarationState.NotIncluded, missing.Before);
        Assert.Contains(IdentityExperimentGap.MissingDeclaration, missing.Gaps);
        var multiple = IdentityExperimentPlanner.Create(Target, Document(Text + "ethernet1.generatedAddress = \"00:50:56:00:00:02\"\n"), Version, Off, IdentityExperimentKind.MacUniqueness);
        Assert.Contains(IdentityExperimentGap.AdapterScopeUnconfirmed, multiple.Gaps);
        var duplicate = IdentityExperimentPlanner.Create(Target, Document(Text + "ETHERNET0.ADDRESS = \"" + Mac + "\"\n"), Version, Off, IdentityExperimentKind.MacUniqueness);
        Assert.Equal(DeclarationState.Ambiguous, duplicate.Before);
        Assert.Contains(IdentityExperimentGap.AmbiguousConfiguration, duplicate.Gaps);
    }

    [Theory]
    [InlineData("unknown broken syntax\n", IdentityExperimentGap.AmbiguousConfiguration)]
    [InlineData("encryption.keySafe = \"synthetic-opaque\"\n", IdentityExperimentGap.UnsupportedConfiguration)]
    [InlineData(".encoding = \"unsupported\"\n", IdentityExperimentGap.UnsupportedConfiguration)]
    public void UnsupportedInputOnlyProducesBlockedResearchInformation(string extra, IdentityExperimentGap expected)
    {
        var plan = IdentityExperimentPlanner.Create(Target, Document(extra + Text), Version, Off, IdentityExperimentKind.UuidOnCopy);
        Assert.Contains(expected, plan.Gaps);
        Assert.False(plan.CanExecute);
    }

    [Fact]
    public void TargetContentVersionStateObservationAndSelectionAllBindThePreview()
    {
        var plan = Plan();
        Assert.True(plan.IsCurrent(Target, Document(), Version, Off, IdentityExperimentKind.MacUniqueness));
        Assert.False(plan.IsCurrent(Target + ".other.vmx", Document(), Version, Off, IdentityExperimentKind.MacUniqueness));
        Assert.False(plan.IsCurrent(Target, Document(Text + "# external edit\n"), Version, Off, IdentityExperimentKind.MacUniqueness));
        Assert.False(plan.IsCurrent(Target, Document(), new(17, 5, 2), Off, IdentityExperimentKind.MacUniqueness));
        Assert.False(plan.IsCurrent(Target, Document(), Version, Off with { State = VmPowerState.Suspended }, IdentityExperimentKind.MacUniqueness));
        Assert.False(plan.IsCurrent(Target, Document(), Version, Off with { ObservedAt = Off.ObservedAt.AddSeconds(1) }, IdentityExperimentKind.MacUniqueness));
        Assert.False(plan.IsCurrent(Target, Document(), Version, Off with { IsReliable = false }, IdentityExperimentKind.MacUniqueness));
        Assert.False(plan.IsCurrent(Target, Document(), Version, Off, IdentityExperimentKind.UuidOnCopy));
        Assert.NotEqual(plan.Id, Plan().Id);
    }

    [Theory]
    [InlineData(IdentityExperimentKind.MacUniqueness)]
    [InlineData(IdentityExperimentKind.UuidOnCopy)]
    public void ExportIsANonExecutableRedactedReportNotAnImportableConfiguration(IdentityExperimentKind kind)
    {
        var document = Document();
        var plan = IdentityExperimentPlanner.Create(Target, document, Version, Off, kind);
        var bytes = plan.SerializeReport();
        var json = Encoding.UTF8.GetString(bytes);
        using var parsed = JsonDocument.Parse(bytes);
        Assert.Equal("vmil.identity-plan/1", parsed.RootElement.GetProperty("schemaVersion").GetString());
        Assert.Equal("notExecuted", parsed.RootElement.GetProperty("status").GetString());
        Assert.False(parsed.RootElement.GetProperty("canExecute").GetBoolean());
        Assert.Equal("redacted", parsed.RootElement.GetProperty("privacyMode").GetString());
        Assert.DoesNotContain(Mac, json, StringComparison.Ordinal);
        Assert.DoesNotContain(Uuid, json, StringComparison.Ordinal);
        Assert.DoesNotContain("VMIL-private-synthetic-target", json, StringComparison.Ordinal);
        Assert.DoesNotContain(document.ContentHash, json, StringComparison.Ordinal);
        Assert.DoesNotContain("targetPath", json, StringComparison.Ordinal);
        Assert.DoesNotContain("proposedBytes", json, StringComparison.Ordinal);
        Assert.Throws<ReportFormatException>(() => ReportJson.Deserialize(bytes));
        Assert.Equal(Encoding.UTF8.GetBytes(Text), document.GetOriginalBytes());
    }
}
