using System.Net;
using System.Text.Json;
using VMIdentityLab.Core.Reports;
using VMIdentityLab.VMware;

namespace VMIdentityLab.UnitTests;

public sealed class RestPowerStateTests
{
    private static readonly string Target = Path.GetFullPath("synthetic-state.vmx");
    private static string Inventory => JsonSerializer.Serialize(new[] { new { id = "synthetic-id", path = Target } });

    [Theory]
    [InlineData("poweredOn", VmPowerState.PoweredOn, true)]
    [InlineData("poweredOff", VmPowerState.PoweredOff, true)]
    [InlineData("suspended", VmPowerState.Suspended, true)]
    [InlineData("paused", VmPowerState.Unknown, false)]
    [InlineData("future-state", VmPowerState.Unknown, false)]
    public async Task RecognizesSnapshotStatesOnlyForTheMatchedTarget(string name, VmPowerState expected, bool reliable)
    {
        using var handler = new ReplyHandler(Inventory, JsonSerializer.Serialize(new { power_state = name }));
        using var client = Client(handler);
        var result = await new WorkstationRestPowerStateProbe(client).ProbeAsync(Target, TestContext.Current.CancellationToken);
        Assert.Equal(expected, result.State);
        Assert.Equal(reliable, result.IsReliable);
        Assert.Equal(new[] { "GET /api/vms", "GET /api/vms/synthetic-id/power" }, handler.Requests);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("[{\"id\":\"x\"}]")]
    [InlineData("not-json")]
    public async Task MissingOrInvalidInventoryNeverImpliesPoweredOff(string inventory)
    {
        using var handler = new ReplyHandler(inventory, "{\"power_state\":\"poweredOff\"}");
        using var client = Client(handler);
        var result = await new WorkstationRestPowerStateProbe(client).ProbeAsync(Target, TestContext.Current.CancellationToken);
        Assert.Equal(VmPowerState.Unknown, result.State);
        Assert.False(result.IsReliable);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task AmbiguousRegistrationsPreventThePowerRequest()
    {
        foreach (var inventory in new[]
        {
            JsonSerializer.Serialize(new[] { new { id = "a", path = Target }, new { id = "b", path = Target } }),
            JsonSerializer.Serialize(new[] { new { id = "a", path = Target }, new { id = "a", path = Target + ".other" } })
        })
        {
            using var handler = new ReplyHandler(inventory, "{\"power_state\":\"poweredOff\"}");
            using var client = Client(handler);
            var result = await new WorkstationRestPowerStateProbe(client).ProbeAsync(Target, TestContext.Current.CancellationToken);
            Assert.Equal(VmPowerState.Unknown, result.State);
            Assert.Single(handler.Requests);
        }
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"power_state\":null}")]
    [InlineData("{\"power_state\":0}")]
    [InlineData("{\"power_state\":\"suspended\",\"power_state\":\"poweredOff\"}")]
    [InlineData("{\"power_state\":\"POWEREDOFF\"}")]
    public async Task InvalidPowerResponsesRemainUnknown(string power)
    {
        using var handler = new ReplyHandler(Inventory, power);
        using var client = Client(handler);
        var result = await new WorkstationRestPowerStateProbe(client).ProbeAsync(Target, TestContext.Current.CancellationToken);
        Assert.Equal(VmPowerState.Unknown, result.State);
        Assert.False(result.IsReliable);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task FailedPowerRequestsDoNotProduceAState(HttpStatusCode code)
    {
        using var handler = new ReplyHandler(Inventory, "{\"power_state\":\"poweredOff\"}", code);
        using var client = Client(handler);
        var result = await new WorkstationRestPowerStateProbe(client).ProbeAsync(Target, TestContext.Current.CancellationToken);
        Assert.Equal(VmPowerState.Unknown, result.State);
        Assert.DoesNotContain(Target, result.Explanation);
    }

    [Fact]
    public async Task CallerCancellationIsNotReportedAsAnUnknownSuccess()
    {
        using var handler = new ReplyHandler(Inventory, "{}");
        using var client = Client(handler);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new WorkstationRestPowerStateProbe(client).ProbeAsync(Target, cancelled.Token));
    }

    [Fact]
    public void VerifiedSourceSurvivesReportRedaction()
    {
        var report = ReportPrivacy.Apply(new() { CollectedAt = DateTimeOffset.UtcNow,
            VmxSummary = new() { PowerState = "poweredOn", StateSource = "vmrest.power" } });
        Assert.Equal("vmrest.power", report.VmxSummary!.StateSource);
    }

    private static HttpClient Client(ReplyHandler handler) => new(handler)
        { BaseAddress = new Uri("http://127.0.0.1:8697/") };

    private sealed class ReplyHandler(string inventory, string power, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request.Method + " " + request.RequestUri!.AbsolutePath);
            var first = Requests.Count == 1;
            return Task.FromResult(new HttpResponseMessage(first ? HttpStatusCode.OK : status)
                { Content = new StringContent(first ? inventory : power) });
        }
    }
}
