namespace VMIdentityLab.VMware;

public sealed record VmxReadResult(VmxDocument Document, PowerStateEvidence PowerState);

public sealed class VmxReader(IVmPowerStateProbe? stateProbe = null)
{
    public const int MaxVmxBytes = 2 * 1024 * 1024;
    private readonly IVmPowerStateProbe stateProbe = stateProbe ?? new UnavailablePowerStateProbe();

    public async Task<VmxReadResult> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(Path.GetExtension(path), ".vmx", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Select a VMX configuration file.");
        var fullPath = Path.GetFullPath(path);
        byte[] bytes;
        await using (var file = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, true))
        {
            if (file.Length > MaxVmxBytes) throw new InvalidDataException("VMX exceeds the 2 MiB limit.");
            bytes = new byte[(int)file.Length];
            await file.ReadExactlyAsync(bytes, cancellationToken);
        }
        var document = VmxDocument.Parse(bytes);
        PowerStateEvidence state;
        try { state = await stateProbe.ProbeAsync(fullPath, cancellationToken); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException)
        { state = PowerStateEvidence.Unknown("State information is unavailable."); }
        return new(document, state);
    }
}
