using System.Text.Json;
using System.Text.Json.Serialization;

namespace VMIdentityLab.VMware.Configuration;

public sealed record VmxBackupHistoryEntry(Guid Id, DateTimeOffset RecordedAt, string Operation, VmxBackupReceipt Receipt);
public sealed record VmxBackupHistoryResult(IReadOnlyList<VmxBackupHistoryEntry> Entries, int UnreadableFiles);

public interface IVmxBackupHistory
{
    Task SaveAsync(VmxBackupHistoryEntry entry, CancellationToken token = default);
    Task<VmxBackupHistoryResult> ReadAsync(CancellationToken token = default);
}

// Private recovery data. Never include this store in public diagnostics or release packages.
public sealed class LocalVmxBackupHistory(string directory) : IVmxBackupHistory
{
    private readonly string directory = Path.GetFullPath(directory);
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, MaxDepth = 8,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    private sealed record Stored(int SchemaVersion, Guid Id, DateTimeOffset RecordedAt, string Operation,
        string TargetPath, string BackupPath, string OriginalHash, string AppliedHash, string WorkstationVersion,
        string? Backend = null, VmxApiSettings? BeforeSettings = null, VmxApiSettings? AfterSettings = null);

    public async Task SaveAsync(VmxBackupHistoryEntry entry, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        Validate(entry);
        var destination = Path.Combine(directory, entry.Id.ToString("N") + ".json");
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        var receipt = entry.Receipt;
        var stored = new Stored(2, entry.Id, entry.RecordedAt, entry.Operation, receipt.TargetPath,
            receipt.BackupPath, receipt.OriginalHash, receipt.AppliedHash, receipt.WorkstationVersion.ToString(),
            receipt.Backend, receipt.BeforeSettings, receipt.AfterSettings);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(stored, Options);
        if (bytes.Length > 32768) throw new InvalidDataException("Recovery record exceeds the supported size.");
        Directory.CreateDirectory(directory);
        await new LocalVmxFileOperations().WriteNewAsync(temporary, bytes, token);
        token.ThrowIfCancellationRequested();
        File.Move(temporary, destination, false); // Never overwrite a prior recovery record.
    }

    public async Task<VmxBackupHistoryResult> ReadAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!Directory.Exists(directory)) return new(Array.Empty<VmxBackupHistoryEntry>(), 0);
        var entries = new List<VmxBackupHistoryEntry>();
        var unreadable = Directory.EnumerateFiles(directory, "*.tmp").Count();
        foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
        {
            token.ThrowIfCancellationRequested();
            try
            {
                await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                if (file.Length > 32768) throw new InvalidDataException("Recovery record exceeds the supported size.");
                var stored = await JsonSerializer.DeserializeAsync<Stored>(file, Options, token)
                    ?? throw new InvalidDataException("Recovery record is empty.");
                if (stored.SchemaVersion is not (1 or 2) || !Version.TryParse(stored.WorkstationVersion, out var version)
                    || stored.SchemaVersion == 1 && (stored.Backend is not null || stored.BeforeSettings is not null || stored.AfterSettings is not null))
                    throw new InvalidDataException("Recovery record version is unsupported.");
                var entry = new VmxBackupHistoryEntry(stored.Id, stored.RecordedAt, stored.Operation,
                    new(stored.TargetPath, stored.BackupPath, stored.OriginalHash, stored.AppliedHash, version)
                    { Backend = stored.SchemaVersion == 1 ? "vmx-file" : stored.Backend!,
                      BeforeSettings = stored.BeforeSettings, AfterSettings = stored.AfterSettings });
                Validate(entry);
                if (Path.GetFileName(path) != entry.Id.ToString("N") + ".json")
                    throw new InvalidDataException("Recovery record identity does not match its file.");
                entries.Add(entry);
            }
            catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException)
            { unreadable++; }
        }
        return new(entries.OrderByDescending(entry => entry.RecordedAt).ToArray(), unreadable);
    }

    private static void Validate(VmxBackupHistoryEntry entry)
    {
        if (entry.Id == Guid.Empty || entry.RecordedAt == default || entry.Operation is not ("apply" or "restore")
            || entry.Receipt is null) throw new InvalidDataException("Recovery record metadata is invalid.");
        var receipt = entry.Receipt;
        if (receipt.Backend is not ("vmx-file" or VmxApiConfigManager.BackendName)
            || receipt.Backend == VmxApiConfigManager.BackendName &&
               (receipt.BeforeSettings is null || receipt.AfterSettings is null
                || !receipt.BeforeSettings.IsValid || !receipt.AfterSettings.IsValid
                || receipt.BeforeSettings.CoresPerSocket != receipt.AfterSettings.CoresPerSocket
                || receipt.BeforeSettings.HardwareVersion != receipt.AfterSettings.HardwareVersion)
            || receipt.Backend == "vmx-file" && (receipt.BeforeSettings is not null || receipt.AfterSettings is not null))
            throw new InvalidDataException("Recovery backend or API settings are invalid.");
        if (!Path.IsPathFullyQualified(receipt.TargetPath) || !Path.IsPathFullyQualified(receipt.BackupPath)
            || !Path.GetExtension(receipt.TargetPath).Equals(".vmx", StringComparison.OrdinalIgnoreCase)
            || !Path.GetExtension(receipt.BackupPath).Equals(".bak", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Path.GetDirectoryName(Path.GetFullPath(receipt.TargetPath)),
                Path.GetDirectoryName(Path.GetFullPath(receipt.BackupPath)), StringComparison.OrdinalIgnoreCase)
            || receipt.WorkstationVersion is null || !ValidHash(receipt.OriginalHash) || !ValidHash(receipt.AppliedHash))
            throw new InvalidDataException("Recovery paths or hashes are invalid.");
    }

    private static bool ValidHash(string? value) => value is { Length: 64 } && value.All(Uri.IsHexDigit);
}

public sealed class VmxHistoryPersistenceException(VmxBackupHistoryEntry entry, Exception inner)
    : IOException("Configuration was applied, but recovery history could not be saved. Retain the backup path.", inner)
{
    public VmxBackupHistoryEntry Entry { get; } = entry;
}

public sealed class VmxConfigWorkflow(IVmxConfigTransaction manager, IVmxBackupHistory history)
{
    public async Task<VmxBackupHistoryEntry> ApplyAsync(VmxPreparedChange preview, Guid confirmedPreviewId,
        bool restoring, CancellationToken token = default)
    {
        var receipt = await manager.ApplyAsync(preview, confirmedPreviewId, token);
        var entry = new VmxBackupHistoryEntry(Guid.NewGuid(), DateTimeOffset.UtcNow, restoring ? "restore" : "apply", receipt);
        try { await history.SaveAsync(entry, CancellationToken.None); }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
        { throw new VmxHistoryPersistenceException(entry, error); }
        // Once the configuration is applied, history persistence is not cancelled halfway.
        return entry;
    }
}
