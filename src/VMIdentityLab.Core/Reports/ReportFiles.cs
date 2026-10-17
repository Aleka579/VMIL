using System.Text;

namespace VMIdentityLab.Core.Reports;

public static class ReportFiles
{
    public static Task SaveJsonAsync(string path, DiagnosticReport report, bool overwriteConfirmed = false,
        CancellationToken cancellationToken = default) => SaveBytesAsync(WithExtension(path, ".json"), ReportJson.Serialize(report), overwriteConfirmed, cancellationToken);

    public static Task SaveHtmlAsync(string path, DiagnosticReport report, bool overwriteConfirmed = false,
        CancellationToken cancellationToken = default) => SaveBytesAsync(WithExtension(path, ".html"), Encoding.UTF8.GetBytes(HtmlReportRenderer.Render(report)), overwriteConfirmed, cancellationToken);

    private static string WithExtension(string path, string extension) =>
        string.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase)
            ? path : throw new ArgumentException("Select an output path with the expected report extension.");

    public static async Task SaveBytesAsync(string path, byte[] bytes, bool overwriteConfirmed = false,
        CancellationToken cancellationToken = default)
    {
        var destination = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(destination)!;
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, ".vmidentitylab-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            await using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, true))
            {
                await file.WriteAsync(bytes, cancellationToken);
                await file.FlushAsync(cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, destination, overwriteConfirmed);
        }
        finally
        {
            // Only this operation's newly created temporary file is removed.
            File.Delete(temporary);
        }
    }
}
