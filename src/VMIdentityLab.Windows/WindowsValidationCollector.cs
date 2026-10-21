using System.Security.Principal;
using VMIdentityLab.Core.Comparison;
using VMIdentityLab.Core.Reports;

namespace VMIdentityLab.Windows;

public interface IValidationIdentitySource
{
    Task<IReadOnlyList<Finding>> CollectValidationIdentitiesAsync(DateTimeOffset time, CancellationToken token, Guid? networkAdapterId = null);
}

// Called only by an explicitly selected private capture, never ordinary diagnostics.
public sealed class WindowsValidationCollector
{
    private readonly WindowsCollector collector;
    private readonly IValidationIdentitySource identities;
    private readonly Func<bool> elevated;
    private readonly TimeSpan timeout;

    public WindowsValidationCollector(IWindowsDataSource? source = null, IValidationIdentitySource? identities = null,
        Func<bool>? elevated = null, TimeSpan? timeout = null)
    {
        var native = source is null || identities is null ? new NativeWindowsDataSource() : null;
        collector = new(source ?? native!);
        this.identities = identities ?? native!;
        this.elevated = elevated ?? IsElevated;
        this.timeout = timeout ?? TimeSpan.FromSeconds(5);
    }

    public async Task<PrivateValidationSnapshot> CapturePrivateAsync(Guid session, ValidationPhase phase, CancellationToken token = default, Guid? networkAdapterId = null)
    {
        if (session == Guid.Empty || !Enum.IsDefined(phase) || networkAdapterId == Guid.Empty) throw new ArgumentException("An explicit session, phase and valid optional interface are required.");
        var report = await collector.CollectAsync(token); // The program's normal public-data behavior, separately from identity queries.
        var time = DateTimeOffset.UtcNow;
        IReadOnlyList<Finding> observations;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token);
        try { observations = await identities.CollectValidationIdentitiesAsync(time, linked.Token, networkAdapterId).WaitAsync(timeout, token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception error)
        {
            linked.Cancel();
            var status = error switch { TimeoutException => CollectionStatus.Timeout, UnauthorizedAccessException => CollectionStatus.AccessDenied,
                NotSupportedException => CollectionStatus.NotSupported, _ => CollectionStatus.Failed };
            observations = new[] { "smbios.uuid", "network.macAddress" }.Select(id => WindowsCollector.Unavailable(FindingCatalog.ById[id], time, status)).ToArray();
        }
        token.ThrowIfCancellationRequested();
        return new() { SessionId = session, Phase = phase, DataKind = ValidationDataKind.Captured, Elevated = elevated(), NetworkAdapterId = networkAdapterId, Report = report,
            Identities = observations.Where(item => item.Id is "smbios.uuid" or "network.macAddress").Select(item => item with
                { Sensitivity = DataSensitivity.Identifier, Explanation = "", Value = item.Status == CollectionStatus.Success ? item.Value : new() }).ToArray() };
    }

    private static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
