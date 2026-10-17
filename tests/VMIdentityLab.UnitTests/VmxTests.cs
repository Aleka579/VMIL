using System.Text;
using VMIdentityLab.VMware;

namespace VMIdentityLab.UnitTests;

public sealed class VmxTests
{
    [Theory]
    [InlineData("\r\n")]
    [InlineData("\n")]
    [InlineData("\r")]
    public void ParsingPreservesCommentsUnknownKeysAndOriginalBytes(string newline)
    {
        var text = $"# synthetic VMX{newline}.encoding = \"UTF-8\"{newline}memsize = \"4096\"{newline}custom.key = \"unchanged\"{newline}";
        var bytes = Encoding.UTF8.GetBytes(text);
        var document = VmxDocument.Parse(bytes);
        Assert.Equal(bytes, document.GetOriginalBytes());
        Assert.Equal("unchanged", Assert.Single(document.Entries, entry => entry.Key == "custom.key").Value);
        Assert.Equal(VmxLineKind.Comment, document.Lines[0].Kind);
        Assert.Contains(newline, document.Lines[0].Raw);
    }

    [Fact]
    public void Utf16BomAndNonAsciiValuesAreRetained()
    {
        var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("displayName = \"合成虚拟机\"\r\n")).ToArray();
        var document = VmxDocument.Parse(bytes);
        Assert.Equal(bytes, document.GetOriginalBytes());
        Assert.Equal("合成虚拟机", Assert.Single(document.Entries).Value);
    }

    [Fact]
    public void DeclaredWindows1252IsDecodedWithoutChangingBytes()
    {
        var bytes = Encoding.Latin1.GetBytes(".encoding = \"windows-1252\"\ndisplayName = \"Synthetic café\"\n");
        var document = VmxDocument.Parse(bytes);
        Assert.Equal("Synthetic café", Assert.Single(document.Entries, item => item.Key == "displayName").Value);
        Assert.Equal(bytes, document.GetOriginalBytes());
    }

    [Fact]
    public void DuplicateKeysAreAmbiguousAndNotExportedAsOneValue()
    {
        var document = VmxDocument.Parse(Encoding.UTF8.GetBytes("memsize = \"4096\"\nMEMSIZE = \"8192\"\n"));
        Assert.True(document.HasAmbiguity);
        Assert.Single(document.DuplicateKeys);
        Assert.Empty(document.ToSummary(PowerStateEvidence.Unknown("synthetic")).Declarations);
    }

    [Theory]
    [InlineData("GBK")]
    [InlineData("cp936")]
    [InlineData("windows-936")]
    public void DeclaredGbkPreservesChineseValuesBytesAndPublicDeclarations(string name)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var text = $".encoding = \"{name}\"\r\ndisplayName = \"合成虚拟机\"\r\nmemsize = \"4096\"\r\nnumvcpus = \"4\"\r\n";
        var bytes = Encoding.GetEncoding(936).GetBytes(text);
        var document = VmxDocument.Parse(bytes);
        Assert.Equal(bytes, document.GetOriginalBytes());
        Assert.Equal("合成虚拟机", Assert.Single(document.Entries, item => item.Key == "displayName").Value);
        var summary = document.ToSummary(PowerStateEvidence.Unknown("synthetic"));
        Assert.Equal("4096", summary.Declarations["memsize"]);
        Assert.Equal("4", summary.Declarations["numvcpus"]);
    }

    [Fact]
    public void InvalidGbkIsNotSilentlyReplaced()
    {
        var bytes = Encoding.ASCII.GetBytes(".encoding = \"GBK\"\ndisplayName = \"")
            .Concat(new byte[] { 0x81, 0x30, 0x22, 0x0A }).ToArray();
        var document = VmxDocument.Parse(bytes);
        Assert.Empty(document.Lines);
        Assert.NotEmpty(document.Limitations);
        Assert.Equal(bytes, document.GetOriginalBytes());
    }

    [Fact]
    public void UnrecognizedSyntaxAndEncodingRetainRawInput()
    {
        var bytes = Encoding.UTF8.GetBytes("not confirmed syntax\n");
        Assert.Equal(VmxLineKind.Unknown, Assert.Single(VmxDocument.Parse(bytes).Lines).Kind);
        var unsupported = Encoding.UTF8.GetBytes(".encoding = \"unknown-codepage\"\n");
        var document = VmxDocument.Parse(unsupported);
        Assert.Equal(unsupported, document.GetOriginalBytes());
        Assert.NotEmpty(document.Limitations);
    }

    [Fact]
    public void InvalidUtf8IsNotSilentlyReplaced()
    {
        var bytes = new byte[] { 0xFF, 0x00, 0x41 };
        var document = VmxDocument.Parse(bytes);
        Assert.Empty(document.Lines);
        Assert.NotEmpty(document.Limitations);
        Assert.Equal(bytes, document.GetOriginalBytes());
    }

    [Fact]
    public void VmxSummaryExportsOnlyReviewedDeclarationFields()
    {
        var text = "guestOS = \"windows9-64\"\nmemsize = \"4096\"\nnumvcpus = \"4\"\nuuid.bios = \"synthetic-private-uuid\"\nscsi0:0.fileName = \"C:\\private\\disk.vmdk\"\n";
        var summary = VmxDocument.Parse(Encoding.UTF8.GetBytes(text)).ToSummary(PowerStateEvidence.Unknown("synthetic"));
        Assert.Equal(3, summary.Declarations.Count);
        Assert.False(summary.Declarations.ContainsKey("uuid.bios"));
        Assert.Equal("unknown", summary.PowerState);
    }

    [Theory]
    [InlineData("Total running VMs: 0", true)]
    [InlineData("malformed response", true)]
    [InlineData("Total running VMs: 0", false)]
    public void MissingRunningEntryNeverImpliesPoweredOff(string output, bool succeeded)
    {
        var state = PowerStateEvidence.FromRunningList(@"C:\synthetic\test.vmx", output, succeeded);
        Assert.Equal(VmPowerState.Unknown, state.State);
        Assert.False(state.IsReliable);
    }

    [Fact]
    public void RunningListEvidenceIdentifiesOnlyTheListedTarget()
    {
        var state = PowerStateEvidence.FromRunningList(@"C:\synthetic\test.vmx", "Total running VMs: 1\nC:\\synthetic\\test.vmx", true);
        Assert.Equal(VmPowerState.PoweredOn, state.State);
        Assert.True(state.IsReliable);
    }
}
