using System.Text;
using System.Text.Json;
using VMIdentityLab.Core.Experiments;
using VMIdentityLab.VMware;
using VMIdentityLab.VMware.Configuration;

namespace VMIdentityLab.IntegrationTests;

// These are synthetic memory/CPU transactions around read-only identity declarations.
// They do not emulate or approve a MAC/UUID write channel.
public sealed partial class ApiConfigurationTests
{
    private const string IdentityDeclarations = "ethernet0.address = \"00:50:56:00:00:01\"\n"
        + "uuid.bios = \"00 11 22 33 44 55 66 77-88 99 aa bb cc dd ee ff\"\n";

    [Theory]
    [InlineData("ethernet0.address")]
    [InlineData("ethernet0.generatedAddress")]
    [InlineData("uuid.bios")]
    [InlineData("uuid.location")]
    public async Task IdentityKeysAloneOrMixedWithApprovedMemoryCannotCreateExecutablePreviews(string key)
    {
        using var f = new Fixture();
        await File.AppendAllTextAsync(f.Path, IdentityDeclarations, Token);
        var original = await File.ReadAllBytesAsync(f.Path, Token);
        await Assert.ThrowsAsync<ArgumentException>(() => f.Preview(new() { [key] = "2" }));
        await Assert.ThrowsAsync<ArgumentException>(() => f.Preview(new() { ["memsize"] = "4096", [key] = "2" }));
        Assert.Equal(original, await File.ReadAllBytesAsync(f.Path, Token));
        Assert.Empty(f.Server.Requests);
        Assert.Single(Directory.GetFiles(f.Folder));
    }

    [Theory]
    [InlineData(IdentityExperimentKind.MacUniqueness)]
    [InlineData(IdentityExperimentKind.UuidOnCopy)]
    public async Task ReviewingAnIdentityPlanDoesNotConfirmAnExecutableNumericPreview(IdentityExperimentKind kind)
    {
        using var f = new Fixture();
        await File.AppendAllTextAsync(f.Path, IdentityDeclarations, Token);
        var plan = await CreateIdentityPlanAsync(f, kind);
        var preview = await f.Preview();
        var history = new LocalVmxBackupHistory(Path.Combine(f.Folder, "history"));
        var manager = new VmxApiConfigManager(f.Api, () => Version, files: f.Files);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new VmxConfigWorkflow(manager, history)
            .ApplyAsync(preview, plan.Id, false, Token));
        Assert.False(plan.CanExecute);
        Assert.Empty(f.Server.Requests);
        Assert.Empty((await history.ReadAsync(Token)).Entries);
        Assert.Single(Directory.GetFiles(f.Folder));
    }

    [Fact]
    public async Task NumericApiApplyAndReopenedHistoryRestorePreserveLaterIdentityDeclarations()
    {
        using var f = new Fixture();
        await File.AppendAllTextAsync(f.Path, IdentityDeclarations, Token);
        var original = await File.ReadAllBytesAsync(f.Path, Token);
        var manager = new VmxApiConfigManager(f.Api, () => Version, files: f.Files);
        var preview = await f.Preview();
        var directory = Path.Combine(f.Folder, "history");
        var applied = await new VmxConfigWorkflow(manager, new LocalVmxBackupHistory(directory))
            .ApplyAsync(preview, preview.Id, false, Token);
        Assert.Equal(original, await File.ReadAllBytesAsync(applied.Receipt.BackupPath, Token));
        Assert.Equal(preview.Plan.GetProposedBytes(), await File.ReadAllBytesAsync(f.Path, Token));
        Assert.EndsWith(IdentityDeclarations, await File.ReadAllTextAsync(f.Path, Token));

        // An unrelated identity change after a boot must survive numeric-only recovery.
        ChangeSyntheticIdentity(f, "mac");
        ChangeSyntheticIdentity(f, "uuid");
        var laterIdentities = ChangedIdentityText(ChangedIdentityText(IdentityDeclarations, "mac"), "uuid");
        var loaded = Assert.Single((await new LocalVmxBackupHistory(directory).ReadAsync(Token)).Entries);
        var restore = await VmxApiConfigManager.PrepareRestoreAsync(f.Path, loaded.Receipt, Token);
        Assert.All(restore.Plan.Changes, change => Assert.True(change.Key is "memsize" or "numvcpus"));
        var restored = await new VmxConfigWorkflow(manager, new LocalVmxBackupHistory(directory))
            .ApplyAsync(restore, restore.Id, true, Token);
        Assert.Equal("restore", restored.Operation);
        Assert.Equal(Encoding.UTF8.GetString(f.Original) + laterIdentities, await File.ReadAllTextAsync(f.Path, Token));
        Assert.Equal(2, (await new LocalVmxBackupHistory(directory).ReadAsync(Token)).Entries.Count);
        Assert.Equal(2, f.Server.PutCount);
        Assert.Equal(0, f.Files.ReplaceCount);
        Assert.False(VmxApiConfigManager.HasPendingOperation(f.Path));
    }

    [Theory]
    [InlineData("mac")]
    [InlineData("uuid")]
    public async Task IdentityEditsAfterPreviewPreventNumericWriteBeforeBackup(string field)
    {
        using var f = new Fixture();
        await File.AppendAllTextAsync(f.Path, IdentityDeclarations, Token);
        var preview = await f.Preview();
        ChangeSyntheticIdentity(f, field);
        var external = await File.ReadAllBytesAsync(f.Path, Token);
        var error = await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
        Assert.False(error.OutcomeUnknown);
        Assert.False(File.Exists(error.BackupPath));
        Assert.Equal(0, f.Server.PutCount);
        Assert.Equal(external, await File.ReadAllBytesAsync(f.Path, Token));
    }

    [Theory]
    [InlineData("mac")]
    [InlineData("uuid")]
    public async Task IdentityEditsDuringIntentPersistencePreventDispatchAndRetainNotSentEvidence(string field)
    {
        using var f = new Fixture();
        await File.AppendAllTextAsync(f.Path, IdentityDeclarations, Token);
        var original = await File.ReadAllBytesAsync(f.Path, Token);
        var preview = await f.Preview();
        f.Files.AfterIntent = () => ChangeSyntheticIdentity(f, field);
        var error = await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
        Assert.Equal(0, f.Server.PutCount);
        Assert.False(error.OutcomeUnknown);
        Assert.False(VmxApiConfigManager.HasPendingOperation(f.Path));
        Assert.EndsWith(".not-sent", error.JournalPath);
        Assert.True(File.Exists(error.JournalPath));
        Assert.Equal(original, await File.ReadAllBytesAsync(error.BackupPath, Token));
        Assert.Equal(preview.Plan.GetProposedBytes(), await File.ReadAllBytesAsync(error.ProposedPath, Token));
        Assert.Equal(ChangedIdentityText(Encoding.UTF8.GetString(original), field), await File.ReadAllTextAsync(f.Path, Token));
        Assert.Equal(0, f.Files.ReplaceCount);
    }

    [Theory]
    [InlineData("poweredOn")]
    [InlineData("suspended")]
    [InlineData("unrecognized")]
    [InlineData("registration")]
    [InlineData("settings")]
    [InlineData("version")]
    [InlineData("occupied")]
    public async Task ProviderOrFileChangesWhileIntentIsWrittenAreRecheckedBeforePut(string phase)
    {
        using var f = new Fixture();
        var preview = await f.Preview();
        var installed = Version;
        FileStream? occupied = null;
        f.Files.AfterIntent = () =>
        {
            switch (phase)
            {
                case "registration": f.Server.Id = "different-vm"; break;
                case "settings": f.Server.SettingsMismatch = true; break;
                case "version": installed = new Version(17, 5, 2, 23775572); break;
                case "occupied": occupied = new(f.Path, FileMode.Open, FileAccess.Read, FileShare.None); break;
                default: f.Server.Power = phase; break;
            }
        };
        try
        {
            var manager = new VmxApiConfigManager(f.Api, () => installed, files: f.Files);
            var error = await Assert.ThrowsAsync<VmxApiTransactionException>(() => manager.ApplyAsync(preview, preview.Id, Token));
            Assert.False(error.OutcomeUnknown);
            Assert.EndsWith(".not-sent", error.JournalPath);
            Assert.True(File.Exists(error.JournalPath));
            Assert.Equal(f.Original, await File.ReadAllBytesAsync(error.BackupPath, Token));
            Assert.False(VmxApiConfigManager.HasPendingOperation(f.Path));
            Assert.Equal(0, f.Server.PutCount);
        }
        finally { occupied?.Dispose(); }
        Assert.Equal(f.Original, await File.ReadAllBytesAsync(f.Path, Token));
    }

    [Fact]
    public async Task FailureToArchiveAnUnsentIntentRetainsItAndBlocksRestartWithoutPut()
    {
        using var f = new Fixture();
        await File.AppendAllTextAsync(f.Path, IdentityDeclarations, Token);
        var preview = await f.Preview();
        f.Files.AfterIntent = () =>
        {
            var pending = VmxApiConfigManager.PendingOperationPath(f.Path);
            using var intent = JsonDocument.Parse(File.ReadAllBytes(pending));
            var id = intent.RootElement.GetProperty("operationId").GetGuid();
            Directory.CreateDirectory(pending + "." + id.ToString("N") + ".not-sent");
            ChangeSyntheticIdentity(f, "uuid");
        };
        var stopped = await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
        Assert.False(stopped.OutcomeUnknown); // This attempt sent no PUT, but its intent could not be archived.
        Assert.True(VmxApiConfigManager.HasPendingOperation(f.Path));
        var pendingBytes = await File.ReadAllBytesAsync(stopped.JournalPath, Token);
        var external = await File.ReadAllBytesAsync(f.Path, Token);
        var next = await f.Preview();
        var requests = f.Server.Requests.Count;
        var blocked = await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(next, next.Id, Token));
        Assert.True(blocked.OutcomeUnknown);
        Assert.Equal(requests, f.Server.Requests.Count);
        Assert.Equal(0, f.Server.PutCount);
        Assert.Equal(pendingBytes, await File.ReadAllBytesAsync(stopped.JournalPath, Token));
        Assert.Equal(external, await File.ReadAllBytesAsync(f.Path, Token));
    }

    [Theory]
    [InlineData("mac")]
    [InlineData("uuid")]
    public async Task IdentityEditsAfterPutKeepPendingAndPreventReplayAfterManagerRestart(string field)
    {
        using var f = new Fixture();
        await File.AppendAllTextAsync(f.Path, IdentityDeclarations, Token);
        var original = await File.ReadAllBytesAsync(f.Path, Token);
        var preview = await f.Preview();
        f.Server.AfterPut = () => ChangeSyntheticIdentity(f, field);
        var error = await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
        Assert.True(error.OutcomeUnknown);
        var pending = await File.ReadAllBytesAsync(error.JournalPath, Token);
        var external = await File.ReadAllBytesAsync(f.Path, Token);
        Assert.Equal(original, await File.ReadAllBytesAsync(error.BackupPath, Token));
        var requests = f.Server.Requests.Count;
        var reverse = await f.Preview(new() { ["memsize"] = "6144", ["numvcpus"] = "4" });
        await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(reverse, reverse.Id, Token));
        Assert.Equal(requests, f.Server.Requests.Count);
        Assert.Equal(1, f.Server.PutCount);
        Assert.Equal(pending, await File.ReadAllBytesAsync(error.JournalPath, Token));
        Assert.Equal(external, await File.ReadAllBytesAsync(f.Path, Token));
        Assert.Equal(0, f.Files.ReplaceCount);
    }

    [Fact]
    public async Task NumericRestoreNeedsANewConfirmationAndRejectsIdentityEditsAfterItsPreview()
    {
        using var f = new Fixture();
        await File.AppendAllTextAsync(f.Path, IdentityDeclarations, Token);
        var manager = new VmxApiConfigManager(f.Api, () => Version, files: f.Files);
        var apply = await f.Preview();
        var receipt = await manager.ApplyAsync(apply, apply.Id, Token);
        var restore = await VmxApiConfigManager.PrepareRestoreAsync(f.Path, receipt, Token);
        var requests = f.Server.Requests.Count;
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.ApplyAsync(restore, apply.Id, Token));
        Assert.Equal(requests, f.Server.Requests.Count);
        ChangeSyntheticIdentity(f, "uuid");
        var external = await File.ReadAllBytesAsync(f.Path, Token);
        var error = await Assert.ThrowsAsync<VmxApiTransactionException>(() => manager.ApplyAsync(restore, restore.Id, Token));
        Assert.False(error.OutcomeUnknown);
        Assert.False(File.Exists(error.BackupPath));
        Assert.Equal(1, f.Server.PutCount);
        Assert.Equal(external, await File.ReadAllBytesAsync(f.Path, Token));
    }

    [Theory]
    [InlineData(IdentityExperimentKind.MacUniqueness)]
    [InlineData(IdentityExperimentKind.UuidOnCopy)]
    public async Task OfflineIdentityPlanningAfterResponseLossCannotClearPendingOrAuthorizeReplay(IdentityExperimentKind kind)
    {
        using var f = new Fixture();
        await File.AppendAllTextAsync(f.Path, IdentityDeclarations, Token);
        f.Server.Failure = "timeout";
        var preview = await f.Preview();
        var error = await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
        var pending = await File.ReadAllBytesAsync(error.JournalPath, Token);
        var plan = await CreateIdentityPlanAsync(f, kind);
        await plan.SaveReportAsync(Path.Combine(f.Folder, "plan.json"), Token);
        using var report = JsonDocument.Parse(plan.SerializeReport());
        Assert.False(report.RootElement.GetProperty("canExecute").GetBoolean());
        var reverse = await f.Preview(new() { ["memsize"] = "6144", ["numvcpus"] = "4" });
        var requests = f.Server.Requests.Count;
        await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(reverse, reverse.Id, Token));
        Assert.Equal(requests, f.Server.Requests.Count);
        Assert.Equal(1, f.Server.PutCount);
        Assert.True(VmxApiConfigManager.HasPendingOperation(f.Path));
        Assert.Equal(pending, await File.ReadAllBytesAsync(error.JournalPath, Token));
    }

    private static async Task<IdentityExperimentPlan> CreateIdentityPlanAsync(Fixture fixture, IdentityExperimentKind kind)
    {
        var read = await new VmxReader().ReadAsync(fixture.Path, Token);
        return IdentityExperimentPlanner.Create(fixture.Path, read.Document, Version,
            new(VmPowerState.PoweredOff, "synthetic", DateTimeOffset.UtcNow, true, "Synthetic only"), kind);
    }

    private static void ChangeSyntheticIdentity(Fixture fixture, string field)
        => File.WriteAllText(fixture.Path, ChangedIdentityText(File.ReadAllText(fixture.Path), field));

    private static string ChangedIdentityText(string text, string field) => field switch
    {
        "mac" => text.Replace("00:50:56:00:00:01", "00:50:56:00:00:02", StringComparison.Ordinal),
        "uuid" => text.Replace("88 99 aa bb cc dd ee ff", "88 99 aa bb cc dd ee fe", StringComparison.Ordinal),
        _ => throw new ArgumentException("Unknown synthetic identity field.", nameof(field))
    };
}
