using System.Net;
using System.Text;
using System.Text.Json;
using VMIdentityLab.VMware;
using VMIdentityLab.VMware.Configuration;

namespace VMIdentityLab.UnitTests;

public sealed class IdentityConfigTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private const string Mac = "00:50:56:00:00:01";
    private const string Uuid = "56 4d 11 22 33 44 55 66-77 88 99 aa bb cc dd ee";
    private const string Source = "# synthetic only\nvirtualHW.version = \"19\"\nmemsize = \"6144\"\nnumvcpus = \"4\"\ncpuid.coresPerSocket = \"2\"\n"
        + "ethernet0.present = \"TRUE\"\nethernet0.addressType = \"static\"\nethernet0.connectionType = \"hostonly\"\nethernet0.startConnected = \"FALSE\"\n"
        + "ethernet0.address = \"" + Mac + "\"\nuuid.bios = \"" + Uuid + "\"\nunknown.field = \"retain\"\n";

    [Theory]
    [InlineData(IdentityConfigField.StaticMac, "ethernet0.address")]
    [InlineData(IdentityConfigField.BiosUuid, "uuid.bios")]
    public void GeneratesOneVendorFormatValueAndPreservesEveryOtherByte(IdentityConfigField field, string key)
    {
        var original = Encoding.UTF8.GetBytes(Source.Replace("\n", "\r\n", StringComparison.Ordinal));
        var document = VmxDocument.Parse(original);
        var plan = VmxIdentityConfigPlanner.Create(document, field);
        var change = Assert.Single(plan.Changes);
        Assert.Equal(key, change.Key);
        Assert.NotEqual(change.Before, change.After);
        Assert.Matches(field == IdentityConfigField.StaticMac
            ? "^00:50:56:[0-3][0-9a-f]:[0-9a-f]{2}:[0-9a-f]{2}$"
            : "^56 4d(?: [0-9a-f]{2}){6}-(?:[0-9a-f]{2} ){7}[0-9a-f]{2}$", change.After);
        Assert.Equal(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(original).Replace(change.Before, change.After, StringComparison.Ordinal)), plan.GetProposedBytes());
        Assert.Equal(original, document.GetOriginalBytes());
        Assert.True(plan.IsIdentityChange);
        Assert.False(plan.RequiresTopologyReview);
    }

    [Theory]
    [InlineData("virtualHW.version = \"19\"", "virtualHW.version = \"20\"")]
    [InlineData("ethernet0.addressType = \"static\"", "ethernet0.addressType = \"generated\"")]
    [InlineData("ethernet0.startConnected = \"FALSE\"", "ethernet0.startConnected = \"TRUE\"")]
    [InlineData("ethernet0.present = \"TRUE\"", "ethernet0.present = \"FALSE\"")]
    [InlineData("hostonly", "bridged")]
    [InlineData(Mac, "00:50:56:40:00:01")]
    [InlineData(Uuid, "11111111-2222-3333-4444-555555555555")]
    [InlineData("uuid.bios", "uuid.location")]
    public void UnsupportedSourceCannotProduceAPlan(string before, string after)
        => Assert.Throws<InvalidDataException>(() => VmxIdentityConfigPlanner.Create(
            VmxDocument.Parse(Encoding.UTF8.GetBytes(Source.Replace(before, after, StringComparison.Ordinal))), IdentityConfigField.StaticMac));

    [Theory]
    [InlineData("ethernet1.present = \"FALSE\"\n")]
    [InlineData("ethernet0.generatedAddress = \"00:0c:29:00:00:01\"\n")]
    [InlineData("uuid.BIOS = \"duplicate\"\n")]
    [InlineData("encryption.keySafe = \"opaque\"\n")]
    [InlineData("unparseable input\n")]
    public void UnsupportedExtraConfigurationCannotProduceAPlan(string extra)
        => Assert.Throws<InvalidDataException>(() => VmxIdentityConfigPlanner.Create(
            VmxDocument.Parse(Encoding.UTF8.GetBytes(Source + extra)), IdentityConfigField.BiosUuid));

    [Fact]
    public void UnknownEnumIsRejectedAndNumericPlannerRemainsNumericOnly()
    {
        var document = VmxDocument.Parse(Encoding.UTF8.GetBytes(Source));
        Assert.Throws<ArgumentException>(() => VmxIdentityConfigPlanner.Create(document, (IdentityConfigField)2));
        Assert.Throws<ArgumentException>(() => VmxConfigPlanner.Create(document, new Dictionary<string, string> { ["uuid.bios"] = Uuid }));
    }

    [Theory]
    [InlineData(IdentityConfigField.StaticMac)]
    [InlineData(IdentityConfigField.BiosUuid)]
    public async Task PutUsesTheObservedRouteVendorMimeAndOnlyOneClosedNameValuePair(IdentityConfigField field)
    {
        var change = Assert.Single(VmxIdentityConfigPlanner.Create(VmxDocument.Parse(Encoding.UTF8.GetBytes(Source)), field).Changes);
        using var handler = new Handler(request =>
        {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("/api/vms/x/params", request.RequestUri!.AbsolutePath);
            Assert.Equal("application/vnd.vmware.vmw.rest-v1+json", request.Content!.Headers.ContentType!.ToString());
            var body = JsonSerializer.Deserialize<Dictionary<string, string>>(request.Content.ReadAsStringAsync(Token).GetAwaiter().GetResult())!;
            Assert.Equal(2, body.Count);
            Assert.Equal(change.Key, body["name"]);
            Assert.Equal(change.After, body["value"]);
            return new(HttpStatusCode.OK) { Content = new ByteArrayContent([]) };
        });
        using var client = Client(handler);
        await new WorkstationRestClient(client).UpdateIdentityAsync("x", change, Token);
        Assert.Equal(1, handler.Count);
    }

    [Theory]
    [InlineData("ethernet0.generatedAddress", Mac, "00:50:56:00:00:02")]
    [InlineData("ethernet0.address", Mac, "00:50:56:40:00:02")]
    [InlineData("ethernet0.address", Mac, Mac)]
    [InlineData("uuid.bios", Uuid, "\"\nInjected=true")]
    [InlineData("uuid.location", Uuid, Uuid)]
    public async Task RawIdentityClientRejectsUnreviewedKeysValuesAndNoOpsBeforeNetwork(string key, string before, string after)
    {
        Assert.False(new ReleasedVmxApiWritePolicy().IsApproved(VmxIdentityConfigPlanner.VerifiedVersion, new(key, before, after, "")));
        using var handler = new Handler(_ => throw new InvalidOperationException("No request expected"));
        using var client = Client(handler);
        await Assert.ThrowsAsync<ArgumentException>(() => new WorkstationRestClient(client).UpdateIdentityAsync("x", new(key, before, after, ""), Token));
        Assert.Equal(0, handler.Count);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"code\":1,\"message\":\"failed\"}")]
    [InlineData("{\"code\":0,\"code\":1,\"message\":\"ambiguous\"}")]
    [InlineData("{\"code\":0,\"message\":null}")]
    public async Task NonemptySuccessStillRequiresAnUnambiguousSuccessModel(string body)
    {
        using var handler = new Handler(_ => new(HttpStatusCode.OK) { Content = new StringContent(body) });
        using var client = Client(handler);
        await Assert.ThrowsAsync<InvalidDataException>(() => new WorkstationRestClient(client).UpdateIdentityAsync("x", new("ethernet0.address", Mac, "00:50:56:00:00:02", ""), Token));
        Assert.Equal(1, handler.Count);
    }

    [Fact]
    public async Task IndependentReadsRequireBothCorrectEchoedNamesAndValidValues()
    {
        var paths = new List<string>();
        using var handler = new Handler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            paths.Add(path);
            var name = path.Split('/')[^1];
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { name, value = name == "ethernet0.address" ? Mac : Uuid })) };
        });
        using var client = Client(handler);
        Assert.Equal(new(Mac, Uuid), await new WorkstationRestClient(client).ReadIdentityAsync("x", Token));
        Assert.Equal(new[] { "/api/vms/x/params/ethernet0.address", "/api/vms/x/params/uuid.bios" }, paths);
    }

    [Theory]
    [InlineData("{\"name\":\"uuid.bios\",\"value\":\"00:50:56:00:00:01\"}")]
    [InlineData("{\"name\":\"ethernet0.address\",\"value\":null}")]
    [InlineData("{\"name\":\"ethernet0.address\",\"name\":\"uuid.bios\",\"value\":\"00:50:56:00:00:01\"}")]
    public async Task WrongEchoOrAmbiguousReadIsRejected(string body)
    {
        using var handler = new Handler(_ => new(HttpStatusCode.OK) { Content = new StringContent(body) });
        using var client = Client(handler);
        await Assert.ThrowsAsync<InvalidDataException>(() => new WorkstationRestClient(client).ReadIdentityAsync("x", Token));
        Assert.Equal(1, handler.Count);
    }

    private static HttpClient Client(HttpMessageHandler handler) => new(handler) { BaseAddress = new Uri("http://127.0.0.1:8697/") };
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    {
        public int Count { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        { cancellationToken.ThrowIfCancellationRequested(); Count++; return Task.FromResult(reply(request)); }
    }
}
