using System.Text;
using VMIdentityLab.Core.Comparison;
using VMIdentityLab.Core.Reports;

namespace VMIdentityLab.UnitTests;

public sealed class IdentityValidationTests
{
    private static readonly Guid Session = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string Mac = "00:50:56:00:00:01", NewMac = "00:50:56:00:00:02";
    private const string Uuid = "22114d56-4433-6655-7788-99aabbccddee", NewUuid = "77884d56-5566-3344-2211-aabbccddeeff";
    private static readonly DateTimeOffset Time = DateTimeOffset.Parse("2026-10-21T00:00:00Z");

    private static PrivateValidationSnapshot Snapshot(ValidationPhase phase, ValidationField field = ValidationField.StaticMac)
    {
        var time = Time.AddMinutes((int)phase * 2);
        Finding F(string id, FindingValue value) => new() { Id = id, Category = FindingCatalog.ById[id].Category,
            Source = FindingCatalog.ById[id].Source, ObservedAt = time, Status = CollectionStatus.Success, Value = value };
        return new() { Phase = phase, SessionId = Session, DataKind = ValidationDataKind.Synthetic, Elevated = false,
            Report = new() { CollectedAt = time, Findings = FindingCatalog.ById.Values.Where(item => item.Sensitivity == DataSensitivity.Public)
                .Select(item => F(item.Id, item.Id switch { "cpu.logicalProcessors" => FindingValue.FromNumber(4, ValueUnit.Count),
                    "system.memoryBytes" => FindingValue.FromNumber(6442450944m, ValueUnit.Bytes),
                    "smbios.manufacturer" or "cpu.hypervisorVendor" => FindingValue.FromText("VMware"),
                    "cpu.hypervisorPresent" or "tools.installed" => FindingValue.FromBoolean(true),
                    "pnp.vmwareDeviceCount" => FindingValue.FromNumber(1, ValueUnit.Count), _ => FindingValue.FromText("synthetic") })).ToArray() },
            Identities = [F("network.macAddress", FindingValue.FromText(phase == ValidationPhase.After && field == ValidationField.StaticMac ? NewMac : Mac)),
                F("smbios.uuid", FindingValue.FromText(phase == ValidationPhase.After && field == ValidationField.BiosUuid ? NewUuid : Uuid))] };
    }

    [Theory]
    [InlineData(ValidationField.StaticMac)]
    [InlineData(ValidationField.BiosUuid)]
    public void ThreeStagesSeparateConfigGuestAndUnchangedProgramClassification(ValidationField field)
    {
        var config = new ValidationConfiguration(field, new(17, 5, 2, 23775571), Time.AddMinutes(1), Time.AddMinutes(3), Mac,
            field == ValidationField.StaticMac ? NewMac : Mac, Uuid, field == ValidationField.BiosUuid ? NewUuid : Uuid);
        var result = IdentityValidationComparer.Compare(field, Snapshot(0, field), Snapshot(ValidationPhase.After, field), Snapshot(ValidationPhase.Restored, field), config);
        Assert.All(result.Configuration, item => Assert.Equal(ValidationState.Passed, item.State));
        Assert.All(result.Guest.Where(item => item.Check != "networkAvailability"), item => Assert.Equal(ValidationState.Passed, item.State));
        Assert.Equal(ValidationState.NotProvided, result.Guest.Single(item => item.Check == "networkAvailability").State);
        Assert.All(result.Program, item => Assert.Equal("consistent", item.VmwareGuestEvidence));
        Assert.Equal(ValidationDataKind.Synthetic, result.DataKind);
    }

    [Theory]
    [InlineData("unchanged", "changed", ValidationState.Failed, ValidationReason.ValueUnchanged)]
    [InlineData("notRestored", "restored", ValidationState.Failed, ValidationReason.ValueDiffers)]
    [InlineData("other", "otherIdentity", ValidationState.Failed, ValidationReason.OtherIdentityChanged)]
    [InlineData("source", "changed", ValidationState.NotComparable, ValidationReason.SourceMismatch)]
    [InlineData("redacted", "changed", ValidationState.NotComparable, ValidationReason.Redacted)]
    [InlineData("unknown", "changed", ValidationState.NotComparable, ValidationReason.Unavailable)]
    [InlineData("number", "changed", ValidationState.NotComparable, ValidationReason.InvalidIdentity)]
    [InlineData("boolean", "changed", ValidationState.NotComparable, ValidationReason.InvalidIdentity)]
    [InlineData("invalid", "changed", ValidationState.NotComparable, ValidationReason.InvalidIdentity)]
    [InlineData("multiple", "changed", ValidationState.NotComparable, ValidationReason.MultipleAdapters)]
    [InlineData("elevated", "ordinaryUser", ValidationState.Failed, ValidationReason.Elevated)]
    public void NegativeEvidenceCannotBecomeSuccessfulIdentityValidation(string change, string check, ValidationState state, ValidationReason reason)
    {
        var before = Snapshot(0); var after = Snapshot(ValidationPhase.After); var restored = Snapshot(ValidationPhase.Restored);
        Finding Edit(Finding item) => change switch
        {
            "unchanged" when item.Id == "network.macAddress" => item with { Value = FindingValue.FromText(Mac) },
            "other" when item.Id == "smbios.uuid" => item with { Value = FindingValue.FromText(NewUuid) },
            "source" when item.Id == "network.macAddress" => item with { Source = "unverified-source" },
            "redacted" when item.Id == "network.macAddress" => item with { Value = FindingValue.Redacted() },
            "unknown" when item.Id == "network.macAddress" => item with { Value = new() },
            "number" when item.Id == "network.macAddress" => item with { Value = FindingValue.FromNumber(1) },
            "boolean" when item.Id == "network.macAddress" => item with { Value = FindingValue.FromBoolean(true) },
            "invalid" when item.Id == "network.macAddress" => item with { Value = FindingValue.FromText("invalid") },
            "multiple" when item.Id == "network.macAddress" => item with { Value = FindingValue.FromText(NewMac + ";" + Mac) }, _ => item
        };
        after = after with { Elevated = change == "elevated", Identities = after.Identities.Select(Edit).ToArray() };
        if (change == "notRestored") restored = restored with { Identities = restored.Identities.Select(item => item.Id == "network.macAddress"
            ? item with { Value = FindingValue.FromText(NewMac) } : item).ToArray() };
        var row = IdentityValidationComparer.Compare(0, before, after, restored).Guest.Single(item => item.Check == check);
        Assert.Equal(state, row.State); Assert.Equal(reason, row.Reason);
    }

    [Fact]
    public void ExplicitInterfaceScopeRoundTripsWithoutAppearingInPublicOutput()
    {
        var adapter = Guid.Parse("22222222-2222-2222-2222-222222222222");
        PrivateValidationSnapshot Stage(ValidationPhase phase) => ValidationSnapshotJson.Deserialize(
            ValidationSnapshotJson.SerializePrivate(Snapshot(phase) with { NetworkAdapterId = adapter }), phase);
        Assert.Equal(adapter, Stage(0).NetworkAdapterId);
        var result = IdentityValidationComparer.Compare(0, Stage(0), Stage(ValidationPhase.After), Stage(ValidationPhase.Restored));
        Assert.Equal(ValidationState.Passed, result.Guest.Single(row => row.Check == "changed").State);
        Assert.DoesNotContain(adapter.ToString(), IdentityValidationComparer.RenderHtml(result), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DifferentOrMissingInterfaceScopeCannotBeCompared(bool missing)
    {
        var adapter = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var before = Snapshot(0) with { NetworkAdapterId = adapter };
        var after = Snapshot(ValidationPhase.After) with { NetworkAdapterId = missing ? null : Guid.Parse("33333333-3333-3333-3333-333333333333") };
        var restored = Snapshot(ValidationPhase.Restored) with { NetworkAdapterId = adapter };
        var result = IdentityValidationComparer.Compare(0, before, after, restored);
        Assert.Equal(ValidationReason.SourceMismatch, result.Guest.Single(row => row.Check == "context").Reason);
        Assert.Equal(ValidationState.NotComparable, result.Guest.Single(row => row.Check == "changed").State);
    }

    [Fact]
    public void ExplicitScopeCannotConcealMultipleAddressesOrUseEmptyInterfaceId()
    {
        var adapter = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var before = Snapshot(0) with { NetworkAdapterId = adapter };
        var after = Snapshot(ValidationPhase.After) with { NetworkAdapterId = adapter };
        after = after with { Identities = after.Identities.Select(row => row.Id == "network.macAddress"
            ? row with { Value = FindingValue.FromText(Mac + ";" + NewMac) } : row).ToArray() };
        var restored = Snapshot(ValidationPhase.Restored) with { NetworkAdapterId = adapter };
        Assert.Equal(ValidationReason.MultipleAdapters, IdentityValidationComparer.Compare(0, before, after, restored).Guest.Single(row => row.Check == "changed").Reason);
        Assert.Throws<ReportFormatException>(() => ValidationSnapshotJson.SerializePrivate(before with { NetworkAdapterId = Guid.Empty }));
    }

    [Theory]
    [InlineData(CollectionStatus.AccessDenied)]
    [InlineData(CollectionStatus.NotSupported)]
    [InlineData(CollectionStatus.Timeout)]
    [InlineData(CollectionStatus.Failed)]
    public void FailedObservationStatusSurvivesComparisonAndPrivateRoundTrip(CollectionStatus status)
    {
        var after = Snapshot(ValidationPhase.After);
        after = after with { Identities = after.Identities.Select(item => item.Id == "network.macAddress" ? item with { Status = status, Value = new() } : item).ToArray() };
        after = ValidationSnapshotJson.Deserialize(ValidationSnapshotJson.SerializePrivate(after), ValidationPhase.After);
        var row = IdentityValidationComparer.Compare(0, Snapshot(0), after, Snapshot(ValidationPhase.Restored)).Guest.Single(item => item.Check == "changed");
        Assert.Equal(ValidationState.NotComparable, row.State); Assert.Equal(ValidationReason.Unavailable, row.Reason);
        Assert.Equal(status, row.AfterStatus); Assert.Equal(CollectionStatus.Success, row.BeforeStatus);
    }

    [Theory]
    [InlineData("version", ValidationReason.VersionMismatch)]
    [InlineData("session", ValidationReason.SessionMismatch)]
    [InlineData("phase", ValidationReason.PhaseMismatch)]
    [InlineData("time", ValidationReason.TimeOrder)]
    [InlineData("staleObservation", ValidationReason.TimeOrder)]
    public void InconsistentContextIsNotComparable(string kind, ValidationReason reason)
    {
        var after = Snapshot(ValidationPhase.After);
        after = kind switch
        {
            "version" => after with { Report = after.Report with { AppVersion = "different" } },
            "session" => after with { SessionId = Guid.NewGuid() },
            "phase" => after with { Phase = ValidationPhase.Restored },
            "time" => after with { Report = after.Report with { CollectedAt = Time.AddDays(-1) } },
            _ => after with { Identities = after.Identities.Select(item => item with { ObservedAt = Time }).ToArray() }
        };
        var row = IdentityValidationComparer.Compare(0, Snapshot(0), after, Snapshot(ValidationPhase.Restored)).Guest.Single(item => item.Check == "changed");
        Assert.Equal(ValidationState.NotComparable, row.State); Assert.Equal(reason, row.Reason);
    }

    [Fact]
    public void LegacyReportsRemainReadableButEqualMasksNeverProveIdentityEquality()
    {
        PrivateValidationSnapshot Legacy(ValidationPhase phase) => ValidationSnapshotJson.Deserialize(ReportJson.Serialize(Snapshot(phase).Report), phase);
        var result = IdentityValidationComparer.Compare(0, Legacy(0), Legacy(ValidationPhase.After), Legacy(ValidationPhase.Restored));
        Assert.Equal(ValidationDataKind.LegacyReport, result.DataKind);
        Assert.Equal(ValidationState.NotProvided, Assert.Single(result.Configuration).State);
        Assert.Equal(ValidationReason.Redacted, result.Guest.Single(item => item.Check == "restored").Reason);
        Assert.Equal(ValidationState.Passed, result.Guest.Single(item => item.Check == "cpu.logicalProcessors").State);
    }

    [Fact]
    public void ChangedPublicCollectionStatusIsVisibleEvenWhenIdentitiesRestore()
    {
        var after = Snapshot(ValidationPhase.After);
        after = after with { Report = after.Report with { Findings = after.Report.Findings.Select(item => item.Id == "system.memoryBytes"
            ? item with { Status = CollectionStatus.Timeout, Value = new() } : item).ToArray() } };
        var row = IdentityValidationComparer.Compare(0, Snapshot(0), after, Snapshot(ValidationPhase.Restored)).Guest.Single(item => item.Check == "system.memoryBytes");
        Assert.Equal(ValidationState.Failed, row.State); Assert.Equal(CollectionStatus.Timeout, row.AfterStatus);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("unknown")]
    [InlineData("missing")]
    [InlineData("wrongPhase")]
    public void AmbiguousOrUnsupportedSnapshotInputsAreRejectedWithoutEchoingRawValues(string kind)
    {
        var json = Encoding.UTF8.GetString(ValidationSnapshotJson.SerializePrivate(Snapshot(0)));
        json = kind switch
        {
            "duplicate" => json.Replace("\"phase\": \"before\"", "\"phase\": \"before\", \"phase\": \"after\"", StringComparison.Ordinal),
            "unknown" => json.Insert(1, "\"unsafe\":\"raw-secret\","),
            "missing" => json.Replace("\"elevated\": false,", "", StringComparison.Ordinal), _ => json
        };
        var error = Assert.Throws<ReportFormatException>(() => ValidationSnapshotJson.Deserialize(Encoding.UTF8.GetBytes(json), kind == "wrongPhase" ? ValidationPhase.After : ValidationPhase.Before));
        Assert.DoesNotContain(Mac, error.Message, StringComparison.Ordinal); Assert.DoesNotContain("raw-secret", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("system.os")]
    [InlineData("pnp.vmwareDeviceCount")]
    [InlineData("tools.installed")]
    public void OtherPublicObservationsAreComparedRatherThanSilentlyOmitted(string id)
    {
        var after = Snapshot(ValidationPhase.After);
        after = after with { Report = after.Report with { Findings = after.Report.Findings.Select(item => item.Id == id
            ? item with { Value = item.Value.Kind == ValueKind.Number ? FindingValue.FromNumber(7, item.Value.Unit)
                : item.Value.Kind == ValueKind.Boolean ? FindingValue.FromBoolean(false) : FindingValue.FromText("changed") } : item).ToArray() } };
        var row = IdentityValidationComparer.Compare(0, Snapshot(0), after, Snapshot(ValidationPhase.Restored)).Guest.Single(item => item.Check == id);
        Assert.Equal(ValidationState.Failed, row.State); Assert.Equal(ValidationReason.PublicFieldChanged, row.Reason);
    }

    [Fact]
    public void MissingIdentityAndLegacyPublicSourceDifferencesRemainDistinctReasons()
    {
        var after = Snapshot(ValidationPhase.After) with { Identities = [] };
        after = after with { Report = after.Report with { Findings = after.Report.Findings.Select(item => item.Id == "cpu.model"
            ? item with { Source = "Win32_Processor.Name" } : item).ToArray() } };
        var result = IdentityValidationComparer.Compare(0, Snapshot(0), after, Snapshot(ValidationPhase.Restored));
        Assert.Equal(ValidationReason.Missing, result.Guest.Single(item => item.Check == "changed").Reason);
        Assert.Equal(ValidationReason.SourceMismatch, result.Guest.Single(item => item.Check == "cpu.model").Reason);
        Assert.Equal(ValidationState.NotComparable, result.Guest.Single(item => item.Check == "cpu.model").State);
    }

    [Fact]
    public void AGuestChangeCannotProveThatTheImportedConfigurationValuesMatch()
    {
        var config = new ValidationConfiguration(0, new(17, 5, 2, 23775571), Time.AddMinutes(1), Time.AddMinutes(3), Mac, "00:50:56:00:00:03", Uuid, Uuid);
        var result = IdentityValidationComparer.Compare(0, Snapshot(0), Snapshot(ValidationPhase.After), Snapshot(ValidationPhase.Restored), config);
        Assert.Equal(ValidationState.Passed, result.Guest.Single(item => item.Check == "changed").State);
        Assert.Equal(ValidationState.Failed, result.Configuration.Single(item => item.Check == "guestMatchesReceipts").State);
    }

    [Fact]
    public void ThreeMissingCollectorVersionsDoNotCountAsMatchingKnownVersions()
    {
        PrivateValidationSnapshot Missing(ValidationPhase phase)
        {
            var snapshot = Snapshot(phase);
            return snapshot with { Report = snapshot.Report with { AppVersion = "" } };
        }
        var result = IdentityValidationComparer.Compare(0, Missing(0), Missing(ValidationPhase.After), Missing(ValidationPhase.Restored));
        Assert.Equal(ValidationReason.VersionMismatch, result.Guest.Single(item => item.Check == "context").Reason);
        Assert.Equal(ValidationState.NotComparable, result.Guest.Single(item => item.Check == "changed").State);
    }
}
