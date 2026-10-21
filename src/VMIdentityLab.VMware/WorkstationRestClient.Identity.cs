using System.Net;
using System.Text;
using System.Text.Json;
using VMIdentityLab.VMware.Configuration;

namespace VMIdentityLab.VMware;

public sealed record WorkstationIdentitySettings(string MacAddress, string BiosUuid);

public sealed partial class WorkstationRestClient
{
    public async Task<WorkstationIdentitySettings> ReadIdentityAsync(string id, CancellationToken token = default)
    {
        var prefix = VmPath(id) + "/params/";
        async Task<string> Read(string name)
        {
            using var document = await GetAsync(prefix + name, token);
            if (String(document.RootElement, "name") != name) throw InvalidResponse();
            return String(document.RootElement, "value");
        }
        var mac = await Read("ethernet0.address");
        var uuid = await Read("uuid.bios");
        if (!VmxIdentityConfigPlanner.IsMac(mac) || !VmxIdentityConfigPlanner.IsUuid(uuid)) throw InvalidResponse();
        return new(mac, uuid);
    }

    // Narrow API primitive, not field admission. The transaction owns version,
    // confirmation, policy, power, backup and independent whole-file readback.
    public async Task UpdateIdentityAsync(string id, VmxConfigChange change, CancellationToken token = default)
    {
        VmxIdentityConfigPlanner.ValidateChange(change);
        using var budget = Budget(token);
        token = budget.Token;
        using var request = new HttpRequestMessage(HttpMethod.Put, VmPath(id) + "/params")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { name = change.Key, value = change.After }),
                Encoding.UTF8, "application/vnd.vmware.vmw.rest-v1+json")
        };
        request.Content.Headers.ContentType!.CharSet = null;
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        if (response.StatusCode != HttpStatusCode.OK) throw new WorkstationApiException(response.StatusCode);
        var bytes = await ReadBytesAsync(response, token);
        // Pinned vmrest 1.3.1 returns an empty 200; an optional ErrorModel must
        // still be valid. Neither response substitutes for independent readback.
        if (bytes.Length == 0) return;
        using var result = JsonDocument.Parse(bytes, new() { MaxDepth = 16 });
        if (Integer(result.RootElement, "code") != 0) throw InvalidResponse();
        _ = String(result.RootElement, "message");
    }
}
