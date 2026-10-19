using System.Text;
using VMIdentityLab.Core.Reports;
using VMIdentityLab.Windows;

namespace VMIdentityLab.IntegrationTests;

public sealed class IdentityCollectionTests
{
    private sealed class IdentifierSource(Exception? failure = null, bool wait = false) : IWindowsDataSource
    {
        public List<ObservationCategory> Calls { get; } = [];
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<IReadOnlyList<Finding>> CollectCategoryAsync(ObservationCategory category, DateTimeOffset time, CancellationToken token)
        {
            Calls.Add(category);
            if (category != ObservationCategory.Identifiers) return [];
            Entered.TrySetResult();
            if (wait) await Task.Delay(Timeout.Infinite, token);
            if (failure is not null) throw failure;
            return FindingCatalog.ById.Values.Where(item => item.Sensitivity == DataSensitivity.Identifier)
                .Select(item => new Finding
                {
                    Id = item.Id, Category = item.Category, Source = item.Source, ObservedAt = time,
                    Status = CollectionStatus.Success, Value = FindingValue.FromText("SYNTHETIC-PRIVATE-VALUE")
                }).ToArray();
        }
    }

    [Fact]
    public async Task DefaultCollectionNeverRequestsIdentifiers()
    {
        var source = new IdentifierSource();
        var report = await new WindowsCollector(source).CollectAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(ObservationCategory.Identifiers, source.Calls);
        Assert.Equal(5, source.Calls.Count);
        Assert.Equal(14, report.Findings.Count);
        Assert.DoesNotContain(report.Findings, item => FindingCatalog.ById[item.Id].Sensitivity == DataSensitivity.Identifier);
    }

    [Fact]
    public async Task ExplicitChoiceReadsThreeIdentifiersAndReturnsOnlyRedactedValues()
    {
        var source = new IdentifierSource();
        var report = await new WindowsCollector(source).CollectAsync(TestContext.Current.CancellationToken, includeIdentifiers: true);
        Assert.Equal(6, source.Calls.Count);
        Assert.Equal(17, report.Findings.Count);
        var identifiers = report.Findings.Where(item => item.Sensitivity == DataSensitivity.Identifier).ToArray();
        Assert.Equal(3, identifiers.Length);
        Assert.All(identifiers, item =>
        {
            Assert.Equal(CollectionStatus.Success, item.Status);
            Assert.Equal(ValueKind.Redacted, item.Value.Kind);
            Assert.Equal(FindingCatalog.ById[item.Id].Source, item.Source);
        });
        Assert.DoesNotContain("SYNTHETIC-PRIVATE-VALUE", Encoding.UTF8.GetString(ReportJson.Serialize(report)));
        Assert.DoesNotContain("SYNTHETIC-PRIVATE-VALUE", HtmlReportRenderer.Render(report));
    }

    [Theory]
    [InlineData(CollectionStatus.AccessDenied)]
    [InlineData(CollectionStatus.NotSupported)]
    [InlineData(CollectionStatus.Failed)]
    public async Task IdentifierErrorsAreExplicitAndDoNotChangePublicCollection(CollectionStatus expected)
    {
        var error = expected switch
        {
            CollectionStatus.AccessDenied => (Exception)new UnauthorizedAccessException("SYNTHETIC-PRIVATE-VALUE"),
            CollectionStatus.NotSupported => new NotSupportedException("SYNTHETIC-PRIVATE-VALUE"),
            _ => new IOException("SYNTHETIC-PRIVATE-VALUE")
        };
        var report = await new WindowsCollector(new IdentifierSource(error)).CollectAsync(TestContext.Current.CancellationToken, includeIdentifiers: true);
        Assert.All(report.Findings.Where(item => item.Sensitivity == DataSensitivity.Identifier), item => Assert.Equal(expected, item.Status));
        Assert.All(report.Findings.Where(item => item.Sensitivity == DataSensitivity.Public), item => Assert.Equal(CollectionStatus.NotSupported, item.Status));
        Assert.DoesNotContain("SYNTHETIC-PRIVATE-VALUE", Encoding.UTF8.GetString(ReportJson.Serialize(report)));
    }

    [Fact]
    public async Task IdentifierTimeoutIsNotReportedAsSuccessfulEmptyData()
    {
        var report = await new WindowsCollector(new IdentifierSource(wait: true), TimeSpan.FromMilliseconds(30))
            .CollectAsync(TestContext.Current.CancellationToken, includeIdentifiers: true);
        Assert.All(report.Findings.Where(item => item.Sensitivity == DataSensitivity.Identifier), item => Assert.Equal(CollectionStatus.Timeout, item.Status));
    }

    [Fact]
    public async Task CancellationDuringExplicitIdentifierCollectionStopsTheOperation()
    {
        var source = new IdentifierSource(wait: true);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var operation = new WindowsCollector(source).CollectAsync(cancellation.Token, includeIdentifiers: true);
        await source.Entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
    }
}
