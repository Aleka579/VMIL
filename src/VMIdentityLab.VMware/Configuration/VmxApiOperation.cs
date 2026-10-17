using System.Security.Cryptography;
using System.Text.Json;

namespace VMIdentityLab.VMware.Configuration;

public sealed record VmxApiSettings(int MemoryMiB, int Processors, int CoresPerSocket, int HardwareVersion)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsValid => MemoryMiB > 0 && MemoryMiB % 4 == 0 && Processors > 0
        && CoresPerSocket > 0 && Processors % CoresPerSocket == 0 && HardwareVersion > 0;
    internal static VmxApiSettings From(VmxDocument document)
    {
        if (document.HasAmbiguity || document.Limitations.Count != 0
            || document.Entries.Any(item => item.Key.StartsWith("encryption.", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("Unsupported configuration cannot be sent to the API.");
        int Value(string name) => VmxConfigPlanner.PositiveNumber(document.Entries.SingleOrDefault(
            item => item.Key.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value
            ?? throw new InvalidDataException("An explicit setting is required."));
        var result = new VmxApiSettings(Value("memsize"), Value("numvcpus"), Value("cpuid.coresPerSocket"), Value("virtualHW.version"));
        return result.IsValid ? result : throw new InvalidDataException("The configuration topology is invalid.");
    }
    internal bool Matches(WorkstationVmSettings settings) => settings.MemoryMiB == MemoryMiB && settings.Processors == Processors;
}

// Finite provider evidence: docs/V02-FULL-TEST-RESULTS-2026-10-17.md.
// A racing VMware start can change boot metadata while power still reports off. The transaction
// must retain its intent on any unexpected content/state; this policy does not relax those checks.
// This is separate from the legacy file policy. No environment/CLI approval bypass.
public sealed class ReleasedVmxApiWritePolicy : IVmxWritePolicy
{
    private static readonly Version VerifiedVersion = new(17, 5, 2, 23775571);
    public bool IsApproved(Version workstationVersion, VmxConfigChange change)
        => workstationVersion == VerifiedVersion && change.Before != change.After && (change.Key switch
        {
            "memsize" => change.Before is "4096" or "6144" && change.After is "4096" or "6144",
            "numvcpus" => change.Before is "2" or "4" && change.After is "2" or "4",
            _ => false
        });
    public static bool IsTestedCandidate(VmxPreparedChange preview)
    {
        if (preview.WorkstationVersion != VerifiedVersion || preview.Plan.RequiresTopologyReview) return false;
        var after = VmxApiSettings.From(VmxDocument.Parse(preview.Plan.GetProposedBytes()));
        return after.HardwareVersion == 19 && after.CoresPerSocket == 2
            && after.MemoryMiB is 4096 or 6144 && after.Processors is 2 or 4
            && preview.Plan.Changes.All(change => change.Key switch
            {
                "memsize" => change.Before is "4096" or "6144" && change.After is "4096" or "6144",
                "numvcpus" => change.Before is "2" or "4" && change.After is "2" or "4",
                _ => false
            });
    }
}

public sealed class VmxApiTransactionException(string backupPath, string proposedPath, string journalPath,
    bool outcomeUnknown, Exception inner) : IOException("The API operation stopped. Inspect retained recovery evidence before retrying.", inner)
{
    public string BackupPath { get; } = backupPath;
    public string ProposedPath { get; } = proposedPath;
    public string JournalPath { get; } = journalPath;
    public bool OutcomeUnknown { get; } = outcomeUnknown;
}

public sealed class VmxApiBusyException : IOException
{
    public VmxApiBusyException() : base("Another application instance is managing this configuration.") { }
}

internal static class VmxApiFiles
{
    internal static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    internal static string Validate(string path, string extension = ".vmx", bool writable = true)
    {
        var full = Path.GetFullPath(path);
        if (!Path.IsPathFullyQualified(path) || full.StartsWith(@"\\", StringComparison.Ordinal)
            || !Path.GetExtension(full).Equals(extension, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A local configuration or backup path is required.");
        var item = new FileInfo(full);
        if (!item.Exists || (item.Attributes & FileAttributes.ReparsePoint) != 0
            || writable && (item.Attributes & FileAttributes.ReadOnly) != 0) throw new IOException("Configuration is unavailable or protected.");
        for (var parent = item.Directory; parent is not null; parent = parent.Parent)
            if ((parent.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Redirected paths are unsupported.");
        return full;
    }
    internal static async Task<byte[]> ReadExclusiveAsync(string path, CancellationToken token)
    {
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, 8192, true);
        if (file.Length > VmxReader.MaxVmxBytes) throw new InvalidDataException("Configuration exceeds the supported size.");
        var bytes = new byte[(int)file.Length];
        await file.ReadExactlyAsync(bytes, token);
        return bytes;
    }
}

// Persistent sidecars are not VMware .lck files. Never delete a live lease or an unresolved intent.
internal sealed class VmxApiOperation : IDisposable
{
    private readonly FileStream lease;
    internal Guid Id { get; } = Guid.NewGuid();
    internal string PendingPath { get; }
    internal string BackupPath { get; }
    internal string ProposedPath { get; }
    internal VmxApiOperation(string target)
    {
        var leasePath = target + ".vmidentitylab-api.lease";
        if (File.Exists(leasePath) && (File.GetAttributes(leasePath) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Redirected operation lease is unsupported.");
        try { lease = new FileStream(leasePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
        catch (IOException error) when ((error.HResult & 0xFFFF) is 32 or 33)
        { throw new VmxApiBusyException(); }
        PendingPath = target + ".vmidentitylab-api.pending.json";
        var stem = target + ".vmidentitylab-api-" + Id.ToString("N");
        BackupPath = stem + ".bak";
        ProposedPath = stem + ".tmp";
        if (File.Exists(PendingPath) || Directory.Exists(PendingPath))
        {
            lease.Dispose();
            throw new VmxApiTransactionException("", "", PendingPath, true,
                new InvalidOperationException("An earlier API operation has an unresolved outcome."));
        }
    }
    internal async Task WriteIntentAsync(VmxPreparedChange preview, string vmId, IVmxFileOperations files, CancellationToken token)
    {
        var record = new { schemaVersion = 1, operationId = Id, backend = VmxApiConfigManager.BackendName,
            time = DateTimeOffset.UtcNow, targetPath = preview.TargetPath, vmId,
            backupPath = BackupPath, proposedPath = ProposedPath, originalHash = preview.Plan.OriginalHash,
            proposedHash = preview.Plan.ProposedHash, workstationVersion = preview.WorkstationVersion.ToString() };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(record);
        await files.WriteNewAsync(PendingPath, bytes, token);
        var written = await VmxApiFiles.ReadExclusiveAsync(PendingPath, token);
        if (!bytes.SequenceEqual(written))
            throw new InvalidDataException("The durable operation intent could not be verified.");
    }
    internal string Finish(string outcome)
    {
        var completed = PendingPath + "." + Id.ToString("N") + "." + outcome;
        File.Move(PendingPath, completed, false);
        return completed;
    }
    public void Dispose() => lease.Dispose();
}
