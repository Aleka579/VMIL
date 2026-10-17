using System.Net;
using System.Text;
using System.Text.Json;
using VMIdentityLab.VMware.Configuration;

namespace VMIdentityLab.VMware;

public sealed record WorkstationVmSettings(string Id, int MemoryMiB, int Processors);
public sealed class WorkstationApiException(HttpStatusCode statusCode)
    : IOException("The local VMware API rejected the request.")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

// Caller owns authentication and HttpClient. No retries, power actions or file-write fallback.
public sealed class WorkstationRestClient
{
    private readonly HttpClient client;
    public WorkstationRestClient(HttpClient client)
    {
        var address = client.BaseAddress;
        if (address is null || !address.IsLoopback || address.AbsolutePath != "/"
            || address.Scheme is not ("http" or "https") || address.UserInfo.Length != 0
            || address.Query.Length != 0 || address.Fragment.Length != 0)
            throw new ArgumentException("A local VMware API root address is required.", nameof(client));
        this.client = client;
    }

    public async Task<string> ResolveVmAsync(string vmxPath, CancellationToken token = default)
    {
        var target = Path.GetFullPath(vmxPath);
        using var inventory = await GetAsync("api/vms", token);
        if (inventory.RootElement.ValueKind != JsonValueKind.Array) throw InvalidResponse();
        string? targetId = null;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var vm in inventory.RootElement.EnumerateArray())
        {
            var id = String(vm, "id");
            var path = String(vm, "path");
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(path)
                || !Path.IsPathFullyQualified(path) || !ids.Add(id)) throw InvalidResponse();
            if (!Path.GetFullPath(path).Equals(target, StringComparison.OrdinalIgnoreCase)) continue;
            if (targetId is not null) throw InvalidResponse();
            targetId = id;
        }
        return targetId ?? throw new InvalidDataException("The selected VM has no unique API registration.");
    }

    public async Task<PowerStateEvidence> ReadPowerAsync(string id, CancellationToken token = default)
    {
        using var power = await GetAsync(VmPath(id) + "/power", token);
        var state = String(power.RootElement, "power_state") switch
        {
            "poweredOn" => VmPowerState.PoweredOn,
            "poweredOff" => VmPowerState.PoweredOff,
            "suspended" => VmPowerState.Suspended,
            _ => VmPowerState.Unknown
        };
        return state == VmPowerState.Unknown ? PowerStateEvidence.Unknown("Unrecognized VM power state.")
            : new(state, "vmrest.power", DateTimeOffset.UtcNow, true,
                "Authenticated local API state for the registered target at query time.");
    }

    public async Task<WorkstationVmSettings> ReadSettingsAsync(string id, CancellationToken token = default)
    {
        using var document = await GetAsync(VmPath(id), token);
        return Settings(document.RootElement, id);
    }

    public async Task<WorkstationVmSettings> UpdateAsync(string id, IReadOnlyList<VmxConfigChange> changes,
        CancellationToken token = default)
    {
        using var budget = Budget(token);
        token = budget.Token;
        var body = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var change in changes)
        {
            var name = change.Key switch { "memsize" => "memory", "numvcpus" => "processors",
                _ => throw new ArgumentException("Only reviewed memory/CPU settings may be sent.") };
            body.Add(name, VmxConfigPlanner.PositiveNumber(change.After));
        }
        if (body.Count is < 1 or > 2) throw new ArgumentException("A reviewed change is required.");
        using var request = new HttpRequestMessage(HttpMethod.Put, VmPath(id))
        { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/vnd.vmware.vmw.rest-v1+json") };
        // vmrest 1.3.1 rejects the otherwise valid charset parameter (HTTP 406).
        // Keep UTF-8 bytes while sending the exact vendor media type used by its contract.
        request.Content.Headers.ContentType!.CharSet = null;
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        if (response.StatusCode != HttpStatusCode.OK) throw new WorkstationApiException(response.StatusCode);
        using var document = await ReadJsonAsync(response, token);
        return Settings(document.RootElement, id);
    }

    private async Task<JsonDocument> GetAsync(string path, CancellationToken token)
    {
        using var budget = Budget(token);
        token = budget.Token;
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        if (response.StatusCode != HttpStatusCode.OK) throw new WorkstationApiException(response.StatusCode);
        return await ReadJsonAsync(response, token);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken token)
    {
        await using var input = await response.Content.ReadAsStreamAsync(token);
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await input.ReadAsync(buffer, token)) != 0)
        {
            if (output.Length + count > VmxReader.MaxVmxBytes) throw InvalidResponse();
            output.Write(buffer, 0, count);
        }
        return JsonDocument.Parse(output.ToArray(), new() { MaxDepth = 16 });
    }

    private static WorkstationVmSettings Settings(JsonElement value, string expectedId)
    {
        var id = String(value, "id");
        var memory = Integer(value, "memory");
        var processors = Integer(Property(value, "cpu"), "processors");
        if (id != expectedId || memory <= 0 || processors <= 0) throw InvalidResponse();
        return new(id, memory, processors);
    }

    private static string VmPath(string id)
        => "api/vms/" + Uri.EscapeDataString(string.IsNullOrWhiteSpace(id) || id is "." or ".." ? throw InvalidResponse() : id);
    private static JsonElement Property(JsonElement value, string name)
    {
        if (value.ValueKind != JsonValueKind.Object) throw InvalidResponse();
        var properties = value.EnumerateObject().Where(item => item.NameEquals(name)).ToArray();
        return properties.Length == 1 ? properties[0].Value : throw InvalidResponse();
    }
    private static string String(JsonElement value, string name)
    {
        var property = Property(value, name);
        return property.ValueKind == JsonValueKind.String ? property.GetString()! : throw InvalidResponse();
    }
    private static int Integer(JsonElement value, string name)
    {
        var property = Property(value, name);
        return property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number) ? number : throw InvalidResponse();
    }
    private static InvalidDataException InvalidResponse() => new("The local VMware API response is incomplete or ambiguous.");
    private CancellationTokenSource Budget(CancellationToken token)
    {
        var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        // ResponseHeadersRead ends HttpClient's timeout before content consumption.
        budget.CancelAfter(client.Timeout == Timeout.InfiniteTimeSpan ? TimeSpan.FromSeconds(10) : client.Timeout);
        return budget;
    }
}
