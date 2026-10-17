using System.Text;
using VMIdentityLab.Core.Reports;
using VMIdentityLab.Windows;

namespace VMIdentityLab.IntegrationTests;

public sealed class CollectorPipelineTests
{
    private sealed class FailingSource(Exception error) : IWindowsDataSource
    {
        public Task<IReadOnlyList<Finding>> CollectCategoryAsync(ObservationCategory category, DateTimeOffset time, CancellationToken cancellationToken) =>
            Task.FromException<IReadOnlyList<Finding>>(error);
    }
    private sealed class EmptySource : IWindowsDataSource
    {
        public Task<IReadOnlyList<Finding>> CollectCategoryAsync(ObservationCategory category, DateTimeOffset time, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Finding>>([]);
    }
    private sealed class WaitingSource : IWindowsDataSource
    {
        public async Task<IReadOnlyList<Finding>> CollectCategoryAsync(ObservationCategory category, DateTimeOffset time, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return [];
        }
    }

    [Fact]
    public async Task MissingProviderValuesAreNotSuccessfulEmptyResults()
    {
        var report = await new WindowsCollector(new EmptySource()).CollectAsync(TestContext.Current.CancellationToken);
        Assert.Equal(14, report.Findings.Count);
        Assert.All(report.Findings, item => Assert.Equal(CollectionStatus.NotSupported, item.Status));
        Assert.Equal(5, report.Findings.Select(item => item.Category).Distinct().Count());
    }

    [Theory]
    [InlineData(CollectionStatus.AccessDenied)]
    [InlineData(CollectionStatus.NotSupported)]
    [InlineData(CollectionStatus.Failed)]
    public async Task ProviderErrorsAreExplicitAndDoNotExposeRawMessages(CollectionStatus expected)
    {
        var error = expected switch
        {
            CollectionStatus.AccessDenied => (Exception)new UnauthorizedAccessException(@"C:\Users\SyntheticPrivate\secret"),
            CollectionStatus.NotSupported => new NotSupportedException("synthetic-secret"),
            _ => new InvalidOperationException("synthetic-secret")
        };
        var report = await new WindowsCollector(new FailingSource(error)).CollectAsync(TestContext.Current.CancellationToken);
        Assert.All(report.Findings, item => Assert.Equal(expected, item.Status));
        var json = Encoding.UTF8.GetString(ReportJson.Serialize(report));
        Assert.DoesNotContain("SyntheticPrivate", json);
        Assert.DoesNotContain("synthetic-secret", json);
    }

    [Fact]
    public async Task SlowProviderTimesOutWithAnExplicitStatus()
    {
        var report = await new WindowsCollector(new WaitingSource(), TimeSpan.FromMilliseconds(30)).CollectAsync(TestContext.Current.CancellationToken);
        Assert.All(report.Findings, item => Assert.Equal(CollectionStatus.Timeout, item.Status));
    }

    [Fact]
    public async Task CallerCancellationStopsThePipeline()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new WindowsCollector(new WaitingSource()).CollectAsync(cancellation.Token));
    }
}
