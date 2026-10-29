"""Read-only product acceptance through an already-authenticated guest_session queue.

Reuse original archives and an explicit per-run CASE.json. Never manage VM power/configuration.
The separate UI driver shares product runtime files through same-volume NTFS hard links.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import time
import xml.etree.ElementTree as ET
import zipfile


def digest(path):
    with Path(path).open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def quote(value):
    return "'" + str(value).replace("'", "''") + "'"


class Acceptance:
    def __init__(self, folder):
        self.here = Path(folder).resolve()
        self.root = Path(__file__).resolve().parents[2]
        self.config = json.loads((self.here / "CASE.json").read_text(encoding="utf-8"))
        self.version = self.config["version"]
        self.file_count = self.config["productFiles"]
        self.guest = self.config["guestRoot"]
        if not re.fullmatch(r"C:\\Users\\Public\\Documents\\VMIL-[A-Za-z0-9-]+", self.guest):
            raise ValueError("Expected a new, owned Public Documents test directory")
        self.session = self.here / self.config["session"]
        self.evidence = self.here / "guest-evidence"
        self.ps = r"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe"
        self.remote = self.guest + "\\evidence\\"
        self.app = self.guest + r"\app\VMIdentityLab.App.exe"
        self.driver = self.guest + r"\driver-runtime\VMIdentityLab.DesktopTest.exe"

    def save(self, name, value):
        with (self.here / name).open("x", encoding="utf-8") as stream:
            json.dump(value, stream, ensure_ascii=False, indent=2)

    def checkpoint(self, label, checks):
        target = self.here / "gui-checkpoint.json"
        temporary = target.with_suffix(".writing")
        temporary.write_text(json.dumps({"label": label, "checks": checks}, ensure_ascii=False, indent=2), encoding="utf-8")
        temporary.replace(target)

    def submit(self, operation, arguments=()):
        state = json.loads((self.session / "session.json").read_text(encoding="utf-8"))
        if state["status"] != "ready" and operation not in {"copy_from", "file_exists", "dir_exists", "processes", "stop"}:
            raise RuntimeError("Guest session is not ready: " + state["status"])
        number = max((int(p.stem) for p in (self.session / "commands").glob("*.json")), default=0) + 1
        name = f"{number:04d}.json"
        command, result = self.session / "commands" / name, self.session / "results" / name
        if command.exists() or result.exists():
            raise RuntimeError("Queue entry already exists")
        temporary = command.with_suffix(".writing")
        with temporary.open("x", encoding="utf-8") as stream:
            json.dump({"operation": operation, "arguments": list(map(str, arguments))}, stream, ensure_ascii=False)
        temporary.rename(command)
        deadline = time.monotonic() + 55
        while time.monotonic() < deadline:
            if result.exists():
                value = json.loads(result.read_text(encoding="utf-8"))
                print(json.dumps({"command": name, "operation": operation, "exitCode": value["exitCode"]}), flush=True)
                if value["exitCode"] != 0:
                    raise RuntimeError("Guest operation failed; inspect results/" + name)
                return value
            state = json.loads((self.session / "session.json").read_text(encoding="utf-8"))
            if operation == "stop" and state["status"] == "stopped":
                return state
            if state["status"] not in {"ready", "operation_failed"}:
                raise RuntimeError("Guest session changed: " + state["status"])
            time.sleep(0.2)
        raise TimeoutError("Unconfirmed operation; do not replay " + name)

    def powershell(self, script, background=False):
        if len(script) > 2400:
            raise ValueError("Keep each guest operation small and reviewable")
        flags = ["-interactive"] + (["-noWait"] if background else [])
        return self.submit("run", [*flags, self.ps, "-NoProfile", "-NonInteractive", "-WindowStyle", "Hidden", "-Command", script])

    def take(self, name):
        if Path(name).name != name:
            raise ValueError("Evidence filename must be a leaf")
        self.evidence.mkdir(exist_ok=True)
        target = self.evidence / name
        if target.exists():
            raise FileExistsError(target)
        self.submit("copy_from", [self.remote + name, target])
        return target

    def read(self, name):
        return json.loads((self.evidence / name).read_text(encoding="utf-8-sig"))

    def wait_file(self, name):
        path = quote(self.remote + name)
        self.powershell("$ErrorActionPreference='Stop'; for($i=0;$i -lt 40 -and -not (Test-Path -LiteralPath "
                        + path + ");$i++){Start-Sleep -Milliseconds 250}; if(-not(Test-Path -LiteralPath " + path + ")){throw 'Evidence file not ready'}")

    def run_program(self, name, program, arguments, expected=0):
        self.powershell("$ErrorActionPreference='Stop'; $p=Start-Process -FilePath " + quote(program)
                        + " -ArgumentList " + quote(arguments) + " -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput "
                        + quote(self.remote + name + ".stdout") + " -RedirectStandardError " + quote(self.remote + name + ".stderr")
                        + "; $p.ExitCode | Set-Content -LiteralPath " + quote(self.remote + name + ".exit")
                        + "; if($p.ExitCode -ne " + str(expected) + "){throw 'Unexpected program exit code'}")

    def finish_checks(self, name, checks, **metadata):
        result = {"passed": all(checks.values()), "checks": checks, "source": self.config["source"], **metadata}
        self.save(name, result)
        print(json.dumps({"passed": result["passed"], "checks": len(checks), "failed": [k for k, v in checks.items() if not v]}), flush=True)
        if not result["passed"]:
            raise RuntimeError("Acceptance checks failed; retain original evidence")

    def prepare(self):
        archives = {}
        for key in ("app", "driver"):
            path = Path(self.config[key + "Archive"])
            if digest(path) != self.config[key + "Sha256"]:
                raise ValueError(key + " archive does not match its frozen SHA256")
            archives[key] = {"path": str(path), "bytes": path.stat().st_size, "sha256": digest(path)}
        with zipfile.ZipFile(archives["app"]["path"]) as archive:
            if archive.testzip() is not None:
                raise ValueError("Product archive CRC failure")
            expected = {i.filename: hashlib.sha256(archive.read(i)).hexdigest() for i in archive.infolist() if not i.is_dir()}
            info = json.loads(archive.read("BUILD-INFO.json"))
            if len(expected) != self.file_count or info["sourceCommit"] != self.config["source"] or info["version"] != self.version:
                raise ValueError("Unexpected original product identity")
        target = self.here / "synthetic-inputs.zip"
        fixtures = Path(self.config["fixtures"])
        with zipfile.ZipFile(target, "x", zipfile.ZIP_DEFLATED) as archive:
            archive.writestr("expected-product-hashes.json", json.dumps(expected, indent=2))
            for path in sorted(fixtures.rglob("*")):
                if path.is_file():
                    relative = path.relative_to(fixtures)
                    data = path.read_bytes()
                    if path.name == "record-paths.json":
                        paths = json.loads(data)
                        paths = {k: self.guest + "\\inputs\\validation-fixtures\\" + str(relative.parent) + "\\history\\" + Path(v).name for k, v in paths.items()}
                        data = json.dumps(paths, indent=2).encode("utf-8")
                    archive.writestr("validation-fixtures/" + relative.as_posix(), data)
        archives["inputs"] = {"path": str(target), "bytes": target.stat().st_size, "sha256": digest(target)}
        self.save("inputs-check.json", {"passed": True, "archives": archives, "source": self.config["source"], "version": self.version, "productFiles": self.file_count, "originalProductNotRepacked": True})
        print(json.dumps({"prepared": True, "files": self.file_count, "syntheticInputBytes": target.stat().st_size}))

    def deploy(self):
        archives = json.loads((self.here / "inputs-check.json").read_text())["archives"]
        self.submit("run", ["-interactive", r"C:\Windows\System32\whoami.exe"])
        self.powershell("$ErrorActionPreference='Stop'; if((Get-PSDrive C).Free -lt 1GB){throw 'Insufficient test space'}; if(Test-Path -LiteralPath " + quote(self.guest)
                        + "){throw 'Test directory already exists'}; New-Item -ItemType Directory -Path " + quote(self.remote) + " | Out-Null")
        for key, value in archives.items():
            target = self.guest + "\\" + key + ".zip"
            self.submit("copy_to", [value["path"], target])
            self.powershell("$ErrorActionPreference='Stop'; if((Get-FileHash -Algorithm SHA256 -LiteralPath " + quote(target) + ").Hash -ne " + quote(value["sha256"]) + "){throw 'Copied archive hash mismatch'}")
        self.powershell("$ErrorActionPreference='Stop'; Expand-Archive -LiteralPath " + quote(self.guest + "\\app.zip") + " -DestinationPath " + quote(self.guest + "\\app")
                        + "; Expand-Archive -LiteralPath " + quote(self.guest + "\\inputs.zip") + " -DestinationPath " + quote(self.guest + "\\inputs"))
        self.powershell("$ErrorActionPreference='Stop'; $base=" + quote(self.guest + "\\app") + "; $shared=" + quote(self.guest + "\\driver-runtime")
                        + "; New-Item -ItemType Directory -Path $shared | Out-Null; foreach($f in Get-ChildItem -LiteralPath $base -Recurse -File){$p=Join-Path $shared $f.FullName.Substring($base.Length+1); [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($p)) | Out-Null; New-Item -ItemType HardLink -Path $p -Value $f.FullName | Out-Null}; Expand-Archive -LiteralPath "
                        + quote(self.guest + "\\driver.zip") + " -DestinationPath $shared")
        self.integrity("deployment.json")
        self.take("deployment.json")
        data = self.read("deployment.json")
        with zipfile.ZipFile(self.here / "synthetic-inputs.zip") as archive:
            expected = json.loads(archive.read("expected-product-hashes.json"))
        if data["fileCount"] != self.file_count or data["hashes"] != expected or data["originalZipSha256"] != self.config["appSha256"]:
            raise ValueError("Deployment integrity differs from original archive")
        self.context()

    def context(self):
        # Can continue this independent read-only phase without redeploying completed files.
        self.powershell("$ErrorActionPreference='Stop'; $output=" + quote(self.remote + "system.json")
                        + "; if(Test-Path -LiteralPath $output){throw 'Context output already exists'};"
                        + "$os=Get-CimInstance Win32_OperatingSystem; $cpu=@(Get-CimInstance Win32_Processor);"
                        + "$p=[Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent());"
                        + "[pscustomobject]@{Os=$os.Caption;Build=$os.BuildNumber;"
                        + "VisibleMemoryBytes=([uint64]$os.TotalVisibleMemorySize*1024);"
                        + "LogicalProcessors=($cpu | Measure-Object NumberOfLogicalProcessors -Sum).Sum;"
                        + "CpuNames=@($cpu.Name | Select-Object -Unique);ProcessIsX64=[Environment]::Is64BitProcess;"
                        + "Elevated=$p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator);"
                        + "DotnetPaths=@((Test-Path 'C:\\Program Files\\dotnet'),(Test-Path 'C:\\Program Files (x86)\\dotnet'),"
                        + "(Test-Path ($env:USERPROFILE+'\\.dotnet')));"
                        + "DotnetCommand=[bool](Get-Command dotnet -ErrorAction SilentlyContinue);"
                        + "FileSystem=(Get-CimInstance Win32_LogicalDisk | Where-Object DeviceID -eq 'C:').FileSystem;"
                        + "RuntimeLinkType=(Get-Item -LiteralPath " + quote(self.guest + "\\driver-runtime\\coreclr.dll")
                        + ").LinkType} | ConvertTo-Json | Set-Content -Encoding UTF8 -LiteralPath $output")
        self.take("system.json")
        system = self.read("system.json")
        self.finish_checks("deployment-check.json", {"originalProductFiles": True, "originalZip": True,
            "windows11": "Windows 11" in system["Os"] and int(system["Build"]) >= 22000,
            "ordinaryUser": not system["Elevated"], "nativeX64": system["ProcessIsX64"],
            "sharedRuntimeNoFullCopy": system["FileSystem"] == "NTFS" and system["RuntimeLinkType"] == "HardLink"}, system=system)

    def cli(self):
        if not json.loads((self.here / "deployment-check.json").read_text())["passed"]:
            raise RuntimeError("Deployment was not accepted")
        for name, opt_in in (("default", False), ("identifiers", True)):
            self.run_program(name, self.app, "--collect --output " + self.remote + name + ".json --html " + self.remote + name + ".html" + (" --include-identifiers" if opt_in else ""))
            for suffix in (".json", ".html", ".stdout", ".stderr", ".exit"):
                self.take(name + suffix)
            report = self.remote + name + ".json"
            self.powershell("$ErrorActionPreference='Stop'; $before=(Get-FileHash -LiteralPath " + quote(report) + " -Algorithm SHA256).Hash; $p=Start-Process -FilePath " + quote(self.app)
                            + " -ArgumentList " + quote("--collect --output " + report) + " -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput " + quote(self.remote + name + "-existing.stdout")
                            + " -RedirectStandardError " + quote(self.remote + name + "-existing.stderr") + "; [pscustomobject]@{exitCode=$p.ExitCode;before=$before;after=(Get-FileHash -LiteralPath " + quote(report)
                            + " -Algorithm SHA256).Hash} | ConvertTo-Json | Set-Content -Encoding UTF8 -LiteralPath " + quote(self.remote + name + "-existing.json"))
            self.take(name + "-existing.json")
        self.powershell("$ErrorActionPreference='Stop'; $uuid=(Get-CimInstance Win32_ComputerSystemProduct).UUID; $serial=(Get-CimInstance Win32_BIOS).SerialNumber; $macs=@([System.Net.NetworkInformation.NetworkInterface]::GetAllNetworkInterfaces() | ForEach-Object {$_.GetPhysicalAddress().ToString()} | Where-Object {$_}); [pscustomobject]@{UuidAvailable=(-not [string]::IsNullOrWhiteSpace($uuid));SerialAvailable=(-not [string]::IsNullOrWhiteSpace($serial));MacAvailable=($macs.Count -gt 0)} | ConvertTo-Json | Set-Content -Encoding UTF8 -LiteralPath " + quote(self.remote + "identifier-availability.json"))
        self.take("identifier-availability.json")
        for language in ("zh-CN", "en-US"):
            self.run_program(language + "-preview", self.app, "--render-preview --output " + self.remote + language + "-identity.png --language " + language + " --page 1 --preview-tab identity")
            self.run_program(language + "-pixels", self.driver, "verify-preview " + self.remote + language + "-identity.png identity")
            for suffix in ("-identity.png", "-preview.exit", "-pixels.exit", "-pixels.stdout", "-pixels.stderr"):
                self.take(language + suffix)
        system = self.read("system.json")
        checks = {"nativeX64": system["ProcessIsX64"], "identifierAvailability": all(self.read("identifier-availability.json").values())}
        sources = {"smbios.uuid": "Win32_ComputerSystemProduct.UUID", "smbios.serialNumber": "Win32_BIOS.SerialNumber", "network.macAddress": "NetworkInterface.GetAllNetworkInterfaces/GetPhysicalAddress"}
        for name, count in (("default", 14), ("identifiers", 17)):
            report, log, duplicate = self.read(name + ".json"), self.read(name + ".stdout"), self.read(name + "-existing.json")
            fields = {f["id"]: f for f in report["findings"]}
            checks[name + ".version"] = report["appVersion"] == self.version and report["schemaVersion"] == "1.0"
            checks[name + ".count"] = len(fields) == count == log["findingCount"]
            checks[name + ".successfulFindings"] = all(f["status"] == "success" for f in fields.values())
            checks[name + ".ordinaryUser"] = log["status"] == "completed" and not log["elevated"]
            checks[name + ".redacted"] = report["privacyMode"] == "redacted" and all(f["value"]["kind"] == "redacted" for f in fields.values() if f["sensitivity"] == "identifier")
            checks[name + ".cpu"] = fields["cpu.logicalProcessors"]["value"]["number"] == system["LogicalProcessors"] and fields["cpu.model"]["value"]["text"].strip() in [v.strip() for v in system["CpuNames"]]
            checks[name + ".memory"] = fields["system.memoryBytes"]["value"]["number"] == system["VisibleMemoryBytes"]
            checks[name + ".os"] = report["system"]["os"] == system["Os"] and report["system"]["build"] == system["Build"]
            checks[name + ".identifierSources"] = all(key not in fields if count == 14 else fields[key]["source"] == source and fields[key]["value"]["kind"] == "redacted" and all(fields[key]["value"][part] is None for part in ("text", "number", "boolean")) for key, source in sources.items())
            checks[name + ".html"] = self.version in (self.evidence / (name + ".html")).read_text(encoding="utf-8-sig")
            checks[name + ".noOverwrite"] = duplicate["exitCode"] == 2 and duplicate["before"] == duplicate["after"] == digest(self.evidence / (name + ".json")).upper()
            checks[name + ".noStderr"] = not (self.evidence / (name + ".stderr")).read_text(encoding="utf-8-sig").strip()
        for language in ("zh-CN", "en-US"):
            checks[language + ".previewAndPixels"] = all((self.evidence / (language + suffix)).read_text(encoding="utf-8-sig").strip() == "0" for suffix in ("-preview.exit", "-pixels.exit"))
        self.finish_checks("cli-check.json", checks, system=system, freshOsClaim=False)

    def gui(self):
        if not json.loads((self.here / "cli-check.json").read_text())["passed"]:
            raise RuntimeError("CLI acceptance was not completed")
        self.powershell("$ErrorActionPreference='Stop'; $env:VMIDENTITYLAB_TEST_LOG=" + quote(self.remote + "ui-actions.jsonl")
                        + "; $p=Start-Process -FilePath " + quote(self.app) + " -WindowStyle Normal -Wait -PassThru -RedirectStandardOutput "
                        + quote(self.remote + "app.stdout") + " -RedirectStandardError " + quote(self.remote + "app.stderr")
                        + "; [pscustomobject]@{exitCode=$p.ExitCode;hasExitCode=($null -ne $p.ExitCode)} | ConvertTo-Json | Set-Content -Encoding UTF8 -LiteralPath " + quote(self.remote + "app-exit.json"), background=True)
        self.powershell("$ErrorActionPreference='Stop'; Add-Type -AssemblyName System.Windows.Forms; $found=@(); for($i=0;$i -lt 40 -and $found.Count -eq 0;$i++){$found=@(Get-Process -Name VMIdentityLab.App -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq "
                        + quote(self.app) + "}); if($found.Count -eq 0){Start-Sleep -Milliseconds 250}}; if($found.Count -ne 1){throw 'Expected one owned App'}; $p=[Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent()); [pscustomobject]@{pid=$found[0].Id;elevated=$p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator);desktopWidth=[System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea.Width;desktopHeight=[System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea.Height} | ConvertTo-Json | Set-Content -Encoding UTF8 -LiteralPath " + quote(self.remote + "app-process.json"))
        self.take("app-process.json")
        process = self.read("app-process.json")
        pid, counter = str(process["pid"]), 0
        checks = {"ordinaryUser": not process["elevated"], "completedAllStages": False}
        if not checks["ordinaryUser"]:
            raise RuntimeError("App must run as an ordinary user")

        def call(action, *args):
            return self.powershell("$env:VMIDENTITYLAB_TEST_LOG=" + quote(self.remote + "ui-actions.jsonl") + "; & " + quote(self.driver) + " " + " ".join(quote(v) for v in (action, pid, *args)) + "; exit $LASTEXITCODE")

        def snapshot(label):
            nonlocal counter
            counter += 1
            name = f"ui-{counter:03d}-{label}.json"
            call("inspect", self.remote + name)
            self.take(name)
            self.checkpoint(label, checks)
            return self.read(name)

        def visible_button(name):
            for percent in (0, 30, 60, 90, 100):
                call("scroll", "ValidationScroll", percent)
                tree = snapshot("locate-button")
                if any(i["name"] == name and i["type"] == "ControlType.Button" and not i["offscreen"] and i["enabled"] for i in tree):
                    return
            raise RuntimeError("Expected validation button is not visible")

        def import_input(button, path):
            if not str(path).startswith(self.guest + "\\inputs\\validation-fixtures\\"):
                raise ValueError("Only this run's synthetic fixture is permitted")
            visible_button(button)
            call("invoke", button)
            call("wait-dialog", "1148")
            call("typed-value", "1148", path)
            call("native-command", "1")
            call("wait-id", "ValidationFieldSelector")

        def export(button, name):
            visible_button(button)
            call("invoke", button)
            call("wait-dialog", "1001")
            call("typed-value", "1001", self.remote + name)
            call("native-command", "1")
            call("wait-ready", button)
            return self.take(name)

        call("wait-id", "LanguageSelector")
        self.powershell("$ErrorActionPreference='Stop'; $p=Get-Process -Id " + pid + "; [pscustomobject]@{modules=@($p.Modules | Where-Object {$_.ModuleName -in @('hostfxr.dll','hostpolicy.dll','coreclr.dll','clrjit.dll')} | Select-Object ModuleName,FileName,@{Name='Version';Expression={(Get-Item -LiteralPath $_.FileName).VersionInfo.ProductVersion}},@{Name='Hash';Expression={(Get-FileHash -Algorithm SHA256 -LiteralPath $_.FileName).Hash.ToLowerInvariant()}});appVersion=(Get-Item -LiteralPath " + quote(self.guest + "\\app\\VMIdentityLab.App.dll") + ").VersionInfo.ProductVersion} | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 -LiteralPath " + quote(self.remote + "loaded-runtime.json"))
        self.take("loaded-runtime.json")
        runtime = self.read("loaded-runtime.json")
        with zipfile.ZipFile(self.here / "synthetic-inputs.zip") as archive:
            expected_hashes = json.loads(archive.read("expected-product-hashes.json"))
        checks["selfContainedRuntimeLoaded"] = len(runtime["modules"]) == 4 and all(i["FileName"].lower().startswith((self.guest + "\\app\\").lower()) and i["Hash"] == expected_hashes[i["ModuleName"]] for i in runtime["modules"])
        checks["compiledVersionAndSource"] = runtime["appVersion"] == self.version + "+" + self.config["source"]
        words = self.words("zh-CN")
        call("select-option", "LanguageSelector", "简体中文")
        call("select", words["Nav.Detection"])
        call("select", words["Identity.Observations"])
        call("invoke", words["Action.Collect"])
        call("wait-ready", words["Action.Collect"])
        checks["actualGuiDefault14"] = next(i["gridRows"] for i in snapshot("actual-default-collection") if i["id"] == "FindingsTable") == 14
        call("check", "IncludeIdentifiersCheckBox")
        call("invoke", words["Action.Collect"])
        call("wait-ready", words["Action.Collect"])
        checks["actualGuiOptIn17"] = next(i["gridRows"] for i in snapshot("actual-optin-collection") if i["id"] == "FindingsTable") == 17
        call("select", words["Nav.Reports"])
        for key, name in (("Action.ExportJson", "ui-collected.json"), ("Action.ExportHtml", "ui-collected.html")):
            call("invoke", words[key])
            call("wait-dialog", "1001")
            call("typed-value", "1001", self.remote + name)
            call("native-command", "1")
            call("wait-ready", words[key])
            self.take(name)
        report = self.read("ui-collected.json")
        checks["actualGuiReportVersionAndCount"] = report["appVersion"] == self.version and report["schemaVersion"] == "1.0" and len(report["findings"]) == 17
        checks["actualGuiReportRedacted"] = report["privacyMode"] == "redacted" and all(i["value"]["kind"] == "redacted" for i in report["findings"] if i["sensitivity"] == "identifier")
        checks["actualGuiHtmlVersion"] = self.version in (self.evidence / "ui-collected.html").read_text(encoding="utf-8-sig")
        for language, option, field, field_id in (("zh-CN", "简体中文", "StaticMac", "staticMac"), ("en-US", "English", "BiosUuid", "biosUuid")):
            words = self.words(language)
            call("select-option", "LanguageSelector", option)
            for key in ("Overview", "Detection", "Configuration", "Comparison", "Reports"):
                call("select", words["Nav." + key])
                tree = snapshot(language + "-" + key)
                checks[language + ".page." + key] = any(i["name"] == words["Nav." + key] for i in tree)
            checks[language + ".versionWording"] = any(i["name"] == words["State.ReadOnly"] for i in tree)
            call("select", words["Nav.Detection"])
            call("select", words["Identity.Title"])
            for suffix, title in (("Mac", "MAC 地址集合（脱敏）" if language == "zh-CN" else "MAC address set (redacted)"), ("Uuid", "UUID（脱敏）" if language == "zh-CN" else "UUID (redacted)")):
                row = None
                for percent in (0, 25, 50, 75, 100):
                    call("scroll", "IdentityTable", percent)
                    tree = snapshot(language + "-identity-" + suffix)
                    matches = [i for i in tree if i["type"] == "ControlType.DataItem" and "Name = " + title + "," in i["name"] and not i["offscreen"]]
                    if len(matches) == 1:
                        row = matches[0]
                        break
                if row is None:
                    raise RuntimeError("Identity row was not realized")
                checks[language + ".identity." + suffix + ".support"] = "Support = " + words["Identity.Support.ExistingLimitedPolicy"] + "," in row["name"]
                call("select", row["name"])
                selected = snapshot(language + "-identity-selected-" + suffix)
                checks[language + ".identity." + suffix + ".basis"] = any(i["name"] == words["Identity.Basis.M11" + suffix + "ExactRange"] and not i["offscreen"] for i in selected)
            call("select", words["Nav.Comparison"])
            call("select", words["Validation.Title"])
            if language == "en-US":
                visible_button(words["Validation.Clear"])
                call("invoke", words["Validation.Clear"])
            call("scroll", "ValidationScroll", "0")
            call("select-option", "ValidationFieldSelector", words["IdentityConfig.Mac" if field == "StaticMac" else "IdentityConfig.Uuid"])
            checks[language + ".emptyHasNoConclusion"] = next(i["gridRows"] for i in snapshot(language + "-empty") if i["id"] == "ValidationChecksTable") == 0
            fixture = self.guest + "\\inputs\\validation-fixtures\\" + field + "\\"
            for phase in ("Before", "After", "Restored"):
                import_input(words["Validation.Import" + phase], fixture + phase.lower() + ".json")
            with zipfile.ZipFile(self.here / "synthetic-inputs.zip") as archive:
                records = json.loads(archive.read("validation-fixtures/" + field + "/record-paths.json"))
                raw_values = [i["value"]["text"] for phase in ("before", "after", "restored") for i in json.loads(archive.read("validation-fixtures/" + field + "/" + phase + ".json"))["identities"] if i["value"]["text"]]
            import_input(words["Validation.ApplyRecord"], records["apply"])
            import_input(words["Validation.RestoreRecord"], records["restore"])
            tree = snapshot(language + "-complete")
            checks[language + ".rows"] = next(i["gridRows"] for i in tree if i["id"] == "ValidationChecksTable") == 23
            checks[language + ".programRows"] = next(i["gridRows"] for i in tree if i["id"] == "ValidationProgramTable") == 3
            exported = export(words["Validation.ExportJson"], language + "-validation.json"), export(words["Validation.ExportHtml"], language + "-validation.html")
            data = json.loads(exported[0].read_text(encoding="utf-8-sig"))
            checks[language + ".schemaAndVersion"] = data["schemaVersion"] == "vmil.identity-validation/1" and data["toolVersion"] == self.version and data["dataKind"] == "synthetic" and data["field"] == field_id
            checks[language + ".configuration"] = len(data["configuration"]) == 3 and all(i["state"] == "passed" for i in data["configuration"])
            checks[language + ".guest"] = len(data["guest"]) == 20 and all(i["state"] == "passed" for i in data["guest"] if i["check"] != "networkAvailability")
            checks[language + ".networkUnverified"] = next(i["state"] for i in data["guest"] if i["check"] == "networkAvailability") == "notProvided"
            checks[language + ".vmwareEvidenceRetained"] = len(data["program"]) == 3 and all(i["vmwareGuestEvidence"] == "consistent" for i in data["program"])
            forbidden = raw_values + ["targetPath", "backupPath", "sessionId", "beforeIdentity", "afterIdentity", str(self.here), self.guest]
            checks[language + ".redactedExports"] = all(not any(v.lower() in path.read_text(encoding="utf-8-sig").lower() for v in forbidden) for path in exported)
            checks[language + ".uiValuesHidden"] = not any(v.lower() in json.dumps(tree).lower() for v in raw_values)
            self.checkpoint(language + " completed", checks)
            print(json.dumps({"language": language, "completed": True}), flush=True)
        call("close")
        self.wait_file("app-exit.json")
        for name in ("app-exit.json", "app.stdout", "app.stderr", "ui-actions.jsonl"):
            self.take(name)
        exit_data = self.read("app-exit.json")
        checks["normalExit"] = exit_data["hasExitCode"] and exit_data["exitCode"] == 0
        checks["completedAllStages"] = True
        self.finish_checks("gui-check.json", checks, process=process, appExitCode=exit_data["exitCode"])

    def words(self, language):
        tree = ET.parse(self.root / f"src/VMIdentityLab.App/Resources/Strings.{language}.xaml")
        return {i.attrib["{http://schemas.microsoft.com/winfx/2006/xaml}Key"]: i.text for i in tree.getroot()}

    def integrity(self, name):
        self.powershell("$ErrorActionPreference='Stop'; $base=" + quote(self.guest + "\\app") + "; $expected=Get-Content -Raw -LiteralPath " + quote(self.guest + "\\inputs\\expected-product-hashes.json")
                        + " | ConvertFrom-Json; $hashes=[ordered]@{}; foreach($entry in $expected.PSObject.Properties){$hashes[$entry.Name]=(Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $base $entry.Name)).Hash.ToLowerInvariant()}; [pscustomobject]@{fileCount=@(Get-ChildItem -LiteralPath $base -Recurse -File).Count;hashes=$hashes;originalZipSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath "
                        + quote(self.guest + "\\app.zip") + ").Hash.ToLowerInvariant()} | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 -LiteralPath " + quote(self.remote + name))

    def finalize(self):
        for name in ("cli-check.json", "gui-check.json"):
            if not json.loads((self.here / name).read_text())["passed"]:
                raise RuntimeError("Acceptance is incomplete")
        self.integrity("posttest-integrity.json")
        self.take("posttest-integrity.json")
        data = self.read("posttest-integrity.json")
        with zipfile.ZipFile(self.here / "synthetic-inputs.zip") as archive:
            expected = json.loads(archive.read("expected-product-hashes.json"))
        if data["fileCount"] != self.file_count or data["hashes"] != expected or data["originalZipSha256"] != self.config["appSha256"] or digest(self.config["appArchive"]) != self.config["appSha256"]:
            raise ValueError("Post-test product integrity failed")
        self.powershell("$ErrorActionPreference='Stop'; Compress-Archive -LiteralPath " + quote(self.remote.rstrip("\\")) + " -DestinationPath " + quote(self.guest + "\\evidence-final.zip")
                        + "; (Get-FileHash -Algorithm SHA256 -LiteralPath " + quote(self.guest + "\\evidence-final.zip") + ").Hash | Set-Content -Encoding UTF8 -LiteralPath " + quote(self.guest + "\\evidence-final.zip.sha256"))
        archive_path, hash_path = self.here / "guest-evidence-final.zip", self.here / "guest-evidence-final.zip.sha256"
        if archive_path.exists() or hash_path.exists():
            raise FileExistsError("Final archive already exists")
        self.submit("copy_from", [self.guest + "\\evidence-final.zip", archive_path])
        self.submit("copy_from", [self.guest + "\\evidence-final.zip.sha256", hash_path])
        if digest(archive_path) != hash_path.read_text(encoding="utf-8-sig").strip().lower():
            raise ValueError("Final evidence archive hash differs")
        with zipfile.ZipFile(archive_path) as archive:
            if archive.testzip() is not None:
                raise ValueError("Final evidence CRC failed")
            archived = {Path(i.filename.replace("\\", "/")).name: archive.read(i) for i in archive.infolist() if not i.is_dir()}
            retrieved = list(self.evidence.iterdir())
            for path in retrieved:
                if path.is_symlink() or not path.is_file() or path.name not in archived or path.read_bytes() != archived[path.name]:
                    raise ValueError("Retrieved evidence does not match final archive")
        self.save("posttest-check.json", {"passed": True, "originalProductFilesUnchanged": self.file_count, "archiveSha256": digest(archive_path), "finalEvidenceFiles": len(archived), "retrievedFilesMatched": len(retrieved)})
        process = json.loads((self.here / "gui-check.json").read_text())["process"]
        self.powershell("$ErrorActionPreference='Stop'; $target=" + quote(self.guest) + "; if([IO.Path]::GetFullPath($target) -ne " + quote(self.guest)
                        + "){throw 'Wrong cleanup target'}; if(Get-Process -Id " + str(process["pid"]) + " -ErrorAction SilentlyContinue){throw 'Owned App is still running'}; if((Get-Item -LiteralPath $target).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Reparse target'}; if(@(Get-ChildItem -LiteralPath $target -Recurse -Force | Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}).Count){throw 'Reparse child'}; Remove-Item -LiteralPath $target -Recurse; if(Test-Path -LiteralPath $target){throw 'Cleanup incomplete'}")
        self.submit("stop")
        # All loose files are this run's fresh downloads, verified byte-for-byte above.
        if self.evidence.resolve().parent != self.here or self.evidence.is_symlink() or self.evidence.is_junction():
            raise ValueError("Unexpected local evidence path")
        for path in retrieved:
            path.unlink()
        self.evidence.rmdir()
        self.save("cleanup-check.json", {"passed": True, "ownedGuestDirectoryRemoved": True, "credentialSessionStopped": True,
            "looseEvidenceDuplicatesRemoved": len(retrieved), "finalArchivePreserved": True, "vmPowerOperations": 0, "vmConfigurationOperations": 0})
        print(json.dumps({"completed": True, "evidenceFiles": len(archived), "duplicateDownloadsRemoved": len(retrieved)}))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--case", required=True, type=Path)
    parser.add_argument("stage", choices=("prepare", "deploy", "context", "cli", "gui", "finalize"))
    arguments = parser.parse_args()
    getattr(Acceptance(arguments.case), arguments.stage)()
