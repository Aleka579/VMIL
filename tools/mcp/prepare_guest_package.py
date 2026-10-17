"""Package the tested application and UI driver, sharing the same .NET runtime files."""
import argparse
import hashlib
import json
import pathlib
import zipfile

parser = argparse.ArgumentParser()
parser.add_argument("--application", type=pathlib.Path, required=True)
parser.add_argument("--driver", type=pathlib.Path, required=True)
parser.add_argument("--output", type=pathlib.Path, required=True)
parser.add_argument("--source-baseline", required=True)
args = parser.parse_args()
app_runtime = json.loads((args.application / "VMIdentityLab.App.runtimeconfig.json").read_text())
driver_runtime = json.loads((args.driver / "VMIdentityLab.DesktopTest.runtimeconfig.json").read_text())
if app_runtime["runtimeOptions"]["includedFrameworks"] != driver_runtime["runtimeOptions"]["includedFrameworks"]:
    raise RuntimeError("Application and test driver runtime versions differ")
args.output.parent.mkdir(parents=True, exist_ok=True)
manifest = {"purpose": "private guest validation, not a release", "sourceBaseline": args.source_baseline,
            "runtime": app_runtime["runtimeOptions"]["includedFrameworks"], "hashes": {}}
for name in ["VMIdentityLab.App.dll", "VMIdentityLab.Core.dll", "VMIdentityLab.Windows.dll", "VMIdentityLab.VMware.dll"]:
    manifest["hashes"][name] = hashlib.sha256((args.application / name).read_bytes()).hexdigest()
manifest["hashes"]["VMIdentityLab.DesktopTest.dll"] = hashlib.sha256(
    (args.driver / "VMIdentityLab.DesktopTest.dll").read_bytes()).hexdigest()
with zipfile.ZipFile(args.output, "x", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
    for path in sorted(args.application.rglob("*")):
        if path.is_file() and path.suffix.lower() != ".pdb":
            archive.write(path, path.relative_to(args.application).as_posix())
    for suffix in [".exe", ".dll", ".deps.json", ".runtimeconfig.json"]:
        path = args.driver / ("VMIdentityLab.DesktopTest" + suffix)
        archive.write(path, path.name)
    archive.writestr("TEST-PACKAGE.json", json.dumps(manifest, indent=2))
print(json.dumps({"package": str(args.output), "bytes": args.output.stat().st_size,
                  "sha256": hashlib.sha256(args.output.read_bytes()).hexdigest()}))
