using System.Buffers.Binary;
using System.Text;
using VMIdentityLab.Windows;

namespace VMIdentityLab.IntegrationTests;

public sealed class CpuBrandStringTests
{
    [Fact]
    public void UnsupportedExtendedLeavesAreNotQueried()
    {
        var calls = new List<int>();
        var name = CpuBrandString.Read(leaf =>
        {
            calls.Add(leaf);
            return leaf == unchecked((int)0x80000000u)
                ? (unchecked((int)0x80000003u), 0, 0, 0)
                : throw new InvalidOperationException("Unsupported leaf was queried");
        });
        Assert.Null(name);
        Assert.Equal([unchecked((int)0x80000000u)], calls);
    }

    [Fact]
    public void BrandUsesAllRegistersInDocumentedLittleEndianOrder()
    {
        var data = new Dictionary<int, (int, int, int, int)>
        {
            [unchecked((int)0x80000000u)] = (unchecked((int)0x80000004u), 0, 0, 0),
            [unchecked((int)0x80000002u)] = (0x44434241, 0x48474645, 0x4c4b4a49, 0x504f4e4d),
            [unchecked((int)0x80000003u)] = (0x54535251, 0x58575655, 0x62615a59, 0x66656463),
            [unchecked((int)0x80000004u)] = (0x6a696867, 0x6e6d6c6b, 0x7271706f, 0x76757473)
        };
        Assert.Equal("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuv", CpuBrandString.Read(leaf => data[leaf]));
    }

    [Theory]
    [InlineData("    Synthetic Processor 9000    ", "Synthetic Processor 9000")]
    [InlineData("Synthetic CPU\0ignored trailing data", "Synthetic CPU")]
    [InlineData("", null)]
    [InlineData("                                                ", null)]
    public void BrandHandlesPaddingTerminationAndUnavailableText(string raw, string? expected)
    {
        var bytes = new byte[48];
        Encoding.ASCII.GetBytes(raw).CopyTo(bytes, 0);
        var actual = CpuBrandString.Read(leaf =>
        {
            if (leaf == unchecked((int)0x80000000u))
                return (unchecked((int)0x80000004u), 0, 0, 0);
            var offset = checked((int)(unchecked((uint)leaf) - 0x80000002u)) * 16;
            return (BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset)),
                BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset + 4)),
                BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset + 8)),
                BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset + 12)));
        });
        Assert.Equal(expected, actual);
    }
}
