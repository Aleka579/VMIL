using System.Text.Json;

namespace VMIdentityLab.App.Infrastructure;

public static class DiagnosticLog
{
    public static void Write(string operation, string status, string code, int? findingCount = null, bool? elevated = null) =>
        Console.WriteLine(JsonSerializer.Serialize(new { timestamp = DateTimeOffset.UtcNow, operation, status, code, findingCount, elevated }));
}
