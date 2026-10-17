using System.Windows;
using System.IO;
using System.Security.Principal;
using VMIdentityLab.App.Infrastructure;
using VMIdentityLab.App.ViewModels;
using VMIdentityLab.Core.Reports;
using VMIdentityLab.VMware;
using VMIdentityLab.Windows;

namespace VMIdentityLab.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length == 0)
        {
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            MainWindow = new MainWindow(new MainViewModel());
            MainWindow.Show();
            return;
        }
        try
        {
            if (e.Args is ["--help"])
            {
                Console.WriteLine("VMIdentityLab --collect --output report.json [--html report.html] [--vmx selected.vmx]");
                Shutdown(0);
                return;
            }
            var options = ParseArguments(e.Args);
            if (options.Mode == "preview")
            {
                await PreviewRenderer.RenderAsync(options.Output, options.English, options.Page);
                DiagnosticLog.Write("preview", "completed", "syntheticPreview");
            }
            else
            {
                var report = await new WindowsCollector().CollectAsync();
                if (options.Vmx is not null)
                {
                    var vmx = await new VmxReader().ReadAsync(options.Vmx);
                    report = report with { VmxSummary = vmx.Document.ToSummary(vmx.PowerState) };
                }
                await ReportFiles.SaveJsonAsync(options.Output, report);
                if (options.Html is not null) await ReportFiles.SaveHtmlAsync(options.Html, report);
                using var identity = WindowsIdentity.GetCurrent();
                var elevated = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                DiagnosticLog.Write("collect", "completed", report.Diagnostics.Count == 0 ? "collection.complete" : "collection.partial", report.Findings.Count, elevated);
            }
            Shutdown(0);
        }
        catch (Exception error)
        {
            DiagnosticLog.Write("startup", "failed", error.GetType().Name);
            Shutdown(2);
        }
    }

    private sealed record Options(string Mode, string Output, string? Html, string? Vmx, bool English, int Page);

    private static Options ParseArguments(string[] args)
    {
        string? mode = null, output = null, html = null, vmx = null;
        var english = false;
        var page = 0;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is "--collect" or "--render-preview")
            {
                if (mode is not null) throw new ArgumentException("Choose one mode.");
                mode = args[i] == "--collect" ? "collect" : "preview";
                continue;
            }
            if (i + 1 == args.Length) throw new ArgumentException("A required option value is missing.");
            var key = args[i];
            var value = args[++i];
            switch (key)
            {
                case "--output": output = value; break;
                case "--html": html = value; break;
                case "--vmx": vmx = value; break;
                case "--language" when value is "en-US" or "zh-CN": english = value == "en-US"; break;
                case "--page" when int.TryParse(value, out var index) && index is >= 0 and <= 4: page = index; break;
                default: throw new ArgumentException("An option is not supported.");
            }
        }
        if (mode is null || string.IsNullOrWhiteSpace(output)) throw new ArgumentException("Mode and output are required.");
        if (mode == "collect" && (!string.Equals(Path.GetExtension(output), ".json", StringComparison.OrdinalIgnoreCase)
            || html is not null && !string.Equals(Path.GetExtension(html), ".html", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Expected JSON and optional HTML output paths.");
        if (mode == "preview" && !string.Equals(Path.GetExtension(output), ".png", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Expected PNG output path.");
        return new(mode, output, html, vmx, english, page);
    }
}
