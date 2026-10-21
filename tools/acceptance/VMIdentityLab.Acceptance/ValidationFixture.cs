using System.IO;
using System.Text;
using System.Text.Json;
using VMIdentityLab.App.Infrastructure;
using VMIdentityLab.App.ViewModels;
using VMIdentityLab.Core.Comparison;
using VMIdentityLab.Core.Reports;
using VMIdentityLab.VMware.Configuration;

namespace VMIdentityLab.Acceptance;

// Entirely synthetic. Does not construct a native collector or call VMware.
internal static class ValidationFixture
{
    internal static async Task WriteAsync(string folder)
    {
        if (Directory.Exists(folder)) throw new IOException("Use a new synthetic fixture directory.");
        Directory.CreateDirectory(folder);
        var checks = new Dictionary<string, bool>();
        var session = Guid.NewGuid();
        var time = DateTimeOffset.Parse("2026-10-21T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        const string mac = "00:50:56:00:00:01", newMac = "00:50:56:00:00:02";
        const string uuid = "56 4d 11 22 33 44 55 66-77 88 99 aa bb cc dd ee", newUuid = "56 4d 88 77 66 55 44 33-22 11 aa bb cc dd ee ff";
        foreach (var field in Enum.GetValues<ValidationField>())
        {
            var root = Path.Combine(folder, field.ToString()); Directory.CreateDirectory(root);
            var settings = new VmxIdentitySettings(mac, uuid, 19, new string('A', 64));
            var changed = field == ValidationField.StaticMac ? settings with { MacAddress = newMac } : settings with { BiosUuid = newUuid };
            VmxBackupReceipt Receipt(VmxIdentitySettings a, VmxIdentitySettings b) => new(Path.Combine(root, "synthetic.vmx"), Path.Combine(root, Guid.NewGuid() + ".bak"),
                new string('B', 64), new string('C', 64), Program.Version) { Backend = VmxApiConfigManager.BackendName, BeforeIdentity = a, AfterIdentity = b };
            var apply = new VmxBackupHistoryEntry(Guid.NewGuid(), time.AddMinutes(1), "apply", Receipt(settings, changed));
            var restore = new VmxBackupHistoryEntry(Guid.NewGuid(), time.AddMinutes(3), "restore", Receipt(changed, settings));
            var historyRoot = Path.Combine(root, "history"); var history = new LocalVmxBackupHistory(historyRoot);
            await history.SaveAsync(apply); await history.SaveAsync(restore);
            await File.WriteAllTextAsync(Path.Combine(root, "record-paths.json"), JsonSerializer.Serialize(new
                { apply = Path.Combine(historyRoot, apply.Id.ToString("N") + ".json"), restore = Path.Combine(historyRoot, restore.Id.ToString("N") + ".json") }));
            var config = VmxIdentityValidation.FromRecords(apply, restore);
            var snapshots = new List<PrivateValidationSnapshot>();
            foreach (var phase in Enum.GetValues<ValidationPhase>())
            {
                var at = time.AddMinutes((int)phase * 2);
                Finding F(string id, FindingValue value) => new() { Id = id, Category = FindingCatalog.ById[id].Category,
                    Source = FindingCatalog.ById[id].Source, ObservedAt = at, Status = CollectionStatus.Success, Value = value };
                var report = ReportPrivacy.Apply(new() { CollectedAt = at, System = new() { Os = "Windows SYNTHETIC", Build = "00000", Architecture = "X64" },
                    Findings = FindingCatalog.ById.Values.Where(item => item.Sensitivity == DataSensitivity.Public).Select(item => F(item.Id, item.Id switch
                    { "cpu.logicalProcessors" => FindingValue.FromNumber(4, ValueUnit.Count), "system.memoryBytes" => FindingValue.FromNumber(6442450944m, ValueUnit.Bytes),
                        "cpu.hypervisorPresent" or "tools.installed" => FindingValue.FromBoolean(true), "pnp.vmwareDeviceCount" => FindingValue.FromNumber(1, ValueUnit.Count),
                        "smbios.manufacturer" or "cpu.hypervisorVendor" => FindingValue.FromText("VMware"), _ => FindingValue.FromText("synthetic") })).ToArray() });
                var snapshot = new PrivateValidationSnapshot { SessionId = session, Phase = phase, DataKind = ValidationDataKind.Synthetic, Elevated = false, Report = report,
                    Identities = [F("network.macAddress", FindingValue.FromText(phase == ValidationPhase.After ? config.AfterMac : config.BeforeMac)),
                        F("smbios.uuid", FindingValue.FromText(phase == ValidationPhase.After ? config.AfterUuid : config.BeforeUuid))] };
                var name = phase.ToString().ToLowerInvariant();
                await ValidationSnapshotJson.SavePrivateAsync(Path.Combine(root, name + ".json"), snapshot);
                await ReportFiles.SaveJsonAsync(Path.Combine(root, "legacy-" + name + ".json"), report);
                snapshots.Add(snapshot);
            }
            var result = IdentityValidationComparer.Compare(field, snapshots[0], snapshots[1], snapshots[2], config);
            checks[field + ".records"] = result.Configuration.All(item => item.State == ValidationState.Passed);
            checks[field + ".guest"] = result.Guest.Where(item => item.Check != "networkAvailability").All(item => item.State == ValidationState.Passed);
            checks[field + ".program_unchanged"] = result.Program.All(item => item.VmwareGuestEvidence == "consistent");
            var wrong = snapshots[1] with { Identities = snapshots[1].Identities.Select(item => item.Id == "network.macAddress" ? item with { Source = "unverified" } : item).ToArray() };
            await ValidationSnapshotJson.SavePrivateAsync(Path.Combine(root, "wrong-source-after.json"), wrong);
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "invalid.json"), "{\"schemaVersion\":\"unsupported\"}", new UTF8Encoding(false));
        using var model = new MainViewModel();
        checks["empty_export_disabled"] = !model.ExportValidationJsonCommand.CanExecute(null) && !model.ExportValidationHtmlCommand.CanExecute(null);
        foreach (var english in new[] { false, true })
        {
            model.ChangeLanguage(english);
            foreach (var key in Enum.GetNames<ValidationReason>().Select(value => "Validation.Reason." + value)
                .Concat(Enum.GetNames<ValidationState>().Select(value => "Validation.State." + value))
                .Concat(Enum.GetNames<ValidationPhase>().Select(value => "Validation.Phase." + value))
                .Concat(Enum.GetNames<ValidationDataKind>().Select(value => "Validation.Data." + value))
                .Concat(Enum.GetNames<IdentityWriteSupport>().Select(value => "Identity.Support." + value))
                .Concat(Enum.GetNames<IdentityBasis>().Select(value => "Identity.Basis." + value))) checks[english + "." + key] = Localization.Text(key) != key;
        }
        var passed = checks.Values.All(value => value);
        await File.WriteAllTextAsync(Path.Combine(folder, "fixture-check.json"), JsonSerializer.Serialize(new { passed, checks }));
        Console.WriteLine(JsonSerializer.Serialize(new { passed, checks = checks.Count }));
        if (!passed) throw new InvalidOperationException("Synthetic validation fixture checks failed.");
    }
}
