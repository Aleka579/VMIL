using VMIdentityLab.Core.Reports;

namespace VMIdentityLab.VMware;

public static class VmxIdentityReader
{
    // Presence-only inspection of documented declarations; no raw identifiers leave this reader.
    public static IReadOnlyDictionary<string, IdentityDeclaration> Read(VmxDocument document)
    {
        var result = IdentityAnalyzer.ReadDeclarations(document.ToSummary(PowerStateEvidence.Unknown("Read-only snapshot")))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        foreach (var (id, key) in new[] { ("cpu.logicalProcessors", "numvcpus"), ("system.memoryBytes", "memsize"), ("system.os", "guestOS") })
        {
            var entries = document.Entries.Where(item => item.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (entries.Length > 1) result[id] = new(DeclarationState.Ambiguous, new(), "VMX: " + key);
            else if (entries.Length == 1 && result[id].State == DeclarationState.NotIncluded)
                result[id] = new(DeclarationState.Invalid, new(), "VMX: " + key);
        }
        Inspect("smbios.uuid", item => item.Key.Equals("uuid.bios", StringComparison.OrdinalIgnoreCase), "VMX: uuid.bios");
        Inspect("network.macAddress", item => IsMacKey(item.Key), "VMX: ethernet*.address/generatedAddress (declarations only)");
        return result;

        void Inspect(string id, Func<VmxEntry, bool> match, string source)
        {
            var entries = document.Entries.Where(match).ToArray();
            var state = document.Limitations.Count != 0 ? DeclarationState.Invalid
                : entries.GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1) ? DeclarationState.Ambiguous
                : entries.Length == 0 ? DeclarationState.NotIncluded
                : entries.Any(item => string.IsNullOrWhiteSpace(item.Value)) ? DeclarationState.Invalid : DeclarationState.Redacted;
            result[id] = new(state, state == DeclarationState.Redacted ? FindingValue.Redacted() : new(), source);
        }
    }

    private static bool IsMacKey(string key)
    {
        var parts = key.Split('.');
        return parts.Length == 2 && parts[0].StartsWith("ethernet", StringComparison.OrdinalIgnoreCase)
            && parts[0].Length > 8 && parts[0].AsSpan(8).IndexOfAnyExceptInRange('0', '9') < 0
            && (parts[1].Equals("address", StringComparison.OrdinalIgnoreCase) || parts[1].Equals("generatedAddress", StringComparison.OrdinalIgnoreCase));
    }
}
