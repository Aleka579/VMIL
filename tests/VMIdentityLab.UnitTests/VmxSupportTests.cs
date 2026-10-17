using System.Text;
using VMIdentityLab.VMware;

namespace VMIdentityLab.UnitTests;

public sealed class VmxSupportTests
{
    [Theory]
    [InlineData("19", VmxSupportStatus.Confirmed)]
    [InlineData("20", VmxSupportStatus.Confirmed)]
    [InlineData("21", VmxSupportStatus.Unsupported)]
    [InlineData("22", VmxSupportStatus.Unsupported)]
    [InlineData("invalid", VmxSupportStatus.Unknown)]
    public void HardwareVersionUsesTheReviewedProductScope(string value, VmxSupportStatus expected)
    {
        var doc = VmxDocument.Parse(Encoding.UTF8.GetBytes($"virtualHW.version = \"{value}\"\n"));
        var result = VmxSupportCatalog.Analyze(doc, Assert.Single(doc.Entries), new Version(17, 5, 2, 23775571));
        Assert.Equal(expected, result.Status);
        Assert.Equal(VmxSupportCatalog.HardwareSource, result.Source);
    }

    [Theory]
    [InlineData("memsize")]
    [InlineData("numvcpus")]
    [InlineData("firmware")]
    [InlineData("unknown.key")]
    public void ReadableValuesDoNotAcquireUnreviewedSupport(string key)
    {
        var doc = VmxDocument.Parse(Encoding.UTF8.GetBytes($"{key} = \"4\"\n"));
        Assert.Equal(VmxSupportStatus.Unknown,
            VmxSupportCatalog.Analyze(doc, Assert.Single(doc.Entries), new Version(17, 5, 2)).Status);
    }

    [Fact]
    public void OtherOrUnavailableProductsAreNotClaimedAsValidated()
    {
        var doc = VmxDocument.Parse(Encoding.UTF8.GetBytes("virtualHW.version = \"20\"\n"));
        foreach (var version in new Version?[] { null, new(17, 5, 1), new(17, 6, 0), new(25, 0, 0) })
            Assert.Equal(VmxSupportStatus.Unknown, VmxSupportCatalog.Analyze(doc, Assert.Single(doc.Entries), version).Status);
    }

    [Fact]
    public void DuplicatesKeepTheirAmbiguousStatus()
    {
        var doc = VmxDocument.Parse(Encoding.UTF8.GetBytes("virtualHW.version = \"19\"\nvirtualHW.version = \"20\"\n"));
        foreach (var entry in doc.Entries)
            Assert.Equal(VmxSupportStatus.Ambiguous, VmxSupportCatalog.Analyze(doc, entry, new Version(17, 5, 2)).Status);
    }
}
