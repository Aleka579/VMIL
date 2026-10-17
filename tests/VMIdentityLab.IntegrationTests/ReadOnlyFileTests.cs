using System.Text;
using VMIdentityLab.Core.Reports;
using VMIdentityLab.VMware;

namespace VMIdentityLab.IntegrationTests;

public sealed class ReadOnlyFileTests
{
    [Fact]
    public async Task VmxReaderLeavesTheTemporaryFileUnchanged()
    {
        var folder = Path.Combine(Path.GetTempPath(), "VMIdentityLab-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "synthetic.vmx");
        try
        {
            var original = Encoding.UTF8.GetBytes(".encoding = \"UTF-8\"\r\nmemsize = \"4096\"\r\n");
            await File.WriteAllBytesAsync(path, original, TestContext.Current.CancellationToken);
            var result = await new VmxReader().ReadAsync(path, TestContext.Current.CancellationToken);
            Assert.Equal(original, result.Document.GetOriginalBytes());
            Assert.Equal(original, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
            Assert.Equal(VmPowerState.Unknown, result.PowerState.State);
        }
        finally { File.Delete(path); Directory.Delete(folder); }
    }

    [Fact]
    public async Task ExportRequiresConfirmationBeforeReplacingExistingReport()
    {
        var folder = Path.Combine(Path.GetTempPath(), "VMIdentityLab-export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "synthetic.json");
        try
        {
            var original = Encoding.UTF8.GetBytes("user-owned-synthetic-content");
            await File.WriteAllBytesAsync(path, original, TestContext.Current.CancellationToken);
            var report = new DiagnosticReport { CollectedAt = DateTimeOffset.UtcNow };
            await Assert.ThrowsAsync<IOException>(() => ReportFiles.SaveJsonAsync(path, report, cancellationToken: TestContext.Current.CancellationToken));
            Assert.Equal(original, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
            Assert.Single(Directory.GetFiles(folder));
            await ReportFiles.SaveJsonAsync(path, report, true, TestContext.Current.CancellationToken);
            var parsed = await ReportJson.ReadAsync(path, TestContext.Current.CancellationToken);
            Assert.Equal("redacted", parsed.PrivacyMode);
        }
        finally { File.Delete(path); Directory.Delete(folder); }
    }

    [Fact]
    public async Task ReportExportRejectsVmxTargetsBeforeWriting()
    {
        var path = Path.Combine(Path.GetTempPath(), "VMIdentityLab-protected-" + Guid.NewGuid().ToString("N") + ".vmx");
        try
        {
            var original = Encoding.UTF8.GetBytes("memsize = \"4096\"\n");
            await File.WriteAllBytesAsync(path, original, TestContext.Current.CancellationToken);
            var report = new DiagnosticReport { CollectedAt = DateTimeOffset.UtcNow };
            await Assert.ThrowsAsync<ArgumentException>(() => ReportFiles.SaveJsonAsync(path, report, true, TestContext.Current.CancellationToken));
            Assert.Equal(original, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task ReportReaderRejectsOversizedTemporaryInput()
    {
        var path = Path.Combine(Path.GetTempPath(), "VMIdentityLab-report-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            await File.WriteAllBytesAsync(path, new byte[ReportJson.MaxReportBytes + 1], TestContext.Current.CancellationToken);
            await Assert.ThrowsAsync<ReportFormatException>(() => ReportJson.ReadAsync(path, TestContext.Current.CancellationToken));
        }
        finally { File.Delete(path); }
    }
}
