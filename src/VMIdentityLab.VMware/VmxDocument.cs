using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using VMIdentityLab.Core.Reports;

namespace VMIdentityLab.VMware;

public enum VmxLineKind { Blank, Comment, Entry, Unknown }
public sealed record VmxEntry(string Key, string Value, int LineNumber);
public sealed record VmxLine(int Number, string Raw, VmxLineKind Kind, VmxEntry? Entry);

public sealed partial class VmxDocument
{
    private readonly byte[] original;
    public string ContentHash { get; }
    public string EncodingName { get; }
    public IReadOnlyList<VmxLine> Lines { get; }
    public IReadOnlyList<string> Limitations { get; }
    public IReadOnlyList<string> DuplicateKeys { get; }
    public bool HasAmbiguity => DuplicateKeys.Count != 0 || Lines.Any(line => line.Kind == VmxLineKind.Unknown);

    private VmxDocument(byte[] bytes, string encodingName, IReadOnlyList<VmxLine> lines, IReadOnlyList<string> limitations)
    {
        original = bytes.ToArray();
        ContentHash = Convert.ToHexString(SHA256.HashData(original));
        EncodingName = encodingName;
        Lines = lines;
        Limitations = limitations;
        DuplicateKeys = Entries.GroupBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
    }

    public IEnumerable<VmxEntry> Entries => Lines.Where(line => line.Entry is not null).Select(line => line.Entry!);
    public byte[] GetOriginalBytes() => original.ToArray();

    internal IEnumerable<(string Key, string Comment)> InlineComments()
        => Lines.Where(line => line.Entry is not null).OrderBy(line => line.Entry!.Key, StringComparer.OrdinalIgnoreCase)
            .Select(line => (line.Entry!.Key.ToLowerInvariant(),
                EntryLine().Match(line.Raw.TrimEnd('\r', '\n')).Groups["comment"].Value.TrimEnd()));

    // Pure transformation for previews. File writes are owned by VmxConfigManager.
    internal byte[] ReplaceValues(IReadOnlyDictionary<string, string> replacements)
    {
        if (HasAmbiguity || Limitations.Count != 0) throw new InvalidDataException("VMX input is not editable.");
        var bom = original.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }) ? 3
            : original.AsSpan().StartsWith(new byte[] { 0xFF, 0xFE }) || original.AsSpan().StartsWith(new byte[] { 0xFE, 0xFF }) ? 2 : 0;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding(EncodingName, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        var text = string.Concat(Lines.Select(line => line.Raw));
        if (!encoding.GetBytes(text).AsSpan().SequenceEqual(original.AsSpan(bom)))
            throw new InvalidDataException("VMX encoding cannot round-trip unchanged.");
        var updated = string.Concat(Lines.Select(line =>
        {
            if (line.Entry is null || !replacements.TryGetValue(line.Entry.Key, out var value)) return line.Raw;
            var match = EntryLine().Match(line.Raw.TrimEnd('\r', '\n'));
            var group = match.Groups["value"];
            return line.Raw[..group.Index] + value + line.Raw[(group.Index + group.Length)..];
        }));
        return [.. original.AsSpan(0, bom), .. encoding.GetBytes(updated)];
    }

    public static VmxDocument Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > VmxReader.MaxVmxBytes) throw new InvalidDataException("VMX exceeds the 2 MiB limit.");
        var raw = bytes.ToArray();
        var limitations = new List<string>();
        Encoding encoding;
        var skip = 0;
        if (bytes.StartsWith(new byte[] { 0xEF, 0xBB, 0xBF })) { encoding = new UTF8Encoding(false, true); skip = 3; }
        else if (bytes.StartsWith(new byte[] { 0xFF, 0xFE })) { encoding = new UnicodeEncoding(false, false, true); skip = 2; }
        else if (bytes.StartsWith(new byte[] { 0xFE, 0xFF })) { encoding = new UnicodeEncoding(true, false, true); skip = 2; }
        else
        {
            var header = Encoding.ASCII.GetString(bytes[..Math.Min(bytes.Length, 4096)]);
            var declaration = EncodingDeclaration().Match(header);
            var name = declaration.Success ? declaration.Groups[1].Value.ToLowerInvariant() : "utf-8";
            if (name is "utf-8" or "utf8") encoding = new UTF8Encoding(false, true);
            else if (name is "windows-1252" or "cp1252" or "gbk" or "cp936" or "windows-936")
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var codePage = name is "windows-1252" or "cp1252" ? 1252 : 936;
                encoding = Encoding.GetEncoding(codePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            }
            else return new(raw, name, [], ["Declared encoding is not supported; original bytes are retained."]);
        }

        string text;
        try { text = encoding.GetString(raw, skip, raw.Length - skip); }
        catch (DecoderFallbackException) { return new(raw, encoding.WebName, [], ["Input contains invalid encoded bytes; original bytes are retained."]); }

        var lines = new List<VmxLine>();
        foreach (Match match in RawLine().Matches(text))
        {
            if (match.Length == 0) continue;
            var number = lines.Count + 1;
            var rawLine = match.Value;
            var body = rawLine.TrimEnd('\r', '\n');
            var trimmed = body.Trim();
            if (trimmed.Length == 0) lines.Add(new(number, rawLine, VmxLineKind.Blank, null));
            else if (trimmed.StartsWith('#')) lines.Add(new(number, rawLine, VmxLineKind.Comment, null));
            else
            {
                var entry = EntryLine().Match(body);
                if (!entry.Success)
                {
                    lines.Add(new(number, rawLine, VmxLineKind.Unknown, null));
                    limitations.Add($"Unconfirmed syntax at line {number}; original content is retained.");
                }
                else
                {
                    var item = new VmxEntry(entry.Groups["key"].Value, entry.Groups["value"].Value, number);
                    lines.Add(new(number, rawLine, VmxLineKind.Entry, item));
                    if (item.Value.Contains('|')) limitations.Add($"Encoded value at line {number} is preserved without interpretation.");
                }
            }
        }
        if (lines.Count == 0) limitations.Add("No confirmed configuration entries.");
        return new(raw, encoding.WebName, lines, limitations);
    }

    public VmxSummary ToSummary(PowerStateEvidence state)
    {
        var declarations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in Entries)
        {
            if (!ReportPrivacy.PublicVmxKeys.Contains(item.Key) || DuplicateKeys.Contains(item.Key, StringComparer.OrdinalIgnoreCase)) continue;
            var key = item.Key.ToLowerInvariant();
            var valid = key switch
            {
                "memsize" or "numvcpus" or "virtualhw.version" => long.TryParse(item.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0,
                "firmware" => item.Value is "efi" or "bios",
                "guestos" => PublicIdentifier().IsMatch(item.Value),
                _ => false
            };
            if (valid) declarations[item.Key] = item.Value;
        }
        return new()
        {
            Declarations = declarations,
            PowerState = state.State switch { VmPowerState.PoweredOn => "poweredOn", VmPowerState.PoweredOff => "poweredOff", VmPowerState.Suspended => "suspended", _ => "unknown" },
            StateSource = state.Source,
            Limitations = Limitations.Concat(DuplicateKeys.Select(_ => "Duplicate key: ambiguous declaration."))
                .Append("Configuration declarations are not guest observations or verified writable settings.")
                .Append(state.Explanation).ToArray()
        };
    }

    [GeneratedRegex("(?m)^\\s*\\.encoding\\s*=\\s*\"([^\"]+)\"", RegexOptions.CultureInvariant, 200)]
    private static partial Regex EncodingDeclaration();
    [GeneratedRegex(@"[^\r\n]*(?:\r\n|\r|\n|$)", RegexOptions.CultureInvariant, 200)]
    private static partial Regex RawLine();
    [GeneratedRegex("^\\s*(?<key>[A-Za-z0-9_.:-]+)\\s*=\\s*\"(?<value>[^\"]*)\"\\s*(?<comment>#.*)?$", RegexOptions.CultureInvariant, 200)]
    private static partial Regex EntryLine();
    [GeneratedRegex(@"^[A-Za-z0-9_-]{1,64}$", RegexOptions.CultureInvariant, 200)]
    private static partial Regex PublicIdentifier();
}
