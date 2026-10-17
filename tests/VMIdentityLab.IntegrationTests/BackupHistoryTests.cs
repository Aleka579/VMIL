using System.Text;
using VMIdentityLab.VMware;
using VMIdentityLab.VMware.Configuration;

namespace VMIdentityLab.IntegrationTests;

public sealed class BackupHistoryTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task MissingHistoryIsEmptyAndDoesNotCreateDirectory()
    {
        using var fixture = new Fixture();
        var result = await fixture.History.ReadAsync(Token);
        Assert.Empty(result.Entries);
        Assert.Equal(0, result.UnreadableFiles);
        Assert.False(Directory.Exists(fixture.HistoryDirectory));
    }

    [Fact]
    public async Task ApplyAndRestoreRemainRecoverableAfterRestart()
    {
        using var fixture = new Fixture();
        var manager = fixture.Manager();
        var preview = await fixture.PreviewAsync(manager);
        var applied = await new VmxConfigWorkflow(manager, fixture.History).ApplyAsync(preview, preview.Id, false, Token);
        var loaded = await new LocalVmxBackupHistory(fixture.HistoryDirectory).ReadAsync(Token);
        Assert.Equal(applied, Assert.Single(loaded.Entries));
        var restore = await manager.PrepareRestoreAsync(fixture.Path, loaded.Entries[0].Receipt, Token);
        await new VmxConfigWorkflow(manager, fixture.History).ApplyAsync(restore, restore.Id, true, Token);
        var history = await fixture.History.ReadAsync(Token);
        Assert.Equal(2, history.Entries.Count);
        Assert.Contains(history.Entries, entry => entry.Operation == "restore");
        Assert.Equal(fixture.Original, await File.ReadAllBytesAsync(fixture.Path, Token));
    }

    [Fact]
    public async Task ProductionApprovalAndWrongConfirmationCannotCreateHistory()
    {
        using var fixture = new Fixture();
        var production = new VmxConfigManager(new Probe());
        var preview = await fixture.PreviewAsync(production);
        Assert.False(production.IsWriteApproved(preview));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new VmxConfigWorkflow(production, fixture.History)
            .ApplyAsync(preview, preview.Id, false, Token));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new VmxConfigWorkflow(fixture.Manager(), fixture.History)
            .ApplyAsync(preview, Guid.Empty, false, Token));
        Assert.False(Directory.Exists(fixture.HistoryDirectory));
        Assert.Equal(fixture.Original, await File.ReadAllBytesAsync(fixture.Path, Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HistoryFailureRetainsAppliedStateAndCanBeSavedWithoutApplyingAgain(bool invalidData)
    {
        using var fixture = new Fixture();
        var manager = fixture.Manager();
        var preview = await fixture.PreviewAsync(manager);
        var error = await Assert.ThrowsAsync<VmxHistoryPersistenceException>(() => new VmxConfigWorkflow(manager, new FailingHistory(invalidData))
            .ApplyAsync(preview, preview.Id, false, Token));
        Assert.Equal(preview.Plan.GetProposedBytes(), await File.ReadAllBytesAsync(fixture.Path, Token));
        Assert.Equal(fixture.Original, await File.ReadAllBytesAsync(error.Entry.Receipt.BackupPath, Token));
        var retainedBackups = Directory.GetFiles(fixture.Folder, "*.bak").Order().ToArray();
        await fixture.History.SaveAsync(error.Entry, Token);
        Assert.Equal(error.Entry, Assert.Single((await fixture.History.ReadAsync(Token)).Entries));
        Assert.Equal(preview.Plan.GetProposedBytes(), await File.ReadAllBytesAsync(fixture.Path, Token));
        Assert.Equal(retainedBackups, Directory.GetFiles(fixture.Folder, "*.bak").Order().ToArray());
    }

    [Fact]
    public async Task MalformedOversizedAndRenamedRecordsRemainVisibleAsUnreadable()
    {
        using var fixture = new Fixture();
        var manager = fixture.Manager();
        var preview = await fixture.PreviewAsync(manager);
        var entry = await new VmxConfigWorkflow(manager, fixture.History).ApplyAsync(preview, preview.Id, false, Token);
        await File.WriteAllTextAsync(System.IO.Path.Combine(fixture.HistoryDirectory, "broken.json"), "{bad", Token);
        await File.WriteAllTextAsync(System.IO.Path.Combine(fixture.HistoryDirectory, "large.json"), new string('x', 32769), Token);
        await File.WriteAllTextAsync(System.IO.Path.Combine(fixture.HistoryDirectory, "interrupted.json.tmp"), "incomplete", Token);
        File.Copy(System.IO.Path.Combine(fixture.HistoryDirectory, entry.Id.ToString("N") + ".json"),
            System.IO.Path.Combine(fixture.HistoryDirectory, "renamed.json"));
        var result = await fixture.History.ReadAsync(Token);
        Assert.Equal(entry, Assert.Single(result.Entries));
        Assert.Equal(4, result.UnreadableFiles);
        Assert.Equal(4, Directory.GetFiles(fixture.HistoryDirectory, "*.json").Length);
    }

    [Fact]
    public async Task DuplicateRecordIsNotOverwritten()
    {
        using var fixture = new Fixture();
        var manager = fixture.Manager();
        var preview = await fixture.PreviewAsync(manager);
        var entry = await new VmxConfigWorkflow(manager, fixture.History).ApplyAsync(preview, preview.Id, false, Token);
        await Assert.ThrowsAsync<IOException>(() => fixture.History.SaveAsync(entry with { Operation = "restore" }, Token));
        Assert.Equal(entry, Assert.Single((await fixture.History.ReadAsync(Token)).Entries));
    }

    [Fact]
    public async Task InvalidReceiptCannotCreateHistory()
    {
        using var fixture = new Fixture();
        var invalid = new VmxBackupHistoryEntry(Guid.NewGuid(), DateTimeOffset.UtcNow, "apply",
            new("relative.vmx", "relative.bak", new string('A', 64), new string('B', 64), new(17, 5, 2)));
        await Assert.ThrowsAsync<InvalidDataException>(() => fixture.History.SaveAsync(invalid, Token));
        Assert.False(Directory.Exists(fixture.HistoryDirectory));
    }

    [Fact]
    public async Task CancellationBeforeHistoryOperationsCreatesNoFiles()
    {
        using var fixture = new Fixture();
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.History.ReadAsync(cancelled.Token));
        Assert.False(Directory.Exists(fixture.HistoryDirectory));
    }

    [Fact]
    public async Task OversizedMetadataCannotCreateAnUnreadableSuccessfulRecord()
    {
        using var fixture = new Fixture();
        var target = System.IO.Path.Combine(fixture.Folder, new string('x', 20000), "synthetic.vmx");
        var entry = new VmxBackupHistoryEntry(Guid.NewGuid(), DateTimeOffset.UtcNow, "apply",
            new(target, target + ".bak", new string('A', 64), new string('B', 64), new(17, 5, 2)));
        await Assert.ThrowsAsync<InvalidDataException>(() => fixture.History.SaveAsync(entry, Token));
        Assert.False(Directory.Exists(fixture.HistoryDirectory));
    }

    private sealed class Fixture : IDisposable
    {
        public string Folder { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "VMIdentityLab-history-tests-" + Guid.NewGuid().ToString("N"));
        public string Path => System.IO.Path.Combine(Folder, "synthetic.vmx");
        public string HistoryDirectory => System.IO.Path.Combine(Folder, "history");
        public LocalVmxBackupHistory History => new(HistoryDirectory);
        public byte[] Original { get; } = Encoding.UTF8.GetBytes("# synthetic, no disks\nvirtualHW.version = \"19\"\nmemsize = \"4096\"\nnumvcpus = \"2\"\ncpuid.coresPerSocket = \"1\"\n");
        public Fixture() { Directory.CreateDirectory(Folder); File.WriteAllBytes(Path, Original); }
        public VmxConfigManager Manager() => new(new Probe(), new FixturePolicy());
        public Task<VmxPreparedChange> PreviewAsync(VmxConfigManager manager) => manager.PrepareAsync(Path,
            new(17, 5, 2), new Dictionary<string, string> { ["memsize"] = "8192" }, Token);
        public void Dispose()
        {
            var full = System.IO.Path.GetFullPath(Folder);
            var prefix = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "VMIdentityLab-history-tests-");
            if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid cleanup path.");
            Directory.Delete(full, true);
        }
    }
    [Fact]
    public async Task ReadOnlyConfigurationCanBePreviewedButNotApplied()
    {
        using var fixture = new Fixture();
        var attributes = File.GetAttributes(fixture.Path);
        File.SetAttributes(fixture.Path, attributes | FileAttributes.ReadOnly);
        try
        {
            var manager = fixture.Manager();
            var preview = await fixture.PreviewAsync(manager);
            Assert.Single(preview.Plan.Changes);
            await Assert.ThrowsAsync<IOException>(() => manager.ApplyAsync(preview, preview.Id, Token));
            Assert.Single(Directory.GetFiles(fixture.Folder));
            Assert.Equal(fixture.Original, await File.ReadAllBytesAsync(fixture.Path, Token));
        }
        finally { File.SetAttributes(fixture.Path, attributes); }
    }
    private sealed class FixturePolicy : IVmxWritePolicy
    {
        public bool IsApproved(Version workstationVersion, VmxConfigChange change) => change.Key == "memsize";
    }
    private sealed class Probe : IVmPowerStateProbe
    {
        public Task<PowerStateEvidence> ProbeAsync(string vmxPath, CancellationToken token)
            => Task.FromResult(new PowerStateEvidence(VmPowerState.PoweredOff, "synthetic", DateTimeOffset.UtcNow, true, "Synthetic only"));
    }
    private sealed class FailingHistory(bool invalidData) : IVmxBackupHistory
    {
        public Task SaveAsync(VmxBackupHistoryEntry entry, CancellationToken token = default)
        {
            if (invalidData) throw new InvalidDataException("Synthetic invalid metadata");
            throw new IOException("Synthetic history failure");
        }
        public Task<VmxBackupHistoryResult> ReadAsync(CancellationToken token = default) => throw new NotSupportedException();
    }
}
