using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using VMIdentityLab.Core.Reports;

namespace VMIdentityLab.Core.Comparison;

public static class ValidationSnapshotJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, MaxDepth = 32,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };

    public static byte[] SerializePrivate(PrivateValidationSnapshot snapshot)
    {
        Validate(snapshot);
        if (snapshot.DataKind == ValidationDataKind.LegacyReport) throw new ReportFormatException("Legacy reports have no private verification snapshot.");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(snapshot with { Report = ReportPrivacy.Apply(snapshot.Report) }, Options);
        if (bytes.Length > ReportJson.MaxReportBytes) throw new ReportFormatException("Snapshot exceeds the supported size.");
        return bytes;
    }

    public static PrivateValidationSnapshot Deserialize(ReadOnlySpan<byte> bytes, ValidationPhase expectedPhase)
    {
        if (bytes.Length > ReportJson.MaxReportBytes || !Enum.IsDefined(expectedPhase)) throw new ReportFormatException("Invalid snapshot size or phase.");
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray(), new() { MaxDepth = 32 });
            CheckUniqueProperties(document.RootElement);
            var schema = document.RootElement.GetProperty("schemaVersion").GetString();
            if (schema == DiagnosticReport.CurrentSchemaVersion)
                return new() { Phase = expectedPhase, DataKind = ValidationDataKind.LegacyReport, Report = ReportJson.Deserialize(bytes) };
            var snapshot = JsonSerializer.Deserialize<PrivateValidationSnapshot>(bytes, Options) ?? throw new JsonException();
            Validate(snapshot);
            if (snapshot.Phase != expectedPhase || snapshot.DataKind == ValidationDataKind.LegacyReport) throw new JsonException();
            return snapshot with { Report = ReportPrivacy.Apply(snapshot.Report) };
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException)
        { throw new ReportFormatException("Invalid verification snapshot; raw input is not included in this error."); }
    }

    public static async Task<PrivateValidationSnapshot> ReadAsync(string path, ValidationPhase phase, CancellationToken token = default)
    {
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, true);
        if (file.Length > ReportJson.MaxReportBytes) throw new ReportFormatException("Snapshot exceeds the supported size.");
        var bytes = new byte[(int)file.Length];
        await file.ReadExactlyAsync(bytes, token);
        return Deserialize(bytes, phase);
    }

    public static Task SavePrivateAsync(string path, PrivateValidationSnapshot snapshot, CancellationToken token = default)
        => ReportFiles.SaveBytesAsync(Extension(path, ".json"), SerializePrivate(snapshot), cancellationToken: token);

    public static Task SavePublicJsonAsync(string path, IdentityValidationResult result, CancellationToken token = default)
        => ReportFiles.SaveBytesAsync(Extension(path, ".json"), JsonSerializer.SerializeToUtf8Bytes(result, Options), cancellationToken: token);

    public static Task SavePublicHtmlAsync(string path, IdentityValidationResult result, CancellationToken token = default)
        => ReportFiles.SaveBytesAsync(Extension(path, ".html"), Encoding.UTF8.GetBytes(IdentityValidationComparer.RenderHtml(result)), cancellationToken: token);

    internal static void Validate(PrivateValidationSnapshot snapshot)
    {
        if (snapshot.SchemaVersion != PrivateValidationSnapshot.Schema || !Enum.IsDefined(snapshot.Phase)
            || !Enum.IsDefined(snapshot.DataKind) || snapshot.Report is null || snapshot.Identities is null
            || snapshot.NetworkAdapterId == Guid.Empty
            || snapshot.DataKind != ValidationDataKind.LegacyReport && (snapshot.SessionId == Guid.Empty || snapshot.Elevated is null)
            || snapshot.DataKind == ValidationDataKind.LegacyReport && (snapshot.Identities.Count != 0 || snapshot.SessionId != Guid.Empty || snapshot.NetworkAdapterId is not null))
            throw new ReportFormatException("Snapshot metadata is missing or unsupported.");
        ReportJson.Validate(snapshot.Report);
        if (snapshot.Identities.Count > 2 || snapshot.Identities.Select(item => item?.Id).Distinct(StringComparer.Ordinal).Count() != snapshot.Identities.Count
            || snapshot.Identities.Any(item => item is null || item.Id is not ("smbios.uuid" or "network.macAddress")
                || item.Source is null || item.Source.Length > 512 || item.Value is null || !item.Value.IsWellFormed()
                || item.Value.Text?.Length > 4096 || !Enum.IsDefined(item.Status) || item.ObservedAt == default))
            throw new ReportFormatException("Unsupported private observation metadata.");
    }

    private static string Extension(string path, string extension) => Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase)
        ? path : throw new ArgumentException("Select the expected output extension.");

    private static void CheckUniqueProperties(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new JsonException();
                CheckUniqueProperties(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array) foreach (var item in value.EnumerateArray()) CheckUniqueProperties(item);
    }
}
