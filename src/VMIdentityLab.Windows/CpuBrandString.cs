using System.Buffers.Binary;
using System.Text;

namespace VMIdentityLab.Windows;

internal static class CpuBrandString
{
    // Intel/AMD extended CPUID leaves contain 48 ASCII bytes in EAX/EBX/ECX/EDX order.
    internal static string? Read(Func<int, (int Eax, int Ebx, int Ecx, int Edx)> cpuId)
    {
        if (unchecked((uint)cpuId(unchecked((int)0x80000000u)).Eax) < 0x80000004u)
            return null;

        Span<byte> bytes = stackalloc byte[48];
        for (var index = 0; index < 3; index++)
        {
            var registers = cpuId(unchecked((int)(0x80000002u + (uint)index)));
            var block = bytes.Slice(index * 16, 16);
            BinaryPrimitives.WriteInt32LittleEndian(block, registers.Eax);
            BinaryPrimitives.WriteInt32LittleEndian(block[4..], registers.Ebx);
            BinaryPrimitives.WriteInt32LittleEndian(block[8..], registers.Ecx);
            BinaryPrimitives.WriteInt32LittleEndian(block[12..], registers.Edx);
        }

        var terminator = bytes.IndexOf((byte)0);
        var name = Encoding.ASCII.GetString(terminator < 0 ? bytes : bytes[..terminator]).Trim();
        return name.Length == 0 ? null : name;
    }
}
