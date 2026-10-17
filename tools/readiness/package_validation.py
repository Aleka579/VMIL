"""Package an already-published application for private, reproducible validation."""
import argparse
import hashlib
import json
import pathlib
import zipfile

parser = argparse.ArgumentParser()
parser.add_argument("--directory", type=pathlib.Path, required=True)
parser.add_argument("--output", type=pathlib.Path, required=True)
parser.add_argument("--baseline", required=True)
args = parser.parse_args()
runtime = json.loads((args.directory / "VMIdentityLab.App.runtimeconfig.json").read_text())
files = [path for path in sorted(args.directory.rglob("*")) if path.is_file()]
assert all(path.suffix.lower() not in {".pdb", ".vmx", ".vmdk", ".iso", ".log"} for path in files)
assert (args.directory / "LICENSE").is_file() and (args.directory / "third-party").is_dir()
manifest = {"purpose": "local validation build, not a release", "sourceBaseline": args.baseline,
            "sourceState": "baseline plus pending v0.1 readiness changes",
            "runtime": runtime["runtimeOptions"]["includedFrameworks"],
            "files": {path.relative_to(args.directory).as_posix(): hashlib.sha256(path.read_bytes()).hexdigest()
                      for path in files}}
args.output.parent.mkdir(parents=True, exist_ok=True)
with zipfile.ZipFile(args.output, "x", zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
    for path in files:
        archive.write(path, path.relative_to(args.directory).as_posix())
    archive.writestr("VALIDATION-BUILD.json", json.dumps(manifest, indent=2))
print(json.dumps({"file": str(args.output), "bytes": args.output.stat().st_size,
                  "sha256": hashlib.sha256(args.output.read_bytes()).hexdigest(),
                  "files": len(files) + 1, "unpackedBytes": sum(path.stat().st_size for path in files)}))
