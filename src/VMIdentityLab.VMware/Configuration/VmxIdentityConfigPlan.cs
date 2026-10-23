using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace VMIdentityLab.VMware.Configuration;

public enum IdentityConfigField { StaticMac, BiosUuid }

// Private recovery metadata. Never add these values to diagnostic reports or logs.
public sealed record VmxIdentitySettings(string MacAddress, string BiosUuid, int HardwareVersion, string RelatedHash)
{
    [JsonIgnore]
    public bool IsValid => HardwareVersion == 19 && VmxIdentityConfigPlanner.IsMac(MacAddress)
        && VmxIdentityConfigPlanner.IsUuid(BiosUuid) && RelatedHash is { Length: 64 } && RelatedHash.All(Uri.IsHexDigit);

    internal static VmxIdentitySettings From(VmxDocument document)
    {
        VmxIdentityConfigPlanner.ValidateDocument(document);
        var values = document.Entries.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        var related = values.Where(item => !VmxIdentityConfigPlanner.IsIdentityKey(item.Key)
            && (item.Key.StartsWith("ethernet", StringComparison.OrdinalIgnoreCase)
                || item.Key.StartsWith("uuid.", StringComparison.OrdinalIgnoreCase)
                || item.Key.StartsWith("smbios.", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Select(item => new[] { item.Key.ToLowerInvariant(), item.Value }).ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(related)));
        return new(values["ethernet0.address"], values["uuid.bios"], 19, hash);
    }

    internal bool Matches(WorkstationIdentitySettings settings) => MacAddress == settings.MacAddress && BiosUuid == settings.BiosUuid;
    internal bool HasSameContext(VmxIdentitySettings other) => HardwareVersion == other.HardwareVersion && RelatedHash == other.RelatedHash;

    internal static string ChangedKey(VmxIdentitySettings before, VmxIdentitySettings after)
    {
        if (!before.IsValid || !after.IsValid || !before.HasSameContext(after)
            || (before.MacAddress == after.MacAddress) == (before.BiosUuid == after.BiosUuid))
            throw new InvalidDataException("An existing single-field identity round trip is required.");
        return before.MacAddress != after.MacAddress ? "ethernet0.address" : "uuid.bios";
    }
}

// A separate, limited M11 candidate. M10 vendor generation/copy plans stay non-executable.
public static class VmxIdentityConfigPlanner
{
    public static Version VerifiedVersion { get; } = new(17, 5, 2, 23775571);
    public static bool IsIdentityKey(string key) => key.Equals("ethernet0.address", StringComparison.OrdinalIgnoreCase)
        || key.Equals("uuid.bios", StringComparison.OrdinalIgnoreCase);

    public static async Task<VmxPreparedChange> PrepareAsync(string target, Version version, IdentityConfigField field,
        CancellationToken token = default)
    {
        var path = VmxApiFiles.Validate(target, writable: false);
        var document = VmxDocument.Parse(await VmxApiFiles.ReadExclusiveAsync(path, token));
        return new(path, version, Create(document, field));
    }

    public static VmxConfigPlan Create(VmxDocument document, IdentityConfigField field)
    {
        var state = VmxIdentitySettings.From(document);
        var key = field switch { IdentityConfigField.StaticMac => "ethernet0.address", IdentityConfigField.BiosUuid => "uuid.bios",
            _ => throw new ArgumentException("Unknown identity candidate.", nameof(field)) };
        string value;
        do
        {
            var bytes = RandomNumberGenerator.GetBytes(field == IdentityConfigField.StaticMac ? 6 : 16);
            if (field == IdentityConfigField.StaticMac)
            {
                bytes[0] = 0; bytes[1] = 0x50; bytes[2] = 0x56; bytes[3] &= 0x3f;
                value = string.Join(":", bytes.Select(item => item.ToString("x2")));
            }
            else
            {
                bytes[0] = 0x56; bytes[1] = 0x4d;
                value = string.Join(" ", bytes.Take(8).Select(item => item.ToString("x2"))) + "-"
                    + string.Join(" ", bytes.Skip(8).Select(item => item.ToString("x2")));
            }
        } while (value.Equals(field == IdentityConfigField.StaticMac ? state.MacAddress : state.BiosUuid, StringComparison.OrdinalIgnoreCase));
        return CreateRestore(document, key, value);
    }

    internal static VmxConfigPlan CreateRestore(VmxDocument document, string key, string value)
    {
        ValidateDocument(document);
        ValidateChange(new(key, document.Entries.Single(item => item.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value, value, ""));
        var before = document.Entries.Single(item => item.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
        var proposed = document.ReplaceValues(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [key] = value });
        return new(document, proposed, [new(key, before, value, "")], false);
    }

    internal static void ValidateDocument(VmxDocument document)
    {
        if (document.HasAmbiguity || document.Limitations.Count != 0
            || document.Entries.Any(item => item.Key.StartsWith("encryption.", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("Ambiguous, unsupported or encrypted identity input.");
        var values = document.Entries.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        bool Is(string key, string expected) => values.TryGetValue(key, out var value) && value.Equals(expected, StringComparison.OrdinalIgnoreCase);
        if (!Is("virtualHW.version", "19") || !Is("ethernet0.present", "TRUE") || !Is("ethernet0.addressType", "static")
            || !Is("ethernet0.connectionType", "hostonly") || !Is("ethernet0.startConnected", "FALSE")
            || values.Keys.Any(key => key.Contains("generatedAddress", StringComparison.OrdinalIgnoreCase)
                || Regex.IsMatch(key, @"^ethernet[1-9][0-9]*\.", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            || !values.TryGetValue("ethernet0.address", out var mac) || !IsMac(mac)
            || !values.TryGetValue("uuid.bios", out var uuid) || !IsUuid(uuid))
            throw new InvalidDataException("Identity candidates require explicit existing vendor-format identities and a single disconnected static host-only NIC on hardware 19.");
    }

    internal static void ValidateChange(VmxConfigChange change)
    {
        if (change.Before == change.After || (change.Key switch
            { "ethernet0.address" => !IsMac(change.Before) || !IsMac(change.After),
              "uuid.bios" => !IsUuid(change.Before) || !IsUuid(change.After), _ => true }))
            throw new ArgumentException("Only reviewed existing static MAC or BIOS UUID values are supported.");
    }

    internal static bool IsSupported(VmxPreparedChange preview)
    {
        if (preview.WorkstationVersion != VerifiedVersion || !preview.Plan.IsIdentityChange) return false;
        ValidateChange(preview.Plan.Changes[0]);
        ValidateDocument(VmxDocument.Parse(preview.Plan.GetProposedBytes()));
        return true;
    }

    internal static bool IsMac(string value) => value is { Length: 17 } && Regex.IsMatch(value,
        @"^00:50:56:[0-3][0-9a-f]:[0-9a-f]{2}:[0-9a-f]{2}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    internal static bool IsUuid(string value) => value is { Length: 47 } && Regex.IsMatch(value,
        @"^56 4d(?: [0-9a-f]{2}){6}-(?:[0-9a-f]{2} ){7}[0-9a-f]{2}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    internal static bool SameConfiguration(VmxDocument expected, VmxDocument actual)
    {
        if (actual.HasAmbiguity || actual.Limitations.Count != 0) return false;
        var left = expected.Entries.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        var right = actual.Entries.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        return left.Count == right.Count && left.All(item => right.TryGetValue(item.Key, out var value) && value == item.Value)
            && expected.Lines.Where(line => line.Kind == VmxLineKind.Comment).Select(line => line.Raw.TrimEnd('\r', '\n'))
                .SequenceEqual(actual.Lines.Where(line => line.Kind == VmxLineKind.Comment).Select(line => line.Raw.TrimEnd('\r', '\n')))
            && expected.InlineComments().SequenceEqual(actual.InlineComments());
    }
}
