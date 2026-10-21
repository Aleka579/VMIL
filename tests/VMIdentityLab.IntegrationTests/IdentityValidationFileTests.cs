using System.Text;
using VMIdentityLab.Core.Comparison;
using VMIdentityLab.Core.Reports;
using VMIdentityLab.VMware.Configuration;
using VMIdentityLab.Windows;

namespace VMIdentityLab.IntegrationTests;

public sealed class IdentityValidationFileTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private const string Mac = "00:50:56:00:00:01", NewMac = "00:50:56:00:00:02";
    private const string Uuid = "56 4d 11 22 33 44 55 66-77 88 99 aa bb cc dd ee";
    private const string WindowsUuid = "22114d56-4433-6655-7788-99aabbccddee";

    private sealed class Folder : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vmil-validation-" + Guid.NewGuid().ToString("N"));
        public Folder() => Directory.CreateDirectory(Path);
        public string File(string name) => System.IO.Path.Combine(Path, name);
        public void Dispose() => Directory.Delete(Path, true); // Only this test's new synthetic directory.
    }

    private sealed class Source(Exception? error = null, bool wait = false) : IWindowsDataSource, IValidationIdentitySource
    {
        public List<ObservationCategory> Categories { get; } = [];
        public int IdentityCalls { get; private set; }
        public Guid? SelectedAdapter { get; private set; }
        public Task<IReadOnlyList<Finding>> CollectCategoryAsync(ObservationCategory category, DateTimeOffset time, CancellationToken token)
        {
            Categories.Add(category);
            IReadOnlyList<Finding> rows = WindowsCollector.Definitions(category).Select(definition => new Finding
            {
                Id = definition.Id, Category = definition.Category, Source = definition.Source, Status = CollectionStatus.Success, ObservedAt = time,
                Value = definition.Id switch { "cpu.logicalProcessors" => FindingValue.FromNumber(4, ValueUnit.Count),
                    "system.memoryBytes" => FindingValue.FromNumber(6442450944m, ValueUnit.Bytes), "cpu.hypervisorPresent" => FindingValue.FromBoolean(true),
                    "pnp.vmwareDeviceCount" => FindingValue.FromNumber(1, ValueUnit.Count), "tools.installed" => FindingValue.FromBoolean(true),
                    "smbios.manufacturer" or "cpu.hypervisorVendor" => FindingValue.FromText("VMware"), _ => FindingValue.FromText("synthetic") }
            }).ToArray();
            return Task.FromResult(rows);
        }
        public async Task<IReadOnlyList<Finding>> CollectValidationIdentitiesAsync(DateTimeOffset time, CancellationToken token, Guid? networkAdapterId = null)
        {
            IdentityCalls++;
            SelectedAdapter = networkAdapterId;
            if (wait) await Task.Delay(Timeout.Infinite, token);
            if (error is not null) throw error;
            return new[] { "network.macAddress", "smbios.uuid", "smbios.serialNumber" }.Select(id => new Finding { Id = id, Category = "system",
                Source = FindingCatalog.ById[id].Source, ObservedAt = time, Status = CollectionStatus.Success,
                Value = FindingValue.FromText(id == "network.macAddress" ? Mac : id == "smbios.uuid" ? WindowsUuid : "DO-NOT-RETAIN-SERIAL") }).ToArray();
        }
    }

    [Fact]
    public async Task ExplicitCaptureSeparatesPrivateIdentitiesFromNormalProgramReportAndDoesNotKeepSerial()
    {
        var source = new Source();
        var snapshot = await new WindowsValidationCollector(source, source, () => false).CapturePrivateAsync(Guid.NewGuid(), 0, Token);
        Assert.Equal(5, source.Categories.Count); Assert.DoesNotContain(ObservationCategory.Identifiers, source.Categories); Assert.Equal(1, source.IdentityCalls);
        Assert.Equal(14, snapshot.Report.Findings.Count); Assert.Equal(2, snapshot.Identities.Count); Assert.False(snapshot.Elevated);
        Assert.Equal("consistent", snapshot.Report.VirtualizationEvidence.VmwareGuestEvidence);
        var privateText = Encoding.UTF8.GetString(ValidationSnapshotJson.SerializePrivate(snapshot));
        Assert.Contains(Mac, privateText, StringComparison.Ordinal); Assert.Contains(WindowsUuid, privateText, StringComparison.Ordinal);
        Assert.DoesNotContain("DO-NOT-RETAIN-SERIAL", privateText, StringComparison.Ordinal);
        Assert.DoesNotContain(Mac, Encoding.UTF8.GetString(ReportJson.Serialize(snapshot.Report)), StringComparison.Ordinal);
        Assert.DoesNotContain(WindowsUuid, HtmlReportRenderer.Render(snapshot.Report), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("accessDenied", CollectionStatus.AccessDenied)]
    [InlineData("failed", CollectionStatus.Failed)]
    [InlineData("timeout", CollectionStatus.Timeout)]
    public async Task PrivateSourceErrorsStayExplicitAndDoNotLeakExceptionText(string failure, CollectionStatus status)
    {
        var source = new Source(failure == "accessDenied" ? new UnauthorizedAccessException("RAW-SECRET") : new IOException("RAW-SECRET"), failure == "timeout");
        var snapshot = await new WindowsValidationCollector(source, source, () => false, TimeSpan.FromMilliseconds(20))
            .CapturePrivateAsync(Guid.NewGuid(), 0, Token);
        Assert.All(snapshot.Identities, row => Assert.Equal(status, row.Status));
        Assert.DoesNotContain("RAW-SECRET", Encoding.UTF8.GetString(ValidationSnapshotJson.SerializePrivate(snapshot)), StringComparison.Ordinal);
        Assert.All(snapshot.Report.Findings, row => Assert.Equal(CollectionStatus.Success, row.Status));
    }

    [Fact]
    public async Task CancelledCaptureAndEmptySessionCannotReadPrivateIdentitySource()
    {
        var source = new Source(); var collector = new WindowsValidationCollector(source, source, () => false);
        await Assert.ThrowsAsync<ArgumentException>(() => collector.CapturePrivateAsync(Guid.Empty, 0, Token));
        await Assert.ThrowsAsync<ArgumentException>(() => collector.CapturePrivateAsync(Guid.NewGuid(), 0, Token, Guid.Empty));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Token); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => collector.CapturePrivateAsync(Guid.NewGuid(), 0, cancellation.Token));
        Assert.Equal(0, source.IdentityCalls);
    }

    [Fact]
    public async Task PrivateInputsAndPublicJsonHtmlAreSeparateAndExistingFilesNeverOverwritten()
    {
        using var folder = new Folder(); var source = new Source();
        var adapter = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var before = await new WindowsValidationCollector(source, source, () => false).CapturePrivateAsync(Guid.NewGuid(), 0, Token, adapter);
        Assert.Equal(adapter, source.SelectedAdapter);
        Assert.Equal(adapter, before.NetworkAdapterId);
        var path = folder.File("PRIVATE-before.json");
        await ValidationSnapshotJson.SavePrivateAsync(path, before, Token);
        var saved = await File.ReadAllBytesAsync(path, Token);
        await Assert.ThrowsAsync<IOException>(() => ValidationSnapshotJson.SavePrivateAsync(path, before, Token));
        Assert.Equal(saved, await File.ReadAllBytesAsync(path, Token));
        var read = await ValidationSnapshotJson.ReadAsync(path, 0, Token);
        Assert.Equal(before.Identities, read.Identities);
        PrivateValidationSnapshot Stage(ValidationPhase phase) => read with { Phase = phase,
            Report = read.Report with { CollectedAt = read.Report.CollectedAt.AddMinutes((int)phase * 2) },
            Identities = read.Identities.Select(row => row with { ObservedAt = read.Report.CollectedAt.AddMinutes((int)phase * 2),
                Value = row.Id == "network.macAddress" && phase == ValidationPhase.After ? FindingValue.FromText(NewMac) : row.Value }).ToArray() };
        var result = IdentityValidationComparer.Compare(0, Stage(0), Stage(ValidationPhase.After), Stage(ValidationPhase.Restored));
        await ValidationSnapshotJson.SavePublicJsonAsync(folder.File("public.json"), result, Token);
        await ValidationSnapshotJson.SavePublicHtmlAsync(folder.File("public.html"), result, Token);
        foreach (var name in new[] { "public.json", "public.html" })
        {
            var text = await File.ReadAllTextAsync(folder.File(name), Token);
            Assert.DoesNotContain(Mac, text, StringComparison.Ordinal); Assert.DoesNotContain(NewMac, text, StringComparison.Ordinal);
            Assert.DoesNotContain(WindowsUuid, text, StringComparison.Ordinal); Assert.DoesNotContain(folder.Path, text, StringComparison.Ordinal);
            Assert.DoesNotContain(adapter.ToString(), text, StringComparison.OrdinalIgnoreCase);
        }
        await Assert.ThrowsAsync<IOException>(() => ValidationSnapshotJson.SavePublicJsonAsync(folder.File("public.json"), result, Token));
        Assert.Empty(Directory.GetFiles(folder.Path, "*.tmp"));
        Assert.Throws<ReportFormatException>(() => ReportJson.Deserialize(saved));
    }

    [Theory]
    [InlineData(ValidationField.StaticMac)]
    [InlineData(ValidationField.BiosUuid)]
    public async Task ExistingSchemaThreeRecordsProvideReadOnlyInverseEvidenceAndSmbiosByteOrder(ValidationField field)
    {
        using var folder = new Folder();
        var before = new VmxIdentitySettings(Mac, Uuid, 19, new string('A', 64));
        var after = field == ValidationField.StaticMac ? before with { MacAddress = NewMac }
            : before with { BiosUuid = "56 4d 88 77 66 55 44 33-22 11 aa bb cc dd ee ff" };
        var time = DateTimeOffset.UtcNow;
        VmxBackupReceipt Receipt(VmxIdentitySettings a, VmxIdentitySettings b) => new(folder.File("synthetic.vmx"), folder.File(Guid.NewGuid() + ".bak"),
            new string('B', 64), new string('C', 64), VmxIdentityConfigPlanner.VerifiedVersion)
            { Backend = VmxApiConfigManager.BackendName, BeforeIdentity = a, AfterIdentity = b };
        var apply = new VmxBackupHistoryEntry(Guid.NewGuid(), time, "apply", Receipt(before, after));
        var restore = new VmxBackupHistoryEntry(Guid.NewGuid(), time.AddMinutes(2), "restore", Receipt(after, before));
        var history = new LocalVmxBackupHistory(folder.Path);
        await history.SaveAsync(apply, Token); await history.SaveAsync(restore, Token);
        var readApply = await LocalVmxBackupHistory.ReadEntryAsync(folder.File(apply.Id.ToString("N") + ".json"), Token);
        var readRestore = await LocalVmxBackupHistory.ReadEntryAsync(folder.File(restore.Id.ToString("N") + ".json"), Token);
        var config = VmxIdentityValidation.FromRecords(readApply, readRestore);
        Assert.Equal(field, config.Field); Assert.Equal(WindowsUuid, config.BeforeUuid);
        Assert.Equal(field == ValidationField.BiosUuid ? "77884d56-5566-3344-2211-aabbccddeeff" : WindowsUuid, config.AfterUuid);
        Assert.Throws<InvalidDataException>(() => VmxIdentityValidation.FromRecords(readRestore, readApply));
        Assert.Throws<InvalidDataException>(() => VmxIdentityValidation.FromRecords(readApply, readRestore with { Receipt = readRestore.Receipt with { TargetPath = folder.File("other.vmx") } }));
        Assert.Throws<InvalidDataException>(() => VmxIdentityValidation.FromRecords(readApply, readRestore with { Receipt = readRestore.Receipt with { WorkstationVersion = new(17, 5, 2) } }));
        Assert.Equal(2, Directory.GetFiles(folder.Path).Length); // No VMX, backup, API or restore operation.
    }
}
