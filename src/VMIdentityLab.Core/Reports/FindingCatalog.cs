using System.Collections.Frozen;

namespace VMIdentityLab.Core.Reports;

public sealed record FindingDefinition(string Id, string Category, string Source,
    string ChineseTitle, string EnglishTitle, DataSensitivity Sensitivity = DataSensitivity.Public);

public static class FindingCatalog
{
    private static readonly FindingDefinition[] Definitions =
    [
        new("smbios.manufacturer", "smbios", "Win32_ComputerSystem.Manufacturer", "系统厂商", "System manufacturer"),
        new("smbios.model", "smbios", "Win32_ComputerSystem.Model", "系统型号", "System model"),
        new("smbios.biosVersion", "smbios", "Win32_BIOS.SMBIOSBIOSVersion", "BIOS 版本", "BIOS version"),
        new("cpu.model", "cpu", "Win32_Processor.Name", "处理器型号", "Processor model"),
        new("cpu.logicalProcessors", "cpu", "Environment.ProcessorCount", "逻辑处理器", "Logical processors"),
        new("cpu.hypervisorPresent", "cpu", "CPUID.1.ECX[31]", "Hypervisor 信号", "Hypervisor signal"),
        new("cpu.hypervisorVendor", "cpu", "CPUID.40000000", "Hypervisor 厂商", "Hypervisor vendor"),
        new("pnp.vmwareDeviceCount", "pnp", "Win32_PnPEntity.Manufacturer/Name", "VMware 设备数", "VMware device count"),
        new("tools.installed", "tools", "HKLM/Uninstall.DisplayName (VMware Tools)", "VMware Tools 安装痕迹", "VMware Tools installation"),
        new("tools.version", "tools", "HKLM/Uninstall.DisplayVersion (VMware Tools)", "VMware Tools 版本", "VMware Tools version"),
        new("system.os", "system", "Win32_OperatingSystem.Caption", "操作系统", "Operating system"),
        new("system.build", "system", "Win32_OperatingSystem.BuildNumber", "系统 Build", "OS build"),
        new("system.architecture", "system", "RuntimeInformation.OSArchitecture", "系统架构", "OS architecture"),
        new("system.memoryBytes", "system", "Win32_ComputerSystem.TotalPhysicalMemory", "物理内存", "Physical memory"),
        new("smbios.serialNumber", "smbios", "identifier", "序列号（脱敏）", "Serial number (redacted)", DataSensitivity.Identifier),
        new("smbios.uuid", "smbios", "identifier", "UUID（脱敏）", "UUID (redacted)", DataSensitivity.Identifier),
        new("network.macAddress", "system", "identifier", "MAC（脱敏）", "MAC (redacted)", DataSensitivity.Identifier)
    ];

    public static FrozenDictionary<string, FindingDefinition> ById { get; } =
        Definitions.ToFrozenDictionary(item => item.Id, StringComparer.Ordinal);

    public static string Title(string id, bool english = false) => ById.TryGetValue(id, out var definition)
        ? english ? definition.EnglishTitle : definition.ChineseTitle : id;
}
