using System.Net;
using System.Text.Json;
using VMIdentityLab.VMware;
using VMIdentityLab.VMware.Configuration;

namespace VMIdentityLab.UnitTests;

public sealed class RestClientTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("http://example.com/")]
    [InlineData("http://127.0.0.1/api/")]
    [InlineData("http://127.0.0.1/?query=1")]
    [InlineData("http://user@127.0.0.1/")]
    [InlineData("http://127.0.0.1/#fragment")]
    public void RejectsNonLocalOrNonRootAddresses(string address)
    {
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        Assert.Throws<ArgumentException>(() => new WorkstationRestClient(client));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"id\":\"wrong\",\"memory\":4096,\"cpu\":{\"processors\":2}}")]
    [InlineData("{\"id\":\"x\",\"memory\":4096,\"memory\":6144,\"cpu\":{\"processors\":2}}")]
    [InlineData("{\"id\":\"x\",\"memory\":4096,\"cpu\":{\"processors\":2,\"processors\":4}}")]
    [InlineData("{\"id\":\"x\",\"memory\":\"4096\",\"cpu\":{\"processors\":2}}")]
    [InlineData("{\"id\":\"x\",\"memory\":0,\"cpu\":{\"processors\":2}}")]
    [InlineData("{\"id\":\"x\",\"memory\":4096,\"cpu\":{\"processors\":-1}}")]
    public async Task RejectsIncompleteAmbiguousOrWrongTargetSettings(string response)
    {
        using var handler = new Handler(_ => new(HttpStatusCode.OK) { Content = new StringContent(response) });
        using var client = Client(handler);
        await Assert.ThrowsAsync<InvalidDataException>(() => new WorkstationRestClient(client).ReadSettingsAsync("x", Token));
        Assert.Equal(1, handler.Count);
    }

    [Fact]
    public async Task PutSendsOnlyTheReviewedFieldAndUsesTheVendorContentType()
    {
        string? sent = null;
        using var handler = new Handler(request =>
        {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("application/vnd.vmware.vmw.rest-v1+json", request.Content!.Headers.ContentType!.ToString());
            sent = request.Content.ReadAsStringAsync(Token).GetAwaiter().GetResult();
            return new(HttpStatusCode.OK) { Content = new StringContent("{\"id\":\"x\",\"memory\":4096,\"cpu\":{\"processors\":4}}") };
        });
        using var client = Client(handler);
        var result = await new WorkstationRestClient(client).UpdateAsync("x", [new("memsize", "6144", "4096", "MiB")], Token);
        var values = JsonSerializer.Deserialize<Dictionary<string, int>>(sent!)!;
        Assert.Equal(4096, Assert.Single(values).Value);
        Assert.Contains("memory", values.Keys);
        Assert.Equal(4, result.Processors);
        Assert.Equal(1, handler.Count);
    }

    [Fact]
    public async Task UnsupportedKeysAndValuesAreRejectedBeforeAnyRequest()
    {
        using var handler = new Handler(_ => throw new InvalidOperationException("No request expected"));
        using var client = Client(handler);
        var api = new WorkstationRestClient(client);
        await Assert.ThrowsAsync<ArgumentException>(() => api.UpdateAsync("x", [new("unknown", "1", "2", "count")], Token));
        await Assert.ThrowsAsync<ArgumentException>(() => api.UpdateAsync("x", [new("memsize", "6144", "-1", "MiB")], Token));
        Assert.Equal(0, handler.Count);
    }

    [Theory]
    [InlineData("ethernet0.address")]
    [InlineData("ethernet0.generatedAddress")]
    [InlineData("uuid.bios")]
    [InlineData("uuid.location")]
    public async Task IdentityFieldsCannotBeSentAloneOrAlongsideNumericSettings(string key)
    {
        using var handler = new Handler(_ => throw new InvalidOperationException("No request expected"));
        using var client = Client(handler);
        var api = new WorkstationRestClient(client);
        var identity = new VmxConfigChange(key, "1", "2", "count");
        var numeric = new VmxConfigChange("memsize", "6144", "4096", "MiB");
        await Assert.ThrowsAsync<ArgumentException>(() => api.UpdateAsync("x", [identity], Token));
        await Assert.ThrowsAsync<ArgumentException>(() => api.UpdateAsync("x", [numeric, identity], Token));
        await Assert.ThrowsAsync<ArgumentException>(() => api.UpdateAsync("x", [identity, numeric], Token));
        Assert.Equal(0, handler.Count);
    }

    [Fact]
    public async Task ErrorResponsesNeverEchoVendorBodyOrRetry()
    {
        using var handler = new Handler(_ => new(HttpStatusCode.Conflict)
            { Content = new StringContent("synthetic private credential and local path") });
        using var client = Client(handler);
        var error = await Assert.ThrowsAsync<WorkstationApiException>(() => new WorkstationRestClient(client)
            .UpdateAsync("x", [new("memsize", "6144", "4096", "MiB")], Token));
        Assert.Equal(HttpStatusCode.Conflict, error.StatusCode);
        Assert.DoesNotContain("credential", error.ToString());
        Assert.Equal(1, handler.Count);
    }

    [Fact]
    public async Task VmIdentifiersCannotInjectPathsOrQueryParameters()
    {
        const string id = "a/b?x=y";
        using var handler = new Handler(request =>
        {
            Assert.Equal("/api/vms/a%2Fb%3Fx%3Dy", request.RequestUri!.AbsolutePath);
            Assert.Equal("", request.RequestUri.Query);
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { id, memory = 4096, cpu = new { processors = 2 } })) };
        });
        using var client = Client(handler);
        Assert.Equal(id, (await new WorkstationRestClient(client).ReadSettingsAsync(id, Token)).Id);
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    public async Task DotSegmentsCannotRedirectARequestOutsideTheVmEndpoint(string id)
    {
        using var handler = new Handler(_ => throw new InvalidOperationException("No request expected"));
        using var client = Client(handler);
        await Assert.ThrowsAsync<InvalidDataException>(() => new WorkstationRestClient(client).ReadSettingsAsync(id, Token));
        Assert.Equal(0, handler.Count);
    }

    [Fact]
    public async Task OversizedResponsesAreRejected()
    {
        using var handler = new Handler(_ => new(HttpStatusCode.OK)
            { Content = new StringContent(new string('x', VmxReader.MaxVmxBytes + 1)) });
        using var client = Client(handler);
        await Assert.ThrowsAsync<InvalidDataException>(() => new WorkstationRestClient(client).ReadSettingsAsync("x", Token));
    }

    [Fact]
    public async Task TimeoutAlsoBoundsReadingTheBodyAfterHeadersArrive()
    {
        using var handler = new Handler(_ => new(HttpStatusCode.OK) { Content = new HangingContent() });
        using var client = Client(handler);
        client.Timeout = TimeSpan.FromMilliseconds(80);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new WorkstationRestClient(client).ReadSettingsAsync("x", Token));
        Assert.Equal(1, handler.Count);
    }

    private static HttpClient Client(HttpMessageHandler handler) => new(handler) { BaseAddress = new Uri("http://127.0.0.1:8697/") };
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    {
        public int Count { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        { cancellationToken.ThrowIfCancellationRequested(); Count++; return Task.FromResult(reply(request)); }
    }
    private sealed class HangingContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => throw new NotSupportedException();
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult<Stream>(new HangingStream());
    }
    private sealed class HangingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        { await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken); return 0; }
    }
}
