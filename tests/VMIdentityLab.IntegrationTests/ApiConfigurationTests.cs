using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using VMIdentityLab.VMware;
using VMIdentityLab.VMware.Configuration;

namespace VMIdentityLab.IntegrationTests;

public sealed class ApiConfigurationTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static readonly Version Version = new(17, 5, 2, 23775571);
    private static readonly Dictionary<string, string> Changes = new() { ["memsize"] = "4096", ["numvcpus"] = "2" };

    [Fact]
    public async Task ApiApplyAndPostBootRestoreUseTheSameHistoryWorkflowWithoutFileReplacement()
    {
        using var f = new Fixture();
        var manager = new VmxApiConfigManager(f.Api, () => Version);
        var preview = await f.Preview();
        var history = new LocalVmxBackupHistory(Path.Combine(f.Folder, "history"));
        var applied = await new VmxConfigWorkflow(manager, history).ApplyAsync(preview, preview.Id, false, Token);
        Assert.Equal(VmxApiConfigManager.BackendName, applied.Receipt.Backend);
        Assert.Equal(f.Original, await File.ReadAllBytesAsync(applied.Receipt.BackupPath, Token));
        Assert.False(VmxApiConfigManager.HasPendingOperation(f.Path));
        var loaded = Assert.Single((await new LocalVmxBackupHistory(Path.Combine(f.Folder, "history")).ReadAsync(Token)).Entries);
        Assert.Equal(applied, loaded);
        await File.AppendAllTextAsync(f.Path, "# later VMware boot metadata\n", Token);
        var restore = await VmxApiConfigManager.PrepareRestoreAsync(f.Path, loaded.Receipt, Token);
        await new VmxConfigWorkflow(manager, history).ApplyAsync(restore, restore.Id, true, Token);
        Assert.Equal(2, f.Server.PutCount);
        Assert.Equal(2, (await history.ReadAsync(Token)).Entries.Count);
        Assert.Equal(Encoding.UTF8.GetString(f.Original) + "# later VMware boot metadata\n", await File.ReadAllTextAsync(f.Path, Token));
        Assert.All(f.Server.Requests, request => Assert.True(request.StartsWith("GET ", StringComparison.Ordinal)
            || request == "PUT /api/vms/test-vm"));
        Assert.Equal(0, f.Files.ReplaceCount);
    }

    [Fact]
    public async Task ApprovedFinitePreviewStillRequiresItsCurrentConfirmation()
    {
        using var f = new Fixture();
        var preview = await f.Preview();
        Assert.True(ReleasedVmxApiWritePolicy.IsTestedCandidate(preview));
        Assert.True(VmxApiConfigManager.IsProductionWriteApproved(preview));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new VmxApiConfigManager(f.Api, () => Version).ApplyAsync(preview, Guid.Empty, Token));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Manager().ApplyAsync(preview, Guid.Empty, Token));
        Assert.Empty(f.Server.Requests);
        Assert.Single(Directory.GetFiles(f.Folder));
    }

    [Theory]
    [InlineData("17.5.1", "19", "2", "6144", "4")]
    [InlineData("17.5.3", "19", "2", "6144", "4")]
    [InlineData("17.6.0", "19", "2", "6144", "4")]
    [InlineData("17.5.2", "19", "2", "6144", "4")]
    [InlineData("17.5.2.0", "19", "2", "6144", "4")]
    [InlineData("17.5.2.23775572", "19", "2", "6144", "4")]
    [InlineData("17.5.2.23775571", "20", "2", "6144", "4")]
    [InlineData("17.5.2.23775571", "19", "1", "6144", "4")]
    [InlineData("17.5.2.23775571", "19", "2", "8192", "4")]
    [InlineData("17.5.2.23775571", "19", "2", "6144", "8")]
    public async Task UnverifiedVersionTopologyOrOriginalValuesCannotCreateSidecarsOrReachTheApi(
        string versionText, string hardware, string cores, string memory, string processors)
    {
        using var f = new Fixture();
        var version = System.Version.Parse(versionText);
        await File.WriteAllTextAsync(f.Path, $"virtualHW.version = \"{hardware}\"\nmemsize = \"{memory}\"\nnumvcpus = \"{processors}\"\ncpuid.coresPerSocket = \"{cores}\"\n", Token);
        var original = await File.ReadAllBytesAsync(f.Path, Token);
        var preview = await new VmxConfigManager().PrepareAsync(f.Path, version, Changes, Token);
        Assert.False(VmxApiConfigManager.IsProductionWriteApproved(preview));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new VmxApiConfigManager(f.Api, () => version).ApplyAsync(preview, preview.Id, Token));
        Assert.Empty(f.Server.Requests);
        Assert.Single(Directory.GetFiles(f.Folder));
        Assert.Equal(original, await File.ReadAllBytesAsync(f.Path, Token));
    }

    [Theory]
    [InlineData("8192", "4")]
    [InlineData("6144", "8")]
    [InlineData("4096", "6")]
    public async Task UnverifiedProposedValuesCannotReachTheApi(string memory, string processors)
    {
        using var f = new Fixture();
        var preview = await f.Preview(new() { ["memsize"] = memory, ["numvcpus"] = processors });
        Assert.False(VmxApiConfigManager.IsProductionWriteApproved(preview));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new VmxApiConfigManager(f.Api, () => Version).ApplyAsync(preview, preview.Id, Token));
        Assert.Empty(f.Server.Requests);
        Assert.Single(Directory.GetFiles(f.Folder));
    }

    [Fact]
    public async Task StartupMetadataWithStaleOffStateRetainsEvidenceAndPreventsReplay()
    {
        using var f = new Fixture();
        // Synthetic values matching the kind of normal metadata observed during the real start race.
        const string metadata = "numa.autosize.cookie = \"20022\"\nnuma.autosize.vcpu.maxPerVirtualNode = \"2\"\ncleanShutdown = \"FALSE\"\nsoftPowerOff = \"FALSE\"\n";
        f.Server.AfterPut = () => File.AppendAllText(f.Path, metadata);
        var manager = new VmxApiConfigManager(f.Api, () => Version);
        var preview = await f.Preview();
        var error = await Assert.ThrowsAsync<VmxApiTransactionException>(() => manager.ApplyAsync(preview, preview.Id, Token));
        Assert.Equal("poweredOff", f.Server.Power); // A stale off response cannot make content changes acceptable.
        Assert.True(error.OutcomeUnknown);
        Assert.True(VmxApiConfigManager.HasPendingOperation(f.Path));
        Assert.Equal(f.Original, await File.ReadAllBytesAsync(error.BackupPath, Token));
        Assert.Equal(preview.Plan.GetProposedBytes(), await File.ReadAllBytesAsync(error.ProposedPath, Token));
        var retained = await File.ReadAllBytesAsync(f.Path, Token);
        Assert.EndsWith(metadata, Encoding.UTF8.GetString(retained));
        var next = await f.Preview(new() { ["memsize"] = "6144", ["numvcpus"] = "4" });
        await Assert.ThrowsAsync<VmxApiTransactionException>(() => new VmxApiConfigManager(f.Api, () => Version).ApplyAsync(next, next.Id, Token));
        Assert.Equal(1, f.Server.PutCount);
        Assert.Equal(retained, await File.ReadAllBytesAsync(f.Path, Token));
    }

    [Theory]
    [InlineData("poweredOn")]
    [InlineData("suspended")]
    [InlineData("paused")]
    [InlineData("unrecognized")]
    public async Task UnsafePowerStatesNeverReachPut(string state)
    {
        using var f = new Fixture();
        f.Server.Power = state;
        var preview = await f.Preview();
        var error = await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
        Assert.False(error.OutcomeUnknown);
        Assert.Equal(0, f.Server.PutCount);
        Assert.False(File.Exists(error.BackupPath));
        Assert.Equal(f.Original, await File.ReadAllBytesAsync(f.Path, Token));
    }

    [Theory]
    [InlineData("content")]
    [InlineData("power")]
    [InlineData("registration")]
    [InlineData("version")]
    [InlineData("settings")]
    public async Task ChangesDuringPreparationInvalidateTheCommit(string phase)
    {
        using var f = new Fixture();
        var preview = await f.Preview();
        if (phase == "content") f.Files.AfterStage = () => File.AppendAllText(f.Path, "# external edit\n");
        if (phase == "power") f.Server.OnPower = count => { if (count == 2) f.Server.Power = "poweredOn"; };
        if (phase == "registration") f.Server.OnInventory = count => { if (count == 2) f.Server.Id = "other-vm"; };
        if (phase == "settings") f.Server.SettingsMismatch = true;
        var checks = 0;
        var manager = f.Manager(() => phase == "version" && ++checks > 1 ? new Version(17, 6, 0) : Version);
        var error = await Assert.ThrowsAsync<VmxApiTransactionException>(() => manager.ApplyAsync(preview, preview.Id, Token));
        Assert.False(error.OutcomeUnknown);
        Assert.Equal(0, f.Server.PutCount);
        Assert.False(VmxApiConfigManager.HasPendingOperation(f.Path));
    }

    [Fact]
    public async Task StalePreviewAndOccupiedTargetCannotBeWritten()
    {
        using var f = new Fixture();
        var preview = await f.Preview();
        await using (var occupied = new FileStream(f.Path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
        }
        await File.AppendAllTextAsync(f.Path, "# later\n", Token);
        await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
        Assert.Equal(0, f.Server.PutCount);
    }

    [Theory]
    [InlineData("backup")]
    [InlineData("stage")]
    [InlineData("corruptBackup")]
    [InlineData("corruptStage")]
    [InlineData("intent")]
    [InlineData("corruptIntent")]
    public async Task FileFailuresKeepTheTargetAndNeverDispatchPut(string phase)
    {
        using var f = new Fixture();
        f.Files.Failure = phase;
        var preview = await f.Preview();
        var error = await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
        Assert.False(error.OutcomeUnknown);
        Assert.Equal(f.Original, await File.ReadAllBytesAsync(f.Path, Token));
        Assert.Equal(0, f.Server.PutCount);
        Assert.Equal(0, f.Files.ReplaceCount);
    }

    [Fact]
    public async Task SameVmAcrossIndependentManagersCannotSubmitConcurrently()
    {
        using var f = new Fixture();
        f.Server.HoldPut = true;
        var preview = await f.Preview();
        var first = f.Manager().ApplyAsync(preview, preview.Id, Token);
        await f.Server.EnteredPut.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);
        try
        {
            await Assert.ThrowsAsync<VmxApiBusyException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
            Assert.Equal(1, f.Server.PutCount);
        }
        finally { f.Server.ReleasePut.TrySetResult(); }
        await first;
        await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
        Assert.Equal(1, f.Server.PutCount);
    }

    [Theory]
    [InlineData("timeout")]
    [InlineData("disconnect")]
    [InlineData("malformed")]
    [InlineData("wrongReply")]
    [InlineData("external")]
    [InlineData("started")]
    [InlineData("server")]
    [InlineData("partialConflict")]
    public async Task UncertainOutcomesPersistAndBlockReplayAfterManagerRestart(string failure)
    {
        using var f = new Fixture();
        f.Server.Failure = failure;
        var preview = await f.Preview();
        var error = await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
        Assert.True(error.OutcomeUnknown);
        Assert.True(File.Exists(error.JournalPath));
        Assert.True(VmxApiConfigManager.HasPendingOperation(f.Path));
        Assert.Equal(f.Original, await File.ReadAllBytesAsync(error.BackupPath, Token));
        var next = await f.Preview(new() { ["memsize"] = "6144", ["numvcpus"] = "4" });
        await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(next, next.Id, Token));
        Assert.Equal(1, f.Server.PutCount);
        Assert.Equal(0, f.Files.ReplaceCount);
        if (failure == "external") Assert.EndsWith("# external edit\n", await File.ReadAllTextAsync(f.Path, Token));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Conflict)]
    public async Task DefinitiveRejectionRequiresProofOfUnchangedSettingsBeforeReleasingIntent(HttpStatusCode code)
    {
        using var f = new Fixture();
        f.Server.RejectPut = code;
        var preview = await f.Preview();
        var error = await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
        Assert.False(error.OutcomeUnknown);
        Assert.False(VmxApiConfigManager.HasPendingOperation(f.Path));
        Assert.EndsWith(".rejected", error.JournalPath);
        Assert.True(File.Exists(error.JournalPath));
        Assert.Equal(f.Original, await File.ReadAllBytesAsync(f.Path, Token));
        Assert.Equal(1, f.Server.PutCount);
    }

    [Fact]
    public async Task CancellationAfterDispatchStillVerifiesAndPersistsHistory()
    {
        using var f = new Fixture();
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Token);
        f.Server.AfterPut = cancel.Cancel;
        var preview = await f.Preview();
        var history = new LocalVmxBackupHistory(Path.Combine(f.Folder, "history"));
        await new VmxConfigWorkflow(f.Manager(), history).ApplyAsync(preview, preview.Id, false, cancel.Token);
        Assert.True(cancel.IsCancellationRequested);
        Assert.Single((await history.ReadAsync(Token)).Entries);
        Assert.False(VmxApiConfigManager.HasPendingOperation(f.Path));
    }

    [Fact]
    public async Task DelayedServerCommitAfterClientTimeoutCannotBeReplayed()
    {
        using var f = new Fixture();
        f.Server.Failure = "late";
        var preview = await f.Preview();
        var stopped = await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
        Assert.True(stopped.OutcomeUnknown);
        Assert.Equal(f.Original, await File.ReadAllBytesAsync(f.Path, Token));
        try
        {
            // Unchanged bytes now do not prove that the server is finished.
            await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
            Assert.Equal(1, f.Server.PutCount);
        }
        finally { f.Server.ReleaseLate.TrySetResult(); await f.Server.DelayedCommit!.WaitAsync(TimeSpan.FromSeconds(10), Token); }
        Assert.NotEqual(f.Original, await File.ReadAllBytesAsync(f.Path, Token));
        var restore = await f.Preview(new() { ["memsize"] = "6144", ["numvcpus"] = "4" });
        await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(restore, restore.Id, Token));
        Assert.Equal(1, f.Server.PutCount);
    }

    [Fact]
    public async Task DifferentTargetsDoNotShareAGlobalLock()
    {
        using var first = new Fixture();
        using var second = new Fixture();
        first.Server.HoldPut = true;
        var preview = await first.Preview();
        var running = first.Manager().ApplyAsync(preview, preview.Id, Token);
        await first.Server.EnteredPut.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);
        try
        {
            var other = await second.Preview();
            await second.Manager().ApplyAsync(other, other.Id, Token).WaitAsync(TimeSpan.FromSeconds(10), Token);
            Assert.Equal(1, second.Server.PutCount);
        }
        finally { first.Server.ReleasePut.TrySetResult(); await running; }
    }

    [Fact]
    public async Task TheWindowsLeaseExcludesOtherProcessesAndIsReleasedAfterCompletion()
    {
        using var f = new Fixture();
        f.Server.HoldPut = true;
        var preview = await f.Preview();
        var running = f.Manager().ApplyAsync(preview, preview.Id, Token);
        await f.Server.EnteredPut.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);
        async Task<int> ReadFromAnotherProcess()
        {
            // Fixed read-only shell command, no interpolated paths; the fixture is the working directory.
            var start = new ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = f.Folder };
            start.ArgumentList.Add("/d"); start.ArgumentList.Add("/c");
            start.ArgumentList.Add("type synthetic.vmx.vmidentitylab-api.lease");
            using var child = Process.Start(start)!;
            await child.WaitForExitAsync(Token).WaitAsync(TimeSpan.FromSeconds(10), Token);
            return child.ExitCode;
        }
        try { Assert.NotEqual(0, await ReadFromAnotherProcess()); }
        finally { f.Server.ReleasePut.TrySetResult(); await running; }
        Assert.Equal(0, await ReadFromAnotherProcess());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationBeforeDispatchNeverSendsTheMutation(bool afterIntent)
    {
        using var f = new Fixture();
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Token);
        if (afterIntent) f.Files.AfterIntent = cancel.Cancel; else f.Files.AfterStage = cancel.Cancel;
        var preview = await f.Preview();
        var stopped = await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, cancel.Token));
        Assert.False(stopped.OutcomeUnknown);
        Assert.Equal(0, f.Server.PutCount);
        Assert.False(VmxApiConfigManager.HasPendingOperation(f.Path));
        Assert.Equal(f.Original, await File.ReadAllBytesAsync(f.Path, Token));
    }

    [Fact]
    public async Task CorruptPendingIntentIsPreservedAndBlocksRestartWithoutAnyApiRequest()
    {
        using var f = new Fixture();
        var pending = VmxApiConfigManager.PendingOperationPath(f.Path);
        await File.WriteAllTextAsync(pending, "{partial", Token);
        var preview = await f.Preview();
        var stopped = await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
        Assert.True(stopped.OutcomeUnknown);
        Assert.Equal("{partial", await File.ReadAllTextAsync(pending, Token));
        Assert.Empty(f.Server.Requests);
    }

    [Fact]
    public async Task LegacyHistoryRemainsReadableWithoutBeingReclassifiedAsApiHistory()
    {
        using var f = new Fixture();
        var preview = await f.Preview();
        var receipt = await f.Manager().ApplyAsync(preview, preview.Id, Token);
        var legacy = receipt with { Backend = "vmx-file", BeforeSettings = null, AfterSettings = null };
        var entry = new VmxBackupHistoryEntry(Guid.NewGuid(), DateTimeOffset.UtcNow, "apply", legacy);
        var directory = Path.Combine(f.Folder, "history");
        var store = new LocalVmxBackupHistory(directory);
        await store.SaveAsync(entry, Token);
        var file = Path.Combine(directory, entry.Id.ToString("N") + ".json");
        var json = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(file, Token))!.AsObject();
        json["schemaVersion"] = 1;
        json.Remove("backend"); json.Remove("beforeSettings"); json.Remove("afterSettings");
        await File.WriteAllTextAsync(file, json.ToJsonString(), Token);
        Assert.Equal(entry, Assert.Single((await store.ReadAsync(Token)).Entries));
        await Assert.ThrowsAsync<InvalidDataException>(() => VmxApiConfigManager.PrepareRestoreAsync(f.Path, legacy, Token));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HistoryRejectsBackendSubstitutionButKeepsItsEvidence(bool legacySchema)
    {
        using var f = new Fixture();
        var preview = await f.Preview();
        var entry = new VmxBackupHistoryEntry(Guid.NewGuid(), DateTimeOffset.UtcNow, "apply",
            await f.Manager().ApplyAsync(preview, preview.Id, Token));
        var directory = Path.Combine(f.Folder, "history");
        var store = new LocalVmxBackupHistory(directory);
        await store.SaveAsync(entry, Token);
        var file = Path.Combine(directory, entry.Id.ToString("N") + ".json");
        var json = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(file, Token))!.AsObject();
        if (legacySchema) json["schemaVersion"] = 1; else json["backend"] = "unknown-backend";
        await File.WriteAllTextAsync(file, json.ToJsonString(), Token);
        var loaded = await store.ReadAsync(Token);
        Assert.Empty(loaded.Entries);
        Assert.Equal(1, loaded.UnreadableFiles);
        Assert.True(File.Exists(file));
    }

    [Fact]
    public async Task HistoryFailureDoesNotRepeatTheApiUpdate()
    {
        using var f = new Fixture();
        var preview = await f.Preview();
        var error = await Assert.ThrowsAsync<VmxHistoryPersistenceException>(() =>
            new VmxConfigWorkflow(f.Manager(), new FailingHistory()).ApplyAsync(preview, preview.Id, false, Token));
        var store = new LocalVmxBackupHistory(Path.Combine(f.Folder, "history"));
        await store.SaveAsync(error.Entry, Token);
        Assert.Equal(error.Entry, Assert.Single((await store.ReadAsync(Token)).Entries));
        Assert.Equal(1, f.Server.PutCount);
    }

    [Fact]
    public async Task RestoreRejectsCandidateChangesOrTamperedBackupWithoutOverwritingThem()
    {
        using var f = new Fixture();
        var preview = await f.Preview();
        var receipt = await f.Manager().ApplyAsync(preview, preview.Id, Token);
        await File.AppendAllTextAsync(receipt.BackupPath, "# tampered", Token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => VmxApiConfigManager.PrepareRestoreAsync(f.Path, receipt, Token));
        await File.WriteAllBytesAsync(receipt.BackupPath, f.Original, Token);
        await File.WriteAllTextAsync(f.Path, (await File.ReadAllTextAsync(f.Path, Token)).Replace("4096", "6144", StringComparison.Ordinal), Token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => VmxApiConfigManager.PrepareRestoreAsync(f.Path, receipt, Token));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new VmxConfigManager().PrepareRestoreAsync(f.Path, receipt, Token));
    }

    private sealed class Fixture : IDisposable
    {
        public string Folder { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "VMIdentityLab-api-tests-" + Guid.NewGuid().ToString("N"));
        public string Path => System.IO.Path.Combine(Folder, "synthetic.vmx");
        public byte[] Original { get; } = Encoding.UTF8.GetBytes("# synthetic API fixture\nvirtualHW.version = \"19\"\nmemsize = \"6144\"\nnumvcpus = \"4\"\ncpuid.coresPerSocket = \"2\"\ndisplayName = \"合成 API 测试\"\n");
        public Server Server { get; }
        private readonly HttpClient client;
        public WorkstationRestClient Api { get; }
        public Files Files { get; } = new();
        public Fixture()
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Path, Original);
            Server = new(Path);
            client = new(Server) { BaseAddress = new Uri("http://127.0.0.1:8697/") };
            Api = new(client);
        }
        public VmxApiConfigManager Manager(Func<Version?>? version = null) => new(Api, version ?? (() => Version), new CandidatePolicy(), Files);
        public Task<VmxPreparedChange> Preview(Dictionary<string, string>? changes = null)
            => new VmxConfigManager().PrepareAsync(Path, Version, changes ?? Changes, Token);
        public void Dispose()
        {
            client.Dispose();
            var full = System.IO.Path.GetFullPath(Folder);
            var prefix = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "VMIdentityLab-api-tests-");
            if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid test cleanup path.");
            Directory.Delete(full, recursive: true);
        }
    }
    private sealed class CandidatePolicy : IVmxWritePolicy
    { public bool IsApproved(Version version, VmxConfigChange change) => true; } // Synthetic files only.
    private sealed class FailingHistory : IVmxBackupHistory
    {
        public Task SaveAsync(VmxBackupHistoryEntry entry, CancellationToken token = default) => throw new IOException("Synthetic history failure");
        public Task<VmxBackupHistoryResult> ReadAsync(CancellationToken token = default) => throw new NotSupportedException();
    }
    private sealed class Files : IVmxFileOperations
    {
        private readonly LocalVmxFileOperations actual = new();
        public string Failure { get; set; } = "";
        public Action? AfterStage { get; set; }
        public Action? AfterIntent { get; set; }
        public int ReplaceCount { get; private set; }
        public async Task WriteNewAsync(string path, byte[] bytes, CancellationToken token)
        {
            var backup = path.EndsWith(".bak", StringComparison.Ordinal);
            var staging = path.EndsWith(".tmp", StringComparison.Ordinal);
            if (Failure == "backup" && backup || Failure == "stage" && staging || Failure == "intent" && path.EndsWith(".json", StringComparison.Ordinal))
                throw new IOException("Synthetic file failure");
            if (Failure == "corruptBackup" && backup || Failure == "corruptStage" && staging
                || Failure == "corruptIntent" && path.EndsWith(".pending.json", StringComparison.Ordinal)) bytes = [1, 2, 3];
            await actual.WriteNewAsync(path, bytes, token);
            if (staging) AfterStage?.Invoke();
            if (path.EndsWith(".pending.json", StringComparison.Ordinal)) AfterIntent?.Invoke();
        }
        public void Replace(string temporaryPath, string targetPath, string replacedBackupPath)
        { ReplaceCount++; throw new InvalidOperationException("API transactions must never replace VMX files."); }
    }
    private sealed class Server(string path) : HttpMessageHandler
    {
        public string Id { get; set; } = "test-vm";
        public string Power { get; set; } = "poweredOff";
        public string Failure { get; set; } = "";
        public HttpStatusCode? RejectPut { get; set; }
        public bool SettingsMismatch { get; set; }
        public bool HoldPut { get; set; }
        public Action<int>? OnPower { get; set; }
        public Action<int>? OnInventory { get; set; }
        public Action? AfterPut { get; set; }
        public TaskCompletionSource EnteredPut { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleasePut { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseLate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task? DelayedCommit { get; private set; }
        public List<string> Requests { get; } = [];
        public int PutCount { get; private set; }
        private int powers, inventories;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request.Method + " " + request.RequestUri!.AbsolutePath);
            if (request.Method == HttpMethod.Get && request.RequestUri.AbsolutePath == "/api/vms")
            {
                OnInventory?.Invoke(++inventories);
                return Reply(new[] { new { id = Id, path } });
            }
            if (request.Method == HttpMethod.Get && request.RequestUri.AbsolutePath.EndsWith("/power", StringComparison.Ordinal))
            {
                OnPower?.Invoke(++powers);
                return Reply(new { power_state = Power });
            }
            if (request.Method == HttpMethod.Get) return Settings();
            Assert.Equal(HttpMethod.Put, request.Method);
            PutCount++;
            Assert.True(File.Exists(VmxApiConfigManager.PendingOperationPath(path)));
            using (var journal = JsonDocument.Parse(await File.ReadAllBytesAsync(VmxApiConfigManager.PendingOperationPath(path), cancellationToken)))
                Assert.Equal("workstation-rest-v1", journal.RootElement.GetProperty("backend").GetString());
            EnteredPut.TrySetResult();
            if (HoldPut) await ReleasePut.Task.WaitAsync(cancellationToken);
            if (RejectPut is not null) return Reply(new { message = "synthetic rejection" }, RejectPut.Value);
            var changes = JsonSerializer.Deserialize<Dictionary<string, int>>(await request.Content!.ReadAsStringAsync(cancellationToken))!;
            Assert.All(changes.Keys, key => Assert.True(key is "memory" or "processors"));
            var numeric = changes.ToDictionary(item => item.Key == "memory" ? "memsize" : "numvcpus", item => item.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            var text = await File.ReadAllTextAsync(path, cancellationToken);
            if (Failure == "partialConflict") numeric.Remove("numvcpus");
            foreach (var change in numeric)
                text = Regex.Replace(text, "(?m)^(" + Regex.Escape(change.Key) + "\\s*=\\s*\")\\d+(\")",
                    match => match.Groups[1].Value + change.Value + match.Groups[2].Value);
            if (Failure == "late")
            {
                DelayedCommit = CommitLaterAsync(text, Token);
                throw new TaskCanceledException("Synthetic client timeout before the server finishes");
            }
            await File.WriteAllTextAsync(path, text, cancellationToken);
            AfterPut?.Invoke();
            if (Failure == "timeout") throw new TaskCanceledException("Synthetic API timeout");
            if (Failure == "disconnect") throw new HttpRequestException("Synthetic disconnected response");
            if (Failure == "external") await File.AppendAllTextAsync(path, "# external edit\n", cancellationToken);
            if (Failure == "started") Power = "poweredOn";
            if (Failure == "partialConflict") return Reply(new { message = "synthetic conflict" }, HttpStatusCode.Conflict);
            if (Failure == "server") return Reply(new { message = "synthetic failure" }, HttpStatusCode.InternalServerError);
            if (Failure == "malformed") return new(HttpStatusCode.OK) { Content = new StringContent("not JSON") };
            return Settings(Failure == "wrongReply" ? "wrong-vm" : null);
        }
        private HttpResponseMessage Settings(string? id = null)
        {
            var values = VmxDocument.Parse(File.ReadAllBytes(path)).Entries.ToDictionary(item => item.Key, item => item.Value);
            return Reply(new { id = id ?? Id, cpu = new { processors = int.Parse(values["numvcpus"], System.Globalization.CultureInfo.InvariantCulture) },
                memory = int.Parse(values["memsize"], System.Globalization.CultureInfo.InvariantCulture) + (SettingsMismatch ? 4 : 0) });
        }
        private static HttpResponseMessage Reply(object value, HttpStatusCode status = HttpStatusCode.OK)
            => new(status) { Content = new StringContent(JsonSerializer.Serialize(value)) };
        private async Task CommitLaterAsync(string text, CancellationToken token)
        {
            await ReleaseLate.Task.WaitAsync(token);
            await File.WriteAllTextAsync(path, text, token);
        }
    }
}
