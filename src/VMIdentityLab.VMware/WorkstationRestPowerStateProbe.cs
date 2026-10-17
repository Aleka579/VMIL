using System.Text.Json;

namespace VMIdentityLab.VMware;

// Caller owns the authenticated HttpClient. Only inventory/state GET requests are made.
public sealed class WorkstationRestPowerStateProbe : IVmPowerStateProbe
{
    private readonly WorkstationRestClient api;

    public WorkstationRestPowerStateProbe(HttpClient client)
    {
        api = new(client);
    }

    public async Task<PowerStateEvidence> ProbeAsync(string vmxPath, CancellationToken cancellationToken)
    {
        try
        {
            var id = await api.ResolveVmAsync(vmxPath, cancellationToken);
            return await api.ReadPowerAsync(id, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return PowerStateEvidence.Unknown("The API query timed out."); }
        catch (Exception error) when (error is HttpRequestException or JsonException or IOException or InvalidDataException
            or ArgumentException or NotSupportedException)
        { return PowerStateEvidence.Unknown("The API query or target verification failed."); }
    }

}
