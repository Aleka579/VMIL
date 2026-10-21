using System.Text;
using System.Text.Json;
using VMIdentityLab.Core.Experiments;
using VMIdentityLab.VMware;
using VMIdentityLab.VMware.Configuration;

namespace VMIdentityLab.IntegrationTests;

public sealed class IdentityExperimentFileTests
{
    [Fact]
    public async Task PreviewAndExportLeaveConfigurationUnchangedAndRefuseOldOutputOrVmxTargets()
    {
        var folder = Path.Combine(Path.GetTempPath(), "VMIL-M10-files-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var input = Path.Combine(folder, "synthetic.vmx");
        var output = Path.Combine(folder, "plan.json");
        var original = Encoding.UTF8.GetBytes("# synthetic only\r\nvirtualHW.version = \"19\"\r\nethernet0.address = \"00:50:56:00:00:01\"\r\nunknown.entry = \"retain\"\r\n");
        var token = TestContext.Current.CancellationToken;
        try
        {
            await File.WriteAllBytesAsync(input, original, token);
            var snapshot = await new VmxReader().ReadAsync(input, token);
            var plan = IdentityExperimentPlanner.Create(input, snapshot.Document, IdentityExperimentCatalog.ResearchBaseline,
                snapshot.PowerState, IdentityExperimentKind.MacUniqueness);
            await plan.SaveReportAsync(output, token);
            var reportBytes = await File.ReadAllBytesAsync(output, token);
            using var report = JsonDocument.Parse(reportBytes);
            Assert.Equal("notExecuted", report.RootElement.GetProperty("status").GetString());
            Assert.False(report.RootElement.GetProperty("canExecute").GetBoolean());
            Assert.Equal(original, await File.ReadAllBytesAsync(input, token));
            await Assert.ThrowsAsync<IOException>(() => plan.SaveReportAsync(output, token));
            Assert.Equal(reportBytes, await File.ReadAllBytesAsync(output, token));
            await Assert.ThrowsAsync<ArgumentException>(() => plan.SaveReportAsync(input, token));
            Assert.Equal(original, await File.ReadAllBytesAsync(input, token));
            Assert.Equal(2, Directory.GetFiles(folder).Length);
        }
        finally { File.Delete(output); File.Delete(input); Directory.Delete(folder); }
    }

    [Fact]
    public async Task ExternalCommentEditInvalidatesAReadOnlyPlanWithoutWritingAnySidecar()
    {
        var folder = Path.Combine(Path.GetTempPath(), "VMIL-M10-stale-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var input = Path.Combine(folder, "synthetic.vmx");
        var token = TestContext.Current.CancellationToken;
        try
        {
            await File.WriteAllTextAsync(input, "uuid.bios = \"00 11 22 33 44 55 66 77-88 99 aa bb cc dd ee ff\"\n", token);
            var snapshot = await new VmxReader().ReadAsync(input, token);
            var plan = IdentityExperimentPlanner.Create(input, snapshot.Document, null, snapshot.PowerState, IdentityExperimentKind.UuidOnCopy);
            Assert.True(plan.IsCurrent(input, snapshot.Document, null, snapshot.PowerState, IdentityExperimentKind.UuidOnCopy));
            await File.AppendAllTextAsync(input, "# external synthetic edit\n", token);
            var refreshed = await new VmxReader().ReadAsync(input, token);
            Assert.False(plan.IsCurrent(input, refreshed.Document, null, snapshot.PowerState, IdentityExperimentKind.UuidOnCopy));
            Assert.Single(Directory.GetFiles(folder));
            Assert.EndsWith("# external synthetic edit\n", await File.ReadAllTextAsync(input, token), StringComparison.Ordinal);
        }
        finally { File.Delete(input); Directory.Delete(folder); }
    }
}
