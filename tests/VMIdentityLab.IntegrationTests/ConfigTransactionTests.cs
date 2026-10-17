using System.Security.Cryptography;
using System.Text;
using VMIdentityLab.VMware;
using VMIdentityLab.VMware.Configuration;

namespace VMIdentityLab.IntegrationTests;

public sealed class ConfigTransactionTests
{
    private static readonly Version Version = new(17, 5, 2);
    private static readonly Dictionary<string, string> Change = new() { ["memsize"] = "8192" };

    [Fact]
    public async Task AppliesAndRestoresExactOriginalBytesWithFreshConfirmation()
    {
        using var fixture = new Fixture();
        var manager = fixture.Manager();
        var preview = await manager.PrepareAsync(fixture.Path, Version, Change, TestContext.Current.CancellationToken);
        var receipt = await manager.ApplyAsync(preview, preview.Id, TestContext.Current.CancellationToken);
        Assert.Equal(fixture.Original, await File.ReadAllBytesAsync(receipt.BackupPath, TestContext.Current.CancellationToken));
        Assert.Equal(preview.Plan.GetProposedBytes(), await File.ReadAllBytesAsync(fixture.Path, TestContext.Current.CancellationToken));
        Assert.Equal(receipt.AppliedHash, Hash(await File.ReadAllBytesAsync(fixture.Path, TestContext.Current.CancellationToken)));
        var restore = await manager.PrepareRestoreAsync(fixture.Path, receipt, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.ApplyAsync(restore, preview.Id, TestContext.Current.CancellationToken));
        var restored = await manager.ApplyAsync(restore, restore.Id, TestContext.Current.CancellationToken);
        Assert.Equal(fixture.Original, await File.ReadAllBytesAsync(fixture.Path, TestContext.Current.CancellationToken));
        Assert.Equal(receipt.AppliedHash, Hash(await File.ReadAllBytesAsync(restored.BackupPath, TestContext.Current.CancellationToken)));
    }

    [Theory]
    [InlineData(VmPowerState.Unknown, true)]
    [InlineData(VmPowerState.PoweredOn, true)]
    [InlineData(VmPowerState.Suspended, true)]
    [InlineData(VmPowerState.PoweredOff, false)]
    public async Task RefusesEveryUnsafePowerStateWithoutCreatingBackups(VmPowerState state, bool reliable)
    {
        using var fixture = new Fixture();
        var manager = fixture.Manager(new Probe(() => new(state, "synthetic", DateTimeOffset.UtcNow, reliable, "Synthetic state")));
        var preview = await manager.PrepareAsync(fixture.Path, Version, Change, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.ApplyAsync(preview, preview.Id, TestContext.Current.CancellationToken));
        Assert.Equal(fixture.Original, await File.ReadAllBytesAsync(fixture.Path, TestContext.Current.CancellationToken));
        Assert.Single(Directory.GetFiles(fixture.Folder));
    }

    [Theory]
    [InlineData(-60)]
    [InlineData(60)]
    public async Task RejectsStaleOrFutureStateEvidence(int offset)
    {
        using var fixture = new Fixture();
        var manager = fixture.Manager(new Probe(() => new(VmPowerState.PoweredOff, "synthetic", DateTimeOffset.UtcNow.AddSeconds(offset), true, "Synthetic")));
        var preview = await manager.PrepareAsync(fixture.Path, Version, Change, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.ApplyAsync(preview, preview.Id, TestContext.Current.CancellationToken));
        Assert.Single(Directory.GetFiles(fixture.Folder));
    }

    [Fact]
    public async Task ProductionPolicyAndMissingConfirmationPreventWrites()
    {
        using var fixture = new Fixture();
        var manager = new VmxConfigManager(new Probe());
        var preview = await manager.PrepareAsync(fixture.Path, Version, Change, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.ApplyAsync(preview, preview.Id, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Manager().ApplyAsync(preview, Guid.Empty, TestContext.Current.CancellationToken));
        Assert.Single(Directory.GetFiles(fixture.Folder));
    }

    [Theory]
    [InlineData(17, 6, 0)]
    [InlineData(16, 2, 4)]
    public async Task UnreviewedWorkstationVersionsCannotWrite(int major, int minor, int patch)
    {
        using var fixture = new Fixture();
        var manager = fixture.Manager();
        var preview = await manager.PrepareAsync(fixture.Path, new Version(major, minor, patch), Change, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.ApplyAsync(preview, preview.Id, TestContext.Current.CancellationToken));
        Assert.Single(Directory.GetFiles(fixture.Folder));
    }

    [Fact]
    public async Task DetectsExternalChangesAndExclusiveOccupancy()
    {
        using var fixture = new Fixture();
        var manager = fixture.Manager();
        var preview = await manager.PrepareAsync(fixture.Path, Version, Change, TestContext.Current.CancellationToken);
        var external = Encoding.UTF8.GetBytes("memsize = \"2048\"\n");
        await File.WriteAllBytesAsync(fixture.Path, external, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.ApplyAsync(preview, preview.Id, TestContext.Current.CancellationToken));
        Assert.Equal(external, await File.ReadAllBytesAsync(fixture.Path, TestContext.Current.CancellationToken));
        await File.WriteAllBytesAsync(fixture.Path, fixture.Original, TestContext.Current.CancellationToken);
        using (var busy = new FileStream(fixture.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            await Assert.ThrowsAsync<IOException>(() => manager.ApplyAsync(preview, preview.Id, TestContext.Current.CancellationToken));
        Assert.Single(Directory.GetFiles(fixture.Folder));
    }

    [Fact]
    public async Task RechecksPowerAfterStagingAndRetainsOriginalBackupOnFailure()
    {
        using var fixture = new Fixture();
        var calls = 0;
        var probe = new Probe(() => new(++calls == 1 ? VmPowerState.PoweredOff : VmPowerState.PoweredOn,
            "synthetic", DateTimeOffset.UtcNow, true, "Synthetic"));
        var manager = fixture.Manager(probe);
        var preview = await manager.PrepareAsync(fixture.Path, Version, Change, TestContext.Current.CancellationToken);
        var error = await Assert.ThrowsAsync<VmxTransactionException>(() => manager.ApplyAsync(preview, preview.Id, TestContext.Current.CancellationToken));
        Assert.Equal(fixture.Original, await File.ReadAllBytesAsync(fixture.Path, TestContext.Current.CancellationToken));
        Assert.Equal(fixture.Original, await File.ReadAllBytesAsync(error.BackupPath, TestContext.Current.CancellationToken));
        Assert.Equal(2, calls);
    }

    [Theory]
    [InlineData("backup")]
    [InlineData("staging")]
    [InlineData("replace")]
    public async Task InjectedFileFailuresNeverLoseOriginalBytes(string phase)
    {
        using var fixture = new Fixture();
        var operations = new FaultOperations(phase);
        var manager = fixture.Manager(files: operations);
        var preview = await manager.PrepareAsync(fixture.Path, Version, Change, TestContext.Current.CancellationToken);
        var error = await Assert.ThrowsAsync<VmxTransactionException>(() => manager.ApplyAsync(preview, preview.Id, TestContext.Current.CancellationToken));
        Assert.Equal(fixture.Original, await File.ReadAllBytesAsync(fixture.Path, TestContext.Current.CancellationToken));
        if (phase != "backup") Assert.Equal(fixture.Original, await File.ReadAllBytesAsync(error.BackupPath, TestContext.Current.CancellationToken));
        Assert.Equal(phase == "replace" ? 1 : 0, operations.ReplaceCalls);
    }

    [Fact]
    public async Task VerificationFailureDoesNotBlindlyOverwriteChangedTarget()
    {
        using var fixture = new Fixture();
        var manager = fixture.Manager(files: new FaultOperations("verify"));
        var preview = await manager.PrepareAsync(fixture.Path, Version, Change, TestContext.Current.CancellationToken);
        var error = await Assert.ThrowsAsync<VmxTransactionException>(() => manager.ApplyAsync(preview, preview.Id, TestContext.Current.CancellationToken));
        Assert.Equal("external-content", await File.ReadAllTextAsync(fixture.Path, TestContext.Current.CancellationToken));
        Assert.Equal(fixture.Original, await File.ReadAllBytesAsync(error.BackupPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RestoreRejectsTamperedBackupAndLaterExternalChanges()
    {
        using var fixture = new Fixture();
        var manager = fixture.Manager();
        var preview = await manager.PrepareAsync(fixture.Path, Version, Change, TestContext.Current.CancellationToken);
        var receipt = await manager.ApplyAsync(preview, preview.Id, TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(receipt.BackupPath, [1, 2, 3], TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.PrepareRestoreAsync(fixture.Path, receipt, TestContext.Current.CancellationToken));
        await File.WriteAllBytesAsync(receipt.BackupPath, fixture.Original, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(fixture.Path, "user-later-edits", TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.PrepareRestoreAsync(fixture.Path, receipt, TestContext.Current.CancellationToken));
        Assert.Equal("user-later-edits", await File.ReadAllTextAsync(fixture.Path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DetectsPathReplacementAfterConfirmationAndPreservesExternalContent()
    {
        using var fixture = new Fixture();
        var calls = 0;
        var probe = new Probe(() =>
        {
            if (++calls == 2)
            {
                var external = fixture.Path + ".external";
                File.WriteAllText(external, "external-replacement");
                File.Replace(external, fixture.Path, null);
            }
            return new(VmPowerState.PoweredOff, "synthetic", DateTimeOffset.UtcNow, true, "Synthetic");
        });
        var manager = fixture.Manager(probe);
        var preview = await manager.PrepareAsync(fixture.Path, Version, Change, TestContext.Current.CancellationToken);
        var error = await Assert.ThrowsAsync<VmxTransactionException>(() => manager.ApplyAsync(preview, preview.Id, TestContext.Current.CancellationToken));
        Assert.Equal("external-replacement", await File.ReadAllTextAsync(fixture.Path, TestContext.Current.CancellationToken));
        Assert.Equal(fixture.Original, await File.ReadAllBytesAsync(error.BackupPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LateExternalReplacementIsReportedAndItsBytesRemainRecoverable()
    {
        using var fixture = new Fixture();
        var manager = fixture.Manager(files: new FaultOperations("lateExternal"));
        var preview = await manager.PrepareAsync(fixture.Path, Version, Change, TestContext.Current.CancellationToken);
        var error = await Assert.ThrowsAsync<VmxTransactionException>(() => manager.ApplyAsync(preview, preview.Id, TestContext.Current.CancellationToken));
        Assert.NotNull(error.ReplacedBackupPath);
        Assert.Equal("user-later-edits", await File.ReadAllTextAsync(error.ReplacedBackupPath, TestContext.Current.CancellationToken));
        var retained = new List<byte[]>();
        foreach (var file in Directory.GetFiles(fixture.Folder))
            retained.Add(await File.ReadAllBytesAsync(file, TestContext.Current.CancellationToken));
        Assert.Contains(retained, bytes => bytes.SequenceEqual(Encoding.UTF8.GetBytes("user-later-edits")));
        Assert.Contains(retained, bytes => bytes.SequenceEqual(fixture.Original));
    }

    [Fact]
    public async Task RejectsCorruptBackupBeforeStagingOrReplacement()
    {
        using var fixture = new Fixture();
        var files = new FaultOperations("corruptBackup");
        var manager = fixture.Manager(files: files);
        var preview = await manager.PrepareAsync(fixture.Path, Version, Change, TestContext.Current.CancellationToken);
        var error = await Assert.ThrowsAsync<VmxTransactionException>(() => manager.ApplyAsync(preview, preview.Id, TestContext.Current.CancellationToken));
        Assert.Equal(fixture.Original, await File.ReadAllBytesAsync(fixture.Path, TestContext.Current.CancellationToken));
        Assert.False(File.Exists(error.TemporaryPath));
        Assert.Equal(0, files.ReplaceCalls);
    }

    [Fact]
    public async Task CancellationBeforeReplacementRetainsOriginalAndVerifiedBackup()
    {
        using var fixture = new Fixture();
        using var cancelled = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var calls = 0;
        var manager = fixture.Manager(new Probe(() =>
        {
            if (++calls == 2) cancelled.Cancel();
            return new(VmPowerState.PoweredOff, "synthetic", DateTimeOffset.UtcNow, true, "Synthetic");
        }));
        var preview = await manager.PrepareAsync(fixture.Path, Version, Change, TestContext.Current.CancellationToken);
        var error = await Assert.ThrowsAsync<VmxTransactionException>(() => manager.ApplyAsync(preview, preview.Id, cancelled.Token));
        Assert.IsAssignableFrom<OperationCanceledException>(error.InnerException);
        Assert.Equal(fixture.Original, await File.ReadAllBytesAsync(fixture.Path, TestContext.Current.CancellationToken));
        Assert.Equal(fixture.Original, await File.ReadAllBytesAsync(error.BackupPath, TestContext.Current.CancellationToken));
    }

    private sealed class Fixture : IDisposable
    {
        public string Folder { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "VMIdentityLab-config-tests-" + Guid.NewGuid().ToString("N"));
        public string Path => System.IO.Path.Combine(Folder, "synthetic.vmx");
        public byte[] Original { get; } = Encoding.UTF8.GetBytes("# synthetic\r\nvirtualHW.version = \"19\"\r\nmemsize = \"04096\" # keep\r\nnumvcpus = \"2\"\ncpuid.coresPerSocket = \"1\"\ndisplayName = \"合成测试\"\r\n");
        public Fixture() { Directory.CreateDirectory(Folder); File.WriteAllBytes(Path, Original); }
        public VmxConfigManager Manager(IVmPowerStateProbe? probe = null, IVmxFileOperations? files = null) =>
            new(probe ?? new Probe(), new FixturePolicy(), files);
        public void Dispose()
        {
            var expected = System.IO.Path.GetFullPath(Folder);
            var prefix = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "VMIdentityLab-config-tests-");
            if (!expected.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid test cleanup path.");
            foreach (var file in Directory.GetFiles(expected)) File.Delete(file);
            Directory.Delete(expected);
        }
    }

    private sealed class FixturePolicy : IVmxWritePolicy
    {
        public bool IsApproved(Version workstationVersion, VmxConfigChange change) => change.Key is "memsize" or "numvcpus";
    }
    private sealed class Probe(Func<PowerStateEvidence>? read = null) : IVmPowerStateProbe
    {
        public Task<PowerStateEvidence> ProbeAsync(string vmxPath, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(read?.Invoke() ?? new(VmPowerState.PoweredOff, "synthetic", DateTimeOffset.UtcNow, true, "Synthetic only"));
        }
    }
    private sealed class FaultOperations(string phase) : IVmxFileOperations
    {
        private readonly LocalVmxFileOperations actual = new();
        public int ReplaceCalls { get; private set; }
        public async Task WriteNewAsync(string path, byte[] bytes, CancellationToken token)
        {
            if (phase == "backup" && path.EndsWith(".bak", StringComparison.Ordinal)
                || phase == "staging" && path.EndsWith(".tmp", StringComparison.Ordinal)) throw new IOException("Synthetic write failure");
            await actual.WriteNewAsync(path, phase == "corruptBackup" && path.EndsWith(".bak", StringComparison.Ordinal) ? [1, 2, 3] : bytes, token);
        }
        public void Replace(string temporaryPath, string targetPath, string replacedBackupPath)
        {
            ReplaceCalls++;
            if (phase == "replace") throw new IOException("Synthetic replace failure");
            if (phase == "lateExternal")
            {
                var changed = targetPath + ".external";
                File.WriteAllText(changed, "user-later-edits");
                File.Replace(changed, targetPath, null);
            }
            if (phase == "verify")
            {
                File.WriteAllText(temporaryPath, "external-content");
            }
            actual.Replace(temporaryPath, targetPath, replacedBackupPath);
        }
    }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}
