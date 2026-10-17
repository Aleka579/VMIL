using System.Text;
using VMIdentityLab.VMware;
using VMIdentityLab.VMware.Configuration;

namespace VMIdentityLab.UnitTests;

public sealed class ConfigPlanTests
{
    private const string Text = "# preserved\r\nvirtualHW.version = \"19\"\r\n  MemSize = \"4096\" # memory\nnumvcpus = \"2\"\rcpuid.coresPerSocket = \"1\"\r\ndisplayName = \"合成配置\"\nunknown.setting = \"retain\"";

    [Theory]
    [InlineData("utf-8", false)]
    [InlineData("utf-8", true)]
    [InlineData("utf-16", true)]
    [InlineData("utf-16BE", true)]
    [InlineData("gbk", false)]
    public void PreservesEncodingBomCommentsUnknownKeysAndLineEndings(string name, bool bom)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding(name);
        var text = name == "gbk" ? ".encoding = \"GBK\"\r\n" + Text : Text;
        var original = (bom ? encoding.GetPreamble() : []).Concat(encoding.GetBytes(text)).ToArray();
        var document = VmxDocument.Parse(original);
        var plan = VmxConfigPlanner.Create(document, new Dictionary<string, string> { ["memsize"] = "8192" });
        var expected = (bom ? encoding.GetPreamble() : []).Concat(encoding.GetBytes(text.Replace("\"4096\"", "\"8192\""))).ToArray();
        Assert.Equal(expected, plan.GetProposedBytes());
        Assert.Equal(original, document.GetOriginalBytes());
        Assert.Single(plan.Changes);
        var returned = plan.GetProposedBytes();
        returned[0] ^= 1;
        Assert.Equal(expected, plan.GetProposedBytes());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-4")]
    [InlineData("4097")]
    [InlineData(" 4096")]
    [InlineData("4096 ")]
    [InlineData("2147483648")]
    [InlineData("4\n# injected")]
    [InlineData("\"\ncommand = \"x")]
    public void RejectsInvalidNumericInput(string value) => Assert.Throws<ArgumentException>(() =>
        VmxConfigPlanner.Create(VmxDocument.Parse(Encoding.UTF8.GetBytes(Text)), new Dictionary<string, string> { ["memsize"] = value }));

    [Theory]
    [InlineData("hypervisor.cpuid.v0")]
    [InlineData("uuid.bios")]
    [InlineData("isolation.tools.copy.disable")]
    [InlineData("virtualHW.version")]
    [InlineData("cpuid.coresPerSocket")]
    public void RejectsEveryNonCandidateKey(string key) => Assert.Throws<ArgumentException>(() =>
        VmxConfigPlanner.Create(VmxDocument.Parse(Encoding.UTF8.GetBytes(Text)), new Dictionary<string, string> { [key] = "4" }));

    [Theory]
    [InlineData("memsize = \"4096\"\nMEMSIZE = \"4096\"\n")]
    [InlineData("memsize = \"4096\"\nunknown broken line\n")]
    [InlineData("memsize = \"4096\"\nencryption.keySafe = \"opaque\"\n")]
    [InlineData(".encoding = \"unsupported\"\nmemsize = \"4096\"\n")]
    public void RejectsAmbiguousEncryptedOrUnsupportedInput(string text) => Assert.Throws<InvalidDataException>(() =>
        VmxConfigPlanner.Create(VmxDocument.Parse(Encoding.UTF8.GetBytes(text)), new Dictionary<string, string> { ["memsize"] = "8192" }));

    [Fact]
    public void RejectsMissingCandidatesAndNoChange()
    {
        var document = VmxDocument.Parse(Encoding.UTF8.GetBytes("memsize = \"4096\"\n"));
        Assert.Throws<InvalidDataException>(() => VmxConfigPlanner.Create(document, new Dictionary<string, string> { ["numvcpus"] = "4" }));
        Assert.Throws<ArgumentException>(() => VmxConfigPlanner.Create(document, new Dictionary<string, string> { ["memsize"] = "4096" }));
    }

    [Fact]
    public void PreservesDeclaredCpuTopologyAndMarksMissingTopology()
    {
        var document = VmxDocument.Parse(Encoding.UTF8.GetBytes(Text.Replace("coresPerSocket = \"1\"", "coresPerSocket = \"2\"")));
        Assert.Throws<ArgumentException>(() => VmxConfigPlanner.Create(document, new Dictionary<string, string> { ["numvcpus"] = "3" }));
        Assert.False(VmxConfigPlanner.Create(document, new Dictionary<string, string> { ["numvcpus"] = "4" }).RequiresTopologyReview);
        var without = VmxDocument.Parse(Encoding.UTF8.GetBytes("numvcpus = \"2\"\n"));
        Assert.True(VmxConfigPlanner.Create(without, new Dictionary<string, string> { ["numvcpus"] = "4" }).RequiresTopologyReview);
        var invalidOriginal = VmxDocument.Parse(Encoding.UTF8.GetBytes(Text.Replace("coresPerSocket = \"1\"", "coresPerSocket = \"3\"")));
        Assert.Throws<ArgumentException>(() => VmxConfigPlanner.Create(invalidOriginal, new Dictionary<string, string> { ["numvcpus"] = "6" }));
        var invalidMemory = VmxDocument.Parse(Encoding.UTF8.GetBytes(Text.Replace("\"4096\"", "\"4097\"")));
        Assert.Throws<ArgumentException>(() => VmxConfigPlanner.Create(invalidMemory, new Dictionary<string, string> { ["memsize"] = "8192" }));
    }

    [Fact]
    public void ProductionPolicyHasNoWritableParameters()
    {
        var policy = new ReleasedVmxWritePolicy();
        Assert.Equal(2, ReleasedVmxWritePolicy.Candidates.Count);
        foreach (var candidate in ReleasedVmxWritePolicy.Candidates)
            Assert.False(policy.IsApproved(new Version(17, 5, 2), new(candidate.Key, "2", "4", candidate.Unit)));
    }
}
