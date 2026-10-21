using VMIdentityLab.VMware.Configuration;

namespace VMIdentityLab.App.Infrastructure;

internal static class ConfigPresentation
{
    internal static string Value(VmxConfigChange change, bool after, bool reveal = false)
        => VmxIdentityConfigPlanner.IsIdentityKey(change.Key) && !reveal
            ? Localization.Text("IdentityConfig.Hidden")
            : (after ? change.After : change.Before) + (change.Unit == "MiB" ? " MiB" : "");

    internal static string Impact(VmxConfigChange change) => Localization.Text(change.Key switch
    {
        "memsize" => "Config.MemoryImpact", "numvcpus" => "Config.CpuImpact",
        "ethernet0.address" => "IdentityConfig.MacImpact", "uuid.bios" => "IdentityConfig.UuidImpact",
        _ => throw new ArgumentException("Unknown configuration field.")
    });
}
