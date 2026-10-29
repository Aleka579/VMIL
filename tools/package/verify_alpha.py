"""Verify a stable or alpha product ZIP without executing or extracting its contents."""
import argparse
import hashlib
import json
import pathlib
import re
import zipfile


def valid_version(value):
    number = r"(?:0|[1-9]\d*)"
    return isinstance(value, str) and re.fullmatch(rf"{number}\.{number}\.{number}(?:-alpha\.{number})?", value) is not None


def validate_product_path(name):
    path = pathlib.PurePosixPath(name)
    forbidden = {".pdb", ".vmx", ".vmxf", ".vmdk", ".vmsn", ".vmsd", ".vmem", ".vmss", ".nvram", ".iso", ".log", ".trx", ".bak", ".tmp"}
    development = ("vmidentitylab.desktoptest", "vmidentitylab.acceptance", "vmidentitylab.unittests",
                   "vmidentitylab.integrationtests", "xunit", "microsoft.testing", "microsoft.applicationinsights")
    if (not name or path.is_absolute() or ".." in path.parts or "\\" in name or ":" in name
            or path.as_posix() != name or path.suffix.lower() in forbidden
            or path.name.lower().startswith(development)):
        raise ValueError("Non-product or unsafe package path.")


def verify(package, expected_version=None, expected_source=None):
    with zipfile.ZipFile(package) as archive:
        names = archive.namelist()
        if len(set(names)) != len(names):
            raise ValueError("Duplicate ZIP entries.")
        for name in names:
            validate_product_path(name)
        if archive.testzip() is not None:
            raise ValueError("ZIP CRC verification failed.")
        manifest = json.loads(archive.read("PACKAGE-FILES.json"))
        if set(names) != set(manifest) | {"PACKAGE-FILES.json"} or "PACKAGE-FILES.json" in manifest:
            raise ValueError("Manifest must cover every other file exactly.")
        for name, digest in manifest.items():
            if not isinstance(digest, str) or not re.fullmatch("[a-f0-9]{64}", digest):
                raise ValueError("Invalid manifest hash.")
            if hashlib.sha256(archive.read(name)).hexdigest() != digest:
                raise ValueError("File hash mismatch.")
        required = {"BUILD-INFO.json", "README.md", "LICENSE", "THIRD-PARTY-NOTICES.md",
                    "VMIdentityLab.App.exe", "VMIdentityLab.App.dll", "VMIdentityLab.Core.dll",
                    "VMIdentityLab.Windows.dll", "VMIdentityLab.VMware.dll", "VMIdentityLab.App.deps.json",
                    "VMIdentityLab.App.runtimeconfig.json", "third-party/NETCore-LICENSE.txt",
                    "third-party/NETCore-THIRD-PARTY-NOTICES.txt", "third-party/WindowsDesktop-LICENSE.txt",
                    "third-party/System.Management.nuspec", "third-party/System.CodeDom.nuspec"}
        if not required.issubset(manifest):
            raise ValueError("Required product or license file missing.")
        info = json.loads(archive.read("BUILD-INFO.json").decode("utf-8-sig"))
        if (not valid_version(info["version"])
                or not re.fullmatch("[a-f0-9]{40}", info["sourceCommit"])
                or info.get("sourceState") != "clean" or info.get("selfContained") is not True
                or info.get("runtimeIdentifier") != "win-x64"):
            raise ValueError("Invalid build identity.")
        if expected_version is not None and info["version"] != expected_version:
            raise ValueError("Unexpected package version.")
        if expected_source is not None and info["sourceCommit"] != expected_source:
            raise ValueError("Unexpected source commit.")
        runtime = json.loads(archive.read("VMIdentityLab.App.runtimeconfig.json"))["runtimeOptions"]["includedFrameworks"]
        if runtime != info["includedFrameworks"] or {item["name"] for item in runtime} != {
                "Microsoft.NETCore.App", "Microsoft.WindowsDesktop.App"}:
            raise ValueError("Runtime metadata mismatch.")
        return {"version": info["version"], "sourceCommit": info["sourceCommit"],
                "sdkVersion": info["sdkVersion"], "includedFrameworks": runtime,
                "distribution": info["distribution"], "files": len(names),
                "unpackedBytes": sum(item.file_size for item in archive.infolist()),
                "bytes": package.stat().st_size, "sha256": hashlib.sha256(package.read_bytes()).hexdigest()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--package", type=pathlib.Path, required=True)
    parser.add_argument("--expected-version")
    parser.add_argument("--expected-source")
    args = parser.parse_args()
    result = verify(args.package, args.expected_version, args.expected_source)
    checksum = args.package.with_suffix(args.package.suffix + ".sha256")
    if checksum.read_text(encoding="ascii").strip() != f"{result['sha256']}  {args.package.name}":
        raise ValueError("ZIP checksum file mismatch.")
    print(json.dumps(result))


if __name__ == "__main__":
    main()
