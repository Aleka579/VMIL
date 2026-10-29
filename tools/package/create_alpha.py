"""Create a local stable or alpha ZIP from verified build metadata. No deployment."""
import argparse
import hashlib
import json
import pathlib
import shutil
import sys
import zipfile
# Resolve the sibling verifier explicitly when launched in Python isolated mode.
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from verify_alpha import valid_version, validate_product_path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", type=pathlib.Path, required=True)
    parser.add_argument("--output", type=pathlib.Path, required=True)
    args = parser.parse_args()
    root = pathlib.Path(__file__).resolve().parents[2]
    published = args.directory.resolve()
    info = json.loads((published / "BUILD-INFO.json").read_text(encoding="utf-8-sig"))
    if not valid_version(info["version"]) or info.get("sourceState") != "clean":
        raise ValueError("A clean stable or alpha build is required.")
    if len(info["sourceCommit"]) != 40 or any(c not in "0123456789abcdef" for c in info["sourceCommit"]):
        raise ValueError("Invalid build source identity.")
    if args.output.exists() or args.output.with_suffix(args.output.suffix + ".sha256").exists():
        raise FileExistsError("Package or checksum already exists.")
    for source, name in [(root / "LICENSE", "LICENSE"), (root / "THIRD-PARTY-NOTICES.md", "THIRD-PARTY-NOTICES.md"),
                         (root / "docs" / "PACKAGE-README.md", "README.md")]:
        if (published / name).exists():
            raise FileExistsError("Packaging metadata is already present.")
        shutil.copy2(source, published / name)
    notice_dir = published / "third-party"
    notice_dir.mkdir()
    for source in sorted((root / "licenses").iterdir()):
        if source.is_file():
            shutil.copy2(source, notice_dir / source.name)
    files = sorted(path for path in published.rglob("*") if path.is_file())
    for path in files:
        validate_product_path(path.relative_to(published).as_posix())
        if path.is_symlink():
            raise ValueError("Non-product content found in the published directory.")
    manifest_path = published / "PACKAGE-FILES.json"
    if manifest_path.exists():
        raise FileExistsError("Package manifest already exists.")
    manifest = {path.relative_to(published).as_posix(): hashlib.sha256(path.read_bytes()).hexdigest() for path in files}
    with manifest_path.open("x", encoding="utf-8") as stream:
        json.dump(manifest, stream, indent=2)
        stream.write("\n")
    files.append(manifest_path)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(args.output, "x", zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for path in files:
            archive.write(path, path.relative_to(published).as_posix())
    digest = hashlib.sha256(args.output.read_bytes()).hexdigest()
    with args.output.with_suffix(args.output.suffix + ".sha256").open("x", encoding="ascii") as stream:
        stream.write(f"{digest}  {args.output.name}\n")
    print(json.dumps({"version": info["version"], "sourceCommit": info["sourceCommit"], "bytes": args.output.stat().st_size,
                      "sha256": digest, "files": len(files), "unpackedBytes": sum(path.stat().st_size for path in files)}))


if __name__ == "__main__":
    main()
