using System.Globalization;
using System.Net;
using System.Text.Json;

namespace VMIdentityLab.VMware.Configuration;

public sealed class VmxApiConfigManager(WorkstationRestClient api, Func<Version?> installedVersion,
    IVmxWritePolicy? writePolicy = null, IVmxFileOperations? files = null) : IVmxConfigTransaction
{
    public const string BackendName = "workstation-rest-v1";
    private readonly IVmxWritePolicy policy = writePolicy ?? new ReleasedVmxApiWritePolicy();
    private readonly IVmxFileOperations files = files ?? new LocalVmxFileOperations();

    public static bool IsProductionWriteApproved(VmxPreparedChange preview)
        => IsApproved(preview, new ReleasedVmxApiWritePolicy());

    public static string PendingOperationPath(string target) => Path.GetFullPath(target) + ".vmidentitylab-api.pending.json";
    public static bool HasPendingOperation(string target)
        => File.Exists(PendingOperationPath(target)) || Directory.Exists(PendingOperationPath(target));

    private static bool IsApproved(VmxPreparedChange preview, IVmxWritePolicy policy)
    {
        try
        {
            return ReleasedVmxApiWritePolicy.IsTestedCandidate(preview)
                && preview.Plan.Changes.All(change => policy.IsApproved(preview.WorkstationVersion, change));
        }
        catch (Exception error) when (error is ArgumentException or InvalidDataException) { return false; }
    }

    public async Task<VmxBackupReceipt> ApplyAsync(VmxPreparedChange preview, Guid confirmedPreviewId,
        CancellationToken token = default)
    {
        if (confirmedPreviewId != preview.Id || !IsApproved(preview, policy))
            throw new InvalidOperationException("A current confirmation and verified API policy are required.");
        RequireVersion(preview);
        var path = VmxApiFiles.Validate(preview.TargetPath);
        token.ThrowIfCancellationRequested();
        using var operation = new VmxApiOperation(path);
        var requestStarted = false;
        var intentOwned = false;
        var journalPath = operation.PendingPath;
        string? targetId = null;
        try
        {
            var id = await api.ResolveVmAsync(path, token);
            targetId = id;
            await RequireOffAsync(id, token);
            var original = await VmxApiFiles.ReadExclusiveAsync(path, token);
            if (VmxApiFiles.Hash(original) != preview.Plan.OriginalHash) throw new InvalidOperationException("The API preview is stale.");
            var before = VmxApiSettings.From(VmxDocument.Parse(original));
            var after = VmxApiSettings.From(VmxDocument.Parse(preview.Plan.GetProposedBytes()));
            if (!before.Matches(await api.ReadSettingsAsync(id, token)))
                throw new InvalidOperationException("API settings and VMX disagree.");
            await files.WriteNewAsync(operation.BackupPath, original, token);
            if (VmxApiFiles.Hash(await VmxApiFiles.ReadExclusiveAsync(operation.BackupPath, token)) != preview.Plan.OriginalHash)
                throw new IOException("Backup verification failed.");
            await files.WriteNewAsync(operation.ProposedPath, preview.Plan.GetProposedBytes(), token);
            if (VmxApiFiles.Hash(await VmxApiFiles.ReadExclusiveAsync(operation.ProposedPath, token)) != preview.Plan.ProposedHash)
                throw new IOException("Proposed configuration verification failed.");

            // Rebind target and recheck actual state/settings/content after backup and staging.
            if (await api.ResolveVmAsync(path, token) != id) throw new InvalidOperationException("VM registration changed.");
            await RequireOffAsync(id, token);
            if (!before.Matches(await api.ReadSettingsAsync(id, token))) throw new InvalidOperationException("VM settings changed.");
            RequireVersion(preview);
            _ = VmxApiFiles.Validate(path);
            if (VmxApiFiles.Hash(await VmxApiFiles.ReadExclusiveAsync(path, token)) != preview.Plan.OriginalHash)
                throw new InvalidOperationException("The target changed after confirmation.");
            token.ThrowIfCancellationRequested();
            // Finish this small durable write even if cancellation arrives; check again before PUT.
            await operation.WriteIntentAsync(preview, id, files, CancellationToken.None);
            intentOwned = true;
            token.ThrowIfCancellationRequested();
            requestStarted = true;
            // Cancellation cannot mean "unchanged" after dispatch. Client timeout remains bounded.
            var updated = await api.UpdateAsync(id, preview.Plan.Changes, CancellationToken.None);
            if (!after.Matches(updated)) throw new InvalidDataException("Update response does not match the reviewed settings.");
            if (await api.ResolveVmAsync(path, CancellationToken.None) != id) throw new InvalidOperationException("VM registration changed during update.");
            await RequireOffAsync(id, CancellationToken.None);
            if (!after.Matches(await api.ReadSettingsAsync(id, CancellationToken.None)))
                throw new InvalidDataException("Updated API settings could not be verified.");
            RequireVersion(preview);
            _ = VmxApiFiles.Validate(path);
            if (VmxApiFiles.Hash(await VmxApiFiles.ReadExclusiveAsync(path, CancellationToken.None)) != preview.Plan.ProposedHash)
                throw new InvalidDataException("Updated VMX contains an unexpected or concurrent change.");
            journalPath = operation.Finish("completed");
            return new(path, operation.BackupPath, preview.Plan.OriginalHash, preview.Plan.ProposedHash, preview.WorkstationVersion)
            { Backend = BackendName, BeforeSettings = before, AfterSettings = after };
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException
            or InvalidOperationException or ArgumentException or HttpRequestException or JsonException or OperationCanceledException)
        {
            // A completed 4xx response is not a transport timeout. Prove no local/API setting change
            // before permitting a fresh, separately confirmed attempt; never automatically retry.
            var unknown = requestStarted;
            if (intentOwned && error is OperationCanceledException && !requestStarted)
            {
                try { journalPath = operation.Finish("not-sent"); }
                catch (Exception moveError) when (moveError is IOException or UnauthorizedAccessException)
                { /* Retain the intent and all recovery locations. Do not retry. */ }
            }
            else if (intentOwned && error is WorkstationApiException rejected
                && rejected.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                    or HttpStatusCode.NotFound or HttpStatusCode.NotAcceptable or HttpStatusCode.Conflict)
            {
                try
                {
                    var unchanged = await VmxApiFiles.ReadExclusiveAsync(path, CancellationToken.None);
                    if (VmxApiFiles.Hash(unchanged) == preview.Plan.OriginalHash)
                    {
                        var id = await api.ResolveVmAsync(path, CancellationToken.None);
                        var before = VmxApiSettings.From(VmxDocument.Parse(unchanged));
                        if (id == targetId && before.Matches(await api.ReadSettingsAsync(id, CancellationToken.None))
                            && VmxApiFiles.Hash(await VmxApiFiles.ReadExclusiveAsync(path, CancellationToken.None)) == preview.Plan.OriginalHash)
                        { journalPath = operation.Finish("rejected"); unknown = false; }
                    }
                }
                catch (Exception checkError) when (checkError is IOException or InvalidDataException or UnauthorizedAccessException
                    or InvalidOperationException or ArgumentException or HttpRequestException or JsonException or OperationCanceledException)
                { /* Keep the unresolved intent; a failed read is not evidence of no change. */ }
            }
            throw new VmxApiTransactionException(operation.BackupPath, operation.ProposedPath, journalPath, unknown, error);
        }
    }

    public static async Task<VmxPreparedChange> PrepareRestoreAsync(string selectedTarget, VmxBackupReceipt receipt,
        CancellationToken token = default)
    {
        if (receipt.Backend != BackendName || receipt.BeforeSettings is null || receipt.AfterSettings is null
            || !receipt.BeforeSettings.IsValid || !receipt.AfterSettings.IsValid
            || receipt.BeforeSettings.CoresPerSocket != receipt.AfterSettings.CoresPerSocket
            || receipt.BeforeSettings.HardwareVersion != receipt.AfterSettings.HardwareVersion)
            throw new InvalidDataException("An API recovery receipt is required.");
        var path = VmxApiFiles.Validate(selectedTarget);
        if (!path.Equals(Path.GetFullPath(receipt.TargetPath), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Recovery target does not match the receipt.");
        var backup = VmxApiFiles.Validate(receipt.BackupPath, ".bak", writable: false);
        if (!string.Equals(Path.GetDirectoryName(path), Path.GetDirectoryName(backup), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Backup is outside the selected configuration directory.");
        var original = await VmxApiFiles.ReadExclusiveAsync(backup, token);
        if (VmxApiFiles.Hash(original) != receipt.OriginalHash || VmxApiSettings.From(VmxDocument.Parse(original)) != receipt.BeforeSettings)
            throw new InvalidOperationException("Recovery backup changed.");
        var current = VmxDocument.Parse(await VmxApiFiles.ReadExclusiveAsync(path, token));
        if (VmxApiSettings.From(current) != receipt.AfterSettings)
            throw new InvalidOperationException("Candidate settings or topology changed after this operation.");
        var requested = new Dictionary<string, string>();
        if (receipt.BeforeSettings.MemoryMiB != receipt.AfterSettings.MemoryMiB)
            requested.Add("memsize", receipt.BeforeSettings.MemoryMiB.ToString(CultureInfo.InvariantCulture));
        if (receipt.BeforeSettings.Processors != receipt.AfterSettings.Processors)
            requested.Add("numvcpus", receipt.BeforeSettings.Processors.ToString(CultureInfo.InvariantCulture));
        // A boot may update unrelated VMware fields. Preview against the latest complete contents;
        // restore only the settings in this receipt, never an old full VMX over later edits.
        return new(path, receipt.WorkstationVersion, VmxConfigPlanner.Create(current, requested));
    }

    private void RequireVersion(VmxPreparedChange preview)
    {
        if (installedVersion() != preview.WorkstationVersion) throw new InvalidOperationException("Installed Workstation version changed or is unknown.");
    }
    private async Task RequireOffAsync(string id, CancellationToken token)
    {
        var state = await api.ReadPowerAsync(id, token);
        var age = DateTimeOffset.UtcNow - state.ObservedAt;
        if (!state.IsReliable || state.State != VmPowerState.PoweredOff || age < TimeSpan.Zero || age > TimeSpan.FromSeconds(15))
            throw new InvalidOperationException("Fresh reliable powered-off evidence is required.");
    }
}
