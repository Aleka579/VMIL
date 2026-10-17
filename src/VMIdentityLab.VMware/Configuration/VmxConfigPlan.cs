using System.Globalization;
using System.Security.Cryptography;

namespace VMIdentityLab.VMware.Configuration;

public sealed record VmxConfigChange(string Key, string Before, string After, string Unit);

public sealed class VmxConfigPlan
{
    private readonly byte[] proposed;
    public string OriginalHash { get; }
    public string ProposedHash { get; }
    public IReadOnlyList<VmxConfigChange> Changes { get; }
    public bool RequiresTopologyReview { get; }

    internal VmxConfigPlan(VmxDocument document, byte[] bytes, List<VmxConfigChange> changes, bool topologyReview)
    {
        OriginalHash = document.ContentHash;
        proposed = bytes.ToArray();
        ProposedHash = Convert.ToHexString(SHA256.HashData(proposed));
        Changes = changes.AsReadOnly();
        RequiresTopologyReview = topologyReview;
    }

    public byte[] GetProposedBytes() => proposed.ToArray();
}

public static class VmxConfigPlanner
{
    public static VmxConfigPlan Create(VmxDocument document, IReadOnlyDictionary<string, string> requested)
    {
        if (document.HasAmbiguity || document.Limitations.Count != 0
            || document.Entries.Any(item => item.Key.StartsWith("encryption.", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("Ambiguous, unsupported or encrypted input cannot be edited.");
        if (requested.Count is < 1 or > 2) throw new ArgumentException("Select one or two candidate settings.");
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var changes = new List<VmxConfigChange>();
        var topologyReview = false;
        foreach (var (key, value) in requested)
        {
            var canonical = key.ToLowerInvariant();
            if (canonical is not ("memsize" or "numvcpus") || !replacements.TryAdd(canonical, value))
                throw new ArgumentException("Only memory and vCPU candidates can be previewed.");
            var before = document.Entries.SingleOrDefault(item => item.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidDataException("Candidate setting is missing; implicit defaults are not rewritten.");
            var number = PositiveNumber(value);
            var originalNumber = PositiveNumber(before.Value);
            if (canonical == "memsize" && (number % 4 != 0 || originalNumber % 4 != 0))
                throw new ArgumentException("Memory preview values must be a multiple of 4 MiB; this is not write approval.");
            if (canonical == "numvcpus" && before.Value != value)
            {
                var cores = document.Entries.SingleOrDefault(item => item.Key.Equals("cpuid.coresPerSocket", StringComparison.OrdinalIgnoreCase));
                if (cores is null) topologyReview = true;
                else if (number % PositiveNumber(cores.Value) != 0 || originalNumber % PositiveNumber(cores.Value) != 0)
                    throw new ArgumentException("vCPU count must preserve the declared cores-per-socket topology.");
            }
            if (before.Value != value) changes.Add(new(canonical, before.Value, value, canonical == "memsize" ? "MiB" : "count"));
        }
        if (changes.Count == 0) throw new ArgumentException("No changed values.");
        var bytes = document.ReplaceNumericValues(replacements);
        var parsed = VmxDocument.Parse(bytes);
        if (parsed.HasAmbiguity || parsed.Limitations.Count != 0) throw new InvalidDataException("Proposed VMX could not be verified.");
        return new(document, bytes, changes, topologyReview);
    }

    internal static int PositiveNumber(string value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number > 0
            ? number : throw new ArgumentException("A positive decimal integer is required.");
}
