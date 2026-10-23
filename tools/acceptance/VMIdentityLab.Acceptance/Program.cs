using System.IO;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using VMIdentityLab.App;
using VMIdentityLab.App.Infrastructure;
using VMIdentityLab.App.ViewModels;
using VMIdentityLab.VMware.Configuration;

namespace VMIdentityLab.Acceptance;

internal sealed record Manifest(string TargetPath, string HistoryDirectory, string InstallationDirectory,
    string VmwareSha256, string VmrestSha256);

internal static class Program
{
    internal static readonly Version Version = new(17, 5, 2, 23775571);
    internal sealed class CandidatePolicy : IVmxWritePolicy
    {
        public bool IsApproved(Version version, VmxConfigChange change)
            => version == Version && change.Key is "ethernet0.address" or "uuid.bios" && change.Before != change.After;
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            if (new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
                throw new InvalidOperationException("Acceptance must run without administrator privileges.");
            if (args is not ["--self-test", _] && args is not ["--validation-fixture", _] && args.Length != 1)
                throw new ArgumentException("Use one reviewed manifest, --self-test DIRECTORY, or --validation-fixture DIRECTORY.");
            var app = new AcceptanceApp(args);
            // A different Application class cannot load App.xaml's x:Class.
            // Reuse its two actual resource dictionaries and production window.
            foreach (var resource in new[] { "Theme", "Strings.zh-CN" })
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(
                    $"pack://application:,,,/VMIdentityLab.App;component/Resources/{resource}.xaml", UriKind.Absolute) });
            app.DispatcherUnhandledException += (_, error) =>
            {
                error.Handled = true;
                Console.Error.WriteLine("acceptance.dispatcher.failed: " + error.Exception.GetType().Name);
                app.Shutdown(1);
            };
            return app.Run();
        }
        catch (Exception error)
        {
            if (args is ["--self-test", var evidence])
                File.WriteAllText(Path.GetFullPath(evidence) + ".startup-error.txt", error.ToString());
            Console.Error.WriteLine("acceptance.startup.failed: " + error.GetType().Name);
            return 1;
        }
    }

    private sealed class AcceptanceApp(string[] args) : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown;
                if (args is ["--validation-fixture", var fixture])
                {
                    await ValidationFixture.WriteAsync(Path.GetFullPath(fixture));
                    Shutdown(0);
                    return;
                }
                if (args is ["--self-test", var evidence])
                {
                    await SelfTestAsync(Path.GetFullPath(evidence));
                    Shutdown(0);
                    return;
                }
                var manifestPath = Path.GetFullPath(args[0]);
                var bytes = await File.ReadAllBytesAsync(manifestPath);
                if (bytes.Length > 8192) throw new InvalidDataException("Manifest too large.");
                var manifest = JsonSerializer.Deserialize<Manifest>(bytes, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 4
                }) ?? throw new InvalidDataException("Manifest is missing.");
                ValidateManifest(manifestPath, manifest);
                // Same production window, view model, confirmation, credentials,
                // transaction and history. Only policy/target/history are injected.
                var model = new MainViewModel(new CandidatePolicy(), manifest.TargetPath,
                    new LocalVmxBackupHistory(manifest.HistoryDirectory));
                MainWindow = new MainWindow(model) { Title = "VM Identity Lab · M11 独立副本验收" };
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                MainWindow.Show();
                Console.WriteLine("{\"status\":\"acceptance_window_open\",\"elevated\":false}");
            }
            catch (Exception error)
            {
                Console.Error.WriteLine("acceptance.preflight.failed: " + error.GetType().Name);
                Shutdown(1);
            }
        }
    }

    private static void ValidateManifest(string manifestPath, Manifest manifest)
    {
        if (!Path.IsPathFullyQualified(manifest.TargetPath) || !File.Exists(manifest.TargetPath)
            || !Path.GetExtension(manifest.TargetPath).Equals(".vmx", StringComparison.OrdinalIgnoreCase)
            || !Path.IsPathFullyQualified(manifest.HistoryDirectory)
            || !Path.GetFullPath(manifest.HistoryDirectory).StartsWith(Path.GetDirectoryName(manifestPath)! + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("An explicit target and run-private history are required.");
        foreach (var (name, expected) in new[] { ("vmware.exe", manifest.VmwareSha256), ("vmrest.exe", manifest.VmrestSha256) })
            if (Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(manifest.InstallationDirectory, name))))
                != expected.ToUpperInvariant()) throw new InvalidOperationException("VMware installation changed.");
    }

    private static async Task SelfTestAsync(string folder)
    {
        if (Directory.Exists(folder)) throw new IOException("Use a new self-test directory.");
        Directory.CreateDirectory(folder);
        var source = "# acceptance synthetic only\nvirtualHW.version = \"19\"\nmemsize = \"6144\"\nnumvcpus = \"4\"\ncpuid.coresPerSocket = \"2\"\n"
            + "ethernet0.present = \"TRUE\"\nethernet0.addressType = \"static\"\nethernet0.connectionType = \"hostonly\"\nethernet0.startConnected = \"FALSE\"\n"
            + "ethernet0.address = \"00:50:56:00:00:01\"\nuuid.bios = \"56 4d 11 22 33 44 55 66-77 88 99 aa bb cc dd ee\"\n";
        var path = Path.Combine(folder, "synthetic.vmx");
        var other = Path.Combine(folder, "other.vmx");
        await File.WriteAllTextAsync(path, source, new UTF8Encoding(false));
        await File.WriteAllTextAsync(other, source, new UTF8Encoding(false));
        var checks = new Dictionary<string, bool>();
        using var released = new MainViewModel();
        using var scoped = new MainViewModel(new CandidatePolicy(), path, new LocalVmxBackupHistory(Path.Combine(folder, "history")));
        foreach (var field in new[] { IdentityConfigField.StaticMac, IdentityConfigField.BiosUuid })
        {
            var preview = await VmxIdentityConfigPlanner.PrepareAsync(path, Version, field);
            checks[field + ".verified_profile_released"] = released.IsPreviewWriteApproved(preview);
            checks[field + ".scoped_candidate_allowed"] = scoped.IsPreviewWriteApproved(preview);
            var wrong = await VmxIdentityConfigPlanner.PrepareAsync(other, Version, field);
            checks[field + ".other_target_denied"] = !scoped.IsPreviewWriteApproved(wrong);
            var version = await VmxIdentityConfigPlanner.PrepareAsync(path, new Version(17, 5, 2), field);
            checks[field + ".incomplete_version_denied"] = !scoped.IsPreviewWriteApproved(version);
        }
        foreach (var english in new[] { false, true })
        {
            scoped.ChangeLanguage(english);
            checks["resources." + english] = !scoped.IdentityConfigScope.StartsWith("IdentityConfig.", StringComparison.Ordinal);
            checks["default_notice." + english] = released.IdentityConfigScope != scoped.IdentityConfigScope;
        }
        checks["files_unchanged"] = await File.ReadAllTextAsync(path) == source && await File.ReadAllTextAsync(other) == source;
        checks["history_not_created"] = !Directory.Exists(Path.Combine(folder, "history"));
        checks["no_sidecars"] = Directory.GetFiles(folder).Length == 2;
        var passed = checks.Values.All(value => value);
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed, checks }));
        Console.WriteLine(JsonSerializer.Serialize(new { passed, checks = checks.Count }));
        if (!passed) throw new InvalidOperationException("Acceptance host self-test failed.");
    }
}
