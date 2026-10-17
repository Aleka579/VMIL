using System.Security.Cryptography;

namespace VMIdentityLab.VMware.Configuration;

public sealed class VmxPreparedChange
{
    public Guid Id { get; } = Guid.NewGuid();
    public string TargetPath { get; }
    public Version WorkstationVersion { get; }
    public VmxConfigPlan Plan { get; }
    internal VmxPreparedChange(string path, Version version, VmxConfigPlan plan)
        => (TargetPath, WorkstationVersion, Plan) = (path, version, plan);
}

public sealed record VmxBackupReceipt(string TargetPath, string BackupPath, string OriginalHash, string AppliedHash, Version WorkstationVersion)
{
    public string Backend { get; init; } = "vmx-file";
    public VmxApiSettings? BeforeSettings { get; init; }
    public VmxApiSettings? AfterSettings { get; init; }
}

public interface IVmxConfigTransaction
{
    Task<VmxBackupReceipt> ApplyAsync(VmxPreparedChange preview, Guid confirmedPreviewId, CancellationToken token = default);
}

public sealed class VmxTransactionException(string backupPath, string temporaryPath, Exception inner, string? replacedBackupPath = null)
    : IOException("Configuration operation stopped. Retained files require inspection before recovery.", inner)
{
    public string BackupPath { get; } = backupPath;
    public string TemporaryPath { get; } = temporaryPath;
    public string? ReplacedBackupPath { get; } = replacedBackupPath;
}

public interface IVmxFileOperations
{
    Task WriteNewAsync(string path, byte[] bytes, CancellationToken token);
    void Replace(string temporaryPath, string targetPath, string replacedBackupPath);
}

public sealed class LocalVmxFileOperations : IVmxFileOperations
{
    public async Task WriteNewAsync(string path, byte[] bytes, CancellationToken token)
    {
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, true);
        await file.WriteAsync(bytes, token);
        await file.FlushAsync(token);
        file.Flush(true);
    }
    public void Replace(string temporaryPath, string targetPath, string replacedBackupPath)
        => File.Replace(temporaryPath, targetPath, replacedBackupPath);
}

public sealed class VmxConfigManager(IVmPowerStateProbe? powerProbe = null, IVmxWritePolicy? writePolicy = null,
    IVmxFileOperations? files = null) : IVmxConfigTransaction
{
    private readonly IVmPowerStateProbe powerProbe = powerProbe ?? new UnavailablePowerStateProbe();
    private readonly IVmxWritePolicy writePolicy = writePolicy ?? new ReleasedVmxWritePolicy();
    private readonly IVmxFileOperations files = files ?? new LocalVmxFileOperations();

    public bool IsWriteApproved(VmxPreparedChange preview)
    {
        try { ValidateApproval(preview); return true; }
        catch (InvalidOperationException) { return false; }
    }

    public async Task<VmxPreparedChange> PrepareAsync(string targetPath, Version workstationVersion,
        IReadOnlyDictionary<string, string> requested, CancellationToken token = default)
    {
        var path = ValidatePath(targetPath, ".vmx", requireWritable: false);
        var bytes = await ReadAsync(path, token);
        return new(path, workstationVersion, VmxConfigPlanner.Create(VmxDocument.Parse(bytes), requested));
    }

    public async Task<VmxBackupReceipt> ApplyAsync(VmxPreparedChange preview, Guid confirmedPreviewId,
        CancellationToken token = default)
    {
        if (confirmedPreviewId != preview.Id) throw new InvalidOperationException("The current preview must be explicitly confirmed.");
        ValidateApproval(preview);
        var path = ValidatePath(preview.TargetPath, ".vmx");
        await RequirePoweredOffAsync(path, token);
        // Deny concurrent writes to the original while allowing the atomic replacement.
        // Delete-sharing does not exclude external renames; recheck the pathname as well.
        await using var guard = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        var original = await ReadAsync(path, token);
        if (Hash(original) != preview.Plan.OriginalHash) throw new InvalidOperationException("The preview is stale.");
        var suffix = ".vmidentitylab-" + Guid.NewGuid().ToString("N");
        var backup = path + suffix + ".bak";
        var temporary = path + suffix + ".tmp";
        var replacedBackup = path + suffix + ".replaced.bak";
        try
        {
            await files.WriteNewAsync(backup, original, token);
            if (Hash(await ReadAsync(backup, token)) != preview.Plan.OriginalHash) throw new IOException("Backup verification failed.");
            await files.WriteNewAsync(temporary, preview.Plan.GetProposedBytes(), token);
            var staged = await ReadAsync(temporary, token);
            if (Hash(staged) != preview.Plan.ProposedHash || VmxDocument.Parse(staged).HasAmbiguity)
                throw new IOException("Temporary configuration verification failed.");
            await RequirePoweredOffAsync(path, token);
            _ = ValidatePath(path, ".vmx");
            if (Hash(await ReadAsync(path, token)) != preview.Plan.OriginalHash) throw new InvalidOperationException("The target changed after confirmation.");
            token.ThrowIfCancellationRequested();
            files.Replace(temporary, path, replacedBackup);
            // Once replacement starts, finish verification even if cancellation arrives.
            if (Hash(await ReadAsync(replacedBackup, CancellationToken.None)) != preview.Plan.OriginalHash)
                throw new IOException("An external replacement conflicted with this operation; its bytes were retained separately.");
            if (Hash(await ReadAsync(path, CancellationToken.None)) != preview.Plan.ProposedHash)
                throw new IOException("The replaced configuration could not be verified.");
            return new(path, backup, preview.Plan.OriginalHash, preview.Plan.ProposedHash, preview.WorkstationVersion);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or OperationCanceledException)
        {
            // No blind rollback or retries: another writer or a security block may be involved.
            // Keep original-byte backup and staging evidence; explicit recovery checks again.
            throw new VmxTransactionException(backup, temporary, error, replacedBackup);
        }
    }

    public async Task<VmxPreparedChange> PrepareRestoreAsync(string selectedTarget, VmxBackupReceipt receipt,
        CancellationToken token = default)
    {
        if (receipt.Backend != "vmx-file") throw new InvalidOperationException("Use the recorded API backend for this recovery.");
        var path = ValidatePath(selectedTarget, ".vmx");
        if (!path.Equals(Path.GetFullPath(receipt.TargetPath), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Backup target does not match the selected configuration.");
        var backup = ValidatePath(receipt.BackupPath, ".bak");
        if (!Path.GetDirectoryName(backup)!.Equals(Path.GetDirectoryName(path), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Backup is outside the configuration directory.");
        var current = await ReadAsync(path, token);
        var original = await ReadAsync(backup, token);
        if (Hash(current) != receipt.AppliedHash || Hash(original) != receipt.OriginalHash)
            throw new InvalidOperationException("Backup or current configuration changed; recovery requires a new review.");
        var currentDocument = VmxDocument.Parse(current);
        var originalDocument = VmxDocument.Parse(original);
        var requested = originalDocument.Entries.Where(item => item.Key.Equals("memsize", StringComparison.OrdinalIgnoreCase)
            || item.Key.Equals("numvcpus", StringComparison.OrdinalIgnoreCase)).ToDictionary(item => item.Key, item => item.Value,
                StringComparer.OrdinalIgnoreCase);
        var plan = VmxConfigPlanner.Create(currentDocument, requested);
        if (plan.ProposedHash != receipt.OriginalHash)
            throw new InvalidOperationException("Restore would change unreviewed configuration content.");
        return new(path, receipt.WorkstationVersion, plan);
    }

    private void ValidateApproval(VmxPreparedChange preview)
    {
        if (preview.Plan.RequiresTopologyReview || preview.Plan.Changes.Any(change => !writePolicy.IsApproved(preview.WorkstationVersion, change)))
            throw new InvalidOperationException("No verified write policy covers this configuration and version.");
        var document = VmxDocument.Parse(preview.Plan.GetProposedBytes());
        var hardware = document.Entries.SingleOrDefault(item => item.Key.Equals("virtualHW.version", StringComparison.OrdinalIgnoreCase));
        if (hardware is null || VmxSupportCatalog.Analyze(document, hardware, preview.WorkstationVersion).Status != VmxSupportStatus.Confirmed)
            throw new InvalidOperationException("The hardware/version combination has not been reviewed.");
    }

    private async Task RequirePoweredOffAsync(string path, CancellationToken token)
    {
        var state = await powerProbe.ProbeAsync(path, token);
        var age = DateTimeOffset.UtcNow - state.ObservedAt;
        if (state.State != VmPowerState.PoweredOff || !state.IsReliable || age < TimeSpan.Zero || age > TimeSpan.FromSeconds(15))
            throw new InvalidOperationException("Fresh reliable powered-off evidence is required.");
    }

    private static string ValidatePath(string path, string extension, bool requireWritable = true)
    {
        var fullPath = Path.GetFullPath(path);
        if (!Path.GetExtension(fullPath).Equals(extension, StringComparison.OrdinalIgnoreCase)
            || fullPath.StartsWith(@"\\", StringComparison.Ordinal)) throw new ArgumentException("Select a local configuration or backup file.");
        var item = new FileInfo(fullPath);
        if (!item.Exists || (item.Attributes & FileAttributes.ReparsePoint) != 0
            || requireWritable && (item.Attributes & FileAttributes.ReadOnly) != 0)
            throw new IOException("The selected file is unavailable or protected.");
        for (var parent = item.Directory; parent is not null; parent = parent.Parent)
            if ((parent.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Redirected configuration paths are not supported.");
        return fullPath;
    }

    private static async Task<byte[]> ReadAsync(string path, CancellationToken token)
    {
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 8192, true);
        if (file.Length > VmxReader.MaxVmxBytes) throw new InvalidDataException("Configuration exceeds the supported size.");
        var bytes = new byte[(int)file.Length];
        await file.ReadExactlyAsync(bytes, token);
        return bytes;
    }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}
