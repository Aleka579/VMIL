namespace VMIdentityLab.VMware;

public enum VmPowerState { Unknown, PoweredOn, PoweredOff, Suspended }

public sealed record PowerStateEvidence(VmPowerState State, string Source, DateTimeOffset ObservedAt,
    bool IsReliable, string Explanation)
{
    public static PowerStateEvidence Unknown(string explanation) =>
        new(VmPowerState.Unknown, "unavailable", DateTimeOffset.UtcNow, false, explanation);

    // This interprets supplied output; it never starts vmrun or changes a VM.
    public static PowerStateEvidence FromRunningList(string targetVmx, string output, bool querySucceeded)
    {
        if (!querySucceeded) return Unknown("The state query failed.");
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        const string prefix = "Total running VMs: ";
        if (lines.Length == 0 || !lines[0].StartsWith(prefix, StringComparison.Ordinal)
            || !int.TryParse(lines[0][prefix.Length..], out var count) || count != lines.Length - 1)
            return Unknown("The state response is not recognized.");
        try
        {
            var target = Path.GetFullPath(targetVmx);
            if (lines.Skip(1).Any(line => string.Equals(Path.GetFullPath(line), target, StringComparison.OrdinalIgnoreCase)))
                return new(VmPowerState.PoweredOn, "vmrun.list", DateTimeOffset.UtcNow, true, "The target was listed as running at query time.");
        }
        catch (ArgumentException) { return Unknown("The state response contains an invalid path."); }
        return Unknown("Absence from a running list does not prove that the VM is powered off; it may be suspended or unavailable.");
    }
}

public interface IVmPowerStateProbe
{
    Task<PowerStateEvidence> ProbeAsync(string vmxPath, CancellationToken cancellationToken);
}

public sealed class UnavailablePowerStateProbe : IVmPowerStateProbe
{
    public Task<PowerStateEvidence> ProbeAsync(string vmxPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(PowerStateEvidence.Unknown("No maintained VMware version and validated state provider have been configured."));
    }
}
