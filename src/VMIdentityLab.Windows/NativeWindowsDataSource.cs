using System.Buffers.Binary;
using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using Microsoft.Win32;
using VMIdentityLab.Core.Reports;

namespace VMIdentityLab.Windows;

public sealed class NativeWindowsDataSource : IWindowsDataSource
{
    // At most one WMI query can run. A timed-out WMI call cannot accumulate workers.
    private readonly SemaphoreSlim nativeGate = new(1, 1);

    public async Task<IReadOnlyList<Finding>> CollectCategoryAsync(ObservationCategory category, DateTimeOffset observedAt, CancellationToken cancellationToken)
    {
        // CPU observations are local instructions/properties and must not queue behind slow WMI.
        if (category == ObservationCategory.Cpu)
            return await Task.Run(() => Read(category, observedAt, cancellationToken), cancellationToken);

        await nativeGate.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() => Read(category, observedAt, cancellationToken), cancellationToken);
        }
        finally { nativeGate.Release(); }
    }

    private static IReadOnlyList<Finding> Read(ObservationCategory category, DateTimeOffset time, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        token.ThrowIfCancellationRequested();
        Finding Item(string id, FindingValue value, CollectionStatus status = CollectionStatus.Success) => new()
        {
            Id = id, Category = FindingCatalog.ById[id].Category, Source = FindingCatalog.ById[id].Source,
            ObservedAt = time, Status = status, Value = value
        };
        Finding Text(string id, QueryResult result, string property)
        {
            var text = result.Values.GetValueOrDefault(property)?.ToString()?.Trim();
            return Item(id, string.IsNullOrEmpty(text) ? new() : FindingValue.FromText(text),
                result.Status != CollectionStatus.Success ? result.Status : string.IsNullOrEmpty(text) ? CollectionStatus.NotSupported : CollectionStatus.Success);
        }

        switch (category)
        {
            case ObservationCategory.Smbios:
                var system = QueryOne("SELECT Manufacturer, Model FROM Win32_ComputerSystem", ["Manufacturer", "Model"], token);
                var bios = QueryOne("SELECT SMBIOSBIOSVersion FROM Win32_BIOS", ["SMBIOSBIOSVersion"], token);
                return [Text("smbios.manufacturer", system, "Manufacturer"), Text("smbios.model", system, "Model"), Text("smbios.biosVersion", bios, "SMBIOSBIOSVersion")];
            case ObservationCategory.Cpu:
                var cpu = new List<Finding> { Item("cpu.logicalProcessors", FindingValue.FromNumber(Environment.ProcessorCount, ValueUnit.Count)) };
                if (!X86Base.IsSupported)
                {
                    cpu.Add(Item("cpu.model", new(), CollectionStatus.NotSupported));
                    cpu.Add(Item("cpu.hypervisorPresent", new(), CollectionStatus.NotSupported));
                    cpu.Add(Item("cpu.hypervisorVendor", new(), CollectionStatus.NotSupported));
                }
                else
                {
                    var name = CpuBrandString.Read(leaf => X86Base.CpuId(leaf, 0));
                    cpu.Add(Item("cpu.model", name is null ? new() : FindingValue.FromText(name),
                        name is null ? CollectionStatus.NotSupported : CollectionStatus.Success));
                    var present = (X86Base.CpuId(1, 0).Ecx & unchecked((int)0x80000000)) != 0;
                    cpu.Add(Item("cpu.hypervisorPresent", FindingValue.FromBoolean(present)));
                    if (present)
                    {
                        var vendor = X86Base.CpuId(0x40000000, 0);
                        Span<byte> bytes = stackalloc byte[12];
                        BinaryPrimitives.WriteInt32LittleEndian(bytes, vendor.Ebx);
                        BinaryPrimitives.WriteInt32LittleEndian(bytes[4..], vendor.Ecx);
                        BinaryPrimitives.WriteInt32LittleEndian(bytes[8..], vendor.Edx);
                        cpu.Add(Item("cpu.hypervisorVendor", FindingValue.FromText(Encoding.ASCII.GetString(bytes).TrimEnd('\0'))));
                    }
                    else cpu.Add(Item("cpu.hypervisorVendor", new(), CollectionStatus.NotSupported));
                }
                return cpu;
            case ObservationCategory.Pnp:
                try
                {
                    using var searcher = Searcher("SELECT Name, Manufacturer FROM Win32_PnPEntity WHERE Manufacturer LIKE '%VMware%' OR Name LIKE '%VMware%'");
                    using var results = searcher.Get();
                    var count = 0;
                    foreach (ManagementObject device in results) { using (device) { token.ThrowIfCancellationRequested(); count++; } }
                    return [Item("pnp.vmwareDeviceCount", FindingValue.FromNumber(count, ValueUnit.Count))];
                }
                catch (ManagementException error) { return [Item("pnp.vmwareDeviceCount", new(), Classify(error))]; }
            case ObservationCategory.Tools:
                var versions = new List<string>();
                var found = false;
                var checkedViews = 0;
                foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                {
                    using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                    using var uninstall = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                    if (uninstall is null) continue;
                    checkedViews++;
                    foreach (var name in uninstall.GetSubKeyNames())
                    {
                        token.ThrowIfCancellationRequested();
                        using var key = uninstall.OpenSubKey(name);
                        if (!string.Equals(key?.GetValue("DisplayName") as string, "VMware Tools", StringComparison.OrdinalIgnoreCase)) continue;
                        found = true;
                        if (key?.GetValue("DisplayVersion") is string version && version.Length != 0) versions.Add(version);
                    }
                }
                if (checkedViews == 0) throw new NotSupportedException("Installer metadata is unavailable.");
                return [Item("tools.installed", FindingValue.FromBoolean(found)),
                    Item("tools.version", versions.Count == 0 ? new() : FindingValue.FromText(string.Join(", ", versions.Distinct())),
                        versions.Count == 0 ? CollectionStatus.NotSupported : CollectionStatus.Success)];
            case ObservationCategory.System:
                var os = QueryOne("SELECT Caption, BuildNumber FROM Win32_OperatingSystem", ["Caption", "BuildNumber"], token);
                var computer = QueryOne("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem", ["TotalPhysicalMemory"], token);
                var memory = decimal.TryParse(Convert.ToString(computer.Values.GetValueOrDefault("TotalPhysicalMemory"), CultureInfo.InvariantCulture),
                    NumberStyles.Integer, CultureInfo.InvariantCulture, out var capacity);
                return [Text("system.os", os, "Caption"), Text("system.build", os, "BuildNumber"),
                    Item("system.architecture", FindingValue.FromText(RuntimeInformation.OSArchitecture.ToString())),
                    Item("system.memoryBytes", memory ? FindingValue.FromNumber(capacity, ValueUnit.Bytes) : new(),
                        computer.Status != CollectionStatus.Success ? computer.Status : memory ? CollectionStatus.Success : CollectionStatus.NotSupported)];
            default:
                throw new NotSupportedException();
        }
    }

    private sealed record QueryResult(Dictionary<string, object?> Values, CollectionStatus Status);
    private static ManagementObjectSearcher Searcher(string query) => new(new ManagementScope("root\\cimv2"),
        new ObjectQuery(query), new System.Management.EnumerationOptions { ReturnImmediately = false, Timeout = TimeSpan.FromSeconds(2) });

    private static QueryResult QueryOne(string query, string[] properties, CancellationToken token)
    {
        try
        {
            using var searcher = Searcher(query);
            using var results = searcher.Get();
            foreach (ManagementObject item in results)
            {
                using (item)
                {
                    token.ThrowIfCancellationRequested();
                    return new(properties.ToDictionary(property => property, property => (object?)item[property], StringComparer.Ordinal), CollectionStatus.Success);
                }
            }
            return new([], CollectionStatus.NotSupported);
        }
        catch (ManagementException error) { return new([], Classify(error)); }
        catch (UnauthorizedAccessException) { return new([], CollectionStatus.AccessDenied); }
    }

    private static CollectionStatus Classify(ManagementException error) => error.ErrorCode switch
    {
        ManagementStatus.AccessDenied => CollectionStatus.AccessDenied,
        ManagementStatus.InvalidClass or ManagementStatus.NotFound => CollectionStatus.NotSupported,
        ManagementStatus.Timedout => CollectionStatus.Timeout,
        _ => CollectionStatus.Failed
    };
}
