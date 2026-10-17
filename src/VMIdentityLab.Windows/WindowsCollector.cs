using VMIdentityLab.Core.Evidence;
using VMIdentityLab.Core.Reports;

namespace VMIdentityLab.Windows;

public enum ObservationCategory { Smbios, Cpu, Pnp, Tools, System }
public sealed record CollectionProgress(ObservationCategory Category, int Completed, int Total);

public interface IWindowsDataSource
{
    Task<IReadOnlyList<Finding>> CollectCategoryAsync(ObservationCategory category, DateTimeOffset observedAt, CancellationToken cancellationToken);
}

public sealed class WindowsCollector(IWindowsDataSource? source = null, TimeSpan? categoryTimeout = null)
{
    private readonly IWindowsDataSource source = source ?? new NativeWindowsDataSource();
    private readonly TimeSpan timeout = categoryTimeout ?? TimeSpan.FromSeconds(5);
    private readonly SemaphoreSlim collectionGate = new(1, 1);

    public async Task<DiagnosticReport> CollectAsync(CancellationToken cancellationToken = default, IProgress<CollectionProgress>? progress = null)
    {
        await collectionGate.WaitAsync(cancellationToken);
        try
        {
            var findings = new List<Finding>();
            var started = DateTimeOffset.UtcNow;
            var categories = Enum.GetValues<ObservationCategory>();
            foreach (var category in categories)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var observedAt = DateTimeOffset.UtcNow;
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                try
                {
                    var collected = await source.CollectCategoryAsync(category, observedAt, linked.Token).WaitAsync(timeout, cancellationToken);
                    foreach (var definition in Definitions(category))
                    {
                        var item = collected.FirstOrDefault(item => item.Id == definition.Id);
                        findings.Add(item is null || !item.Value.IsWellFormed() || (item.Status == CollectionStatus.Success && item.Value.Kind == ValueKind.Unknown)
                            ? Unavailable(definition, observedAt, CollectionStatus.NotSupported)
                            : item with { Category = definition.Category, Source = definition.Source, ObservedAt = observedAt });
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (Exception error)
                {
                    linked.Cancel();
                    var status = error switch
                    {
                        TimeoutException => CollectionStatus.Timeout,
                        UnauthorizedAccessException => CollectionStatus.AccessDenied,
                        NotSupportedException => CollectionStatus.NotSupported,
                        _ => CollectionStatus.Failed
                    };
                    findings.AddRange(Definitions(category).Select(definition => Unavailable(definition, observedAt, status)));
                }
                progress?.Report(new(category, Array.IndexOf(categories, category) + 1, categories.Length));
            }
            string? Text(string id) => findings.FirstOrDefault(item => item.Id == id && item.Status == CollectionStatus.Success)?.Value.Text;
            return ReportPrivacy.Apply(new()
            {
                CollectedAt = started,
                System = new() { Os = Text("system.os"), Build = Text("system.build"), Architecture = Text("system.architecture") },
                Findings = findings,
                VirtualizationEvidence = EvidenceEvaluator.Evaluate(findings),
                Diagnostics = findings.Any(item => item.Status != CollectionStatus.Success)
                    ? [new() { Code = "collection.partial", Message = "Some fields are unavailable; inspect their individual status." }] : []
            });
        }
        finally { collectionGate.Release(); }
    }

    internal static IEnumerable<FindingDefinition> Definitions(ObservationCategory category) => FindingCatalog.ById.Values
        .Where(item => item.Category == category.ToString().ToLowerInvariant() && item.Sensitivity == DataSensitivity.Public)
        .OrderBy(item => item.Id, StringComparer.Ordinal);

    internal static Finding Unavailable(FindingDefinition definition, DateTimeOffset time, CollectionStatus status) => new()
    {
        Id = definition.Id, Category = definition.Category, Source = definition.Source,
        ObservedAt = time, Status = status, Value = new(), Explanation = "The field is unavailable."
    };
}
