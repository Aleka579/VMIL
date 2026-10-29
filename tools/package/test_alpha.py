"""Offline product-package contract tests; only synthetic files and temporary ZIPs."""
import contextlib
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch
import warnings
import zipfile

# Explicit sibling imports also work with Python's isolated (-I) mode.
sys.path.insert(0, str(Path(__file__).resolve().parent))
import create_alpha
import verify_alpha


class AlphaTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.published = self.root / "published"
        self.published.mkdir()
        self.output = self.root / "alpha.zip"
        self.info = {"version": "0.3.0-alpha.1", "sourceCommit": "a" * 40, "sourceState": "clean",
                     "runtimeIdentifier": "win-x64", "selfContained": True, "sdkVersion": "10.0.401",
                     "distribution": "synthetic-test", "includedFrameworks": [
                         {"name": "Microsoft.NETCore.App", "version": "10.0.13"},
                         {"name": "Microsoft.WindowsDesktop.App", "version": "10.0.13"}]}
        for name in ["VMIdentityLab.App.exe", "VMIdentityLab.App.dll", "VMIdentityLab.Core.dll",
                     "VMIdentityLab.Windows.dll", "VMIdentityLab.VMware.dll", "VMIdentityLab.App.deps.json"]:
            (self.published / name).write_bytes(b"synthetic-not-executable")
        (self.published / "VMIdentityLab.App.runtimeconfig.json").write_text(json.dumps({"runtimeOptions": {
            "includedFrameworks": self.info["includedFrameworks"]}}), encoding="utf-8")

    def create(self):
        (self.published / "BUILD-INFO.json").write_text(json.dumps(self.info), encoding="utf-8")
        with patch.object(sys, "argv", ["create_alpha", "--directory", str(self.published), "--output", str(self.output)]):
            with contextlib.redirect_stdout(io.StringIO()):
                create_alpha.main()

    def rewrite(self, transform):
        with zipfile.ZipFile(self.output) as archive:
            contents = [(name, archive.read(name)) for name in archive.namelist()]
        altered = self.root / "altered.zip"
        with warnings.catch_warnings():
            warnings.simplefilter("ignore", UserWarning)
            with zipfile.ZipFile(altered, "x") as archive:
                for name, data in transform(contents):
                    archive.writestr(name, data)
        return altered

    def test_round_trip_covers_product_and_licenses(self):
        self.create()
        result = verify_alpha.verify(self.output, self.info["version"], self.info["sourceCommit"])
        self.assertGreater(result["files"], 15)
        with patch.object(sys, "argv", ["verify", "--package", str(self.output)]):
            with contextlib.redirect_stdout(io.StringIO()):
                verify_alpha.main()

    def test_dirty_source_rejected(self):
        self.info["sourceState"] = "dirty"
        with self.assertRaises(ValueError): self.create()

    def test_stable_round_trip(self):
        self.info["version"] = "1.0.0"
        self.create()
        self.assertEqual("1.0.0", verify_alpha.verify(self.output, "1.0.0")["version"])

    def test_invalid_version_rejected(self):
        for version in ("1.0", "01.0.0", "1.0.0-alpha.01", "1.0.0-unknown", "1.0.0+unknown", None):
            with self.subTest(version=version):
                self.info["version"] = version
                with self.assertRaises(ValueError): self.create()

    def test_invalid_source_rejected(self):
        self.info["sourceCommit"] = "unknown"
        with self.assertRaises(ValueError): self.create()

    def test_existing_output_and_checksum_preserved(self):
        for path in [self.output, self.output.with_suffix(".zip.sha256")]:
            with self.subTest(path=path.name):
                path.write_text("existing", encoding="ascii")
                with self.assertRaises(FileExistsError): self.create()
                self.assertEqual("existing", path.read_text())
                path.unlink()

    def test_test_tool_and_vm_content_rejected(self):
        for name in ["VMIdentityLab.Acceptance.exe", "VMIdentityLab.DesktopTest.dll", "xunit.v3.dll", "sample.vmx", "snapshot.vmsd", "trace.trx"]:
            with self.subTest(name=name), self.assertRaises(ValueError): verify_alpha.validate_product_path(name)

    def test_unsafe_paths_rejected(self):
        for name in ["../outside", "/absolute", "C:/absolute", "a\\b", "a/./b"]:
            with self.subTest(name=name), self.assertRaises(ValueError): verify_alpha.validate_product_path(name)

    def test_tampered_file_rejected(self):
        self.create()
        altered = self.rewrite(lambda items: [(n, b"changed" if n == "VMIdentityLab.Core.dll" else b) for n, b in items])
        with self.assertRaises(ValueError): verify_alpha.verify(altered)

    def test_duplicate_zip_entry_rejected(self):
        self.create()
        altered = self.rewrite(lambda items: items + [items[0]])
        with self.assertRaises(ValueError): verify_alpha.verify(altered)

    def test_unlisted_file_rejected(self):
        self.create()
        altered = self.rewrite(lambda items: items + [("unlisted.txt", b"extra")])
        with self.assertRaises(ValueError): verify_alpha.verify(altered)

    def test_missing_license_rejected(self):
        self.create()
        altered = self.rewrite(lambda items: [(n, b) for n, b in items if n != "LICENSE"])
        with self.assertRaises(ValueError): verify_alpha.verify(altered)

    def test_wrong_expected_identity_rejected(self):
        self.create()
        with self.assertRaises(ValueError): verify_alpha.verify(self.output, expected_version="0.2.0-alpha.1")
        with self.assertRaises(ValueError): verify_alpha.verify(self.output, expected_source="b" * 40)

    def test_mismatched_runtime_metadata_rejected(self):
        self.info["includedFrameworks"] = [{"name": "Microsoft.NETCore.App", "version": "10.0.12"}]
        self.create()
        with self.assertRaises(ValueError): verify_alpha.verify(self.output)

    def test_checksum_mismatch_rejected(self):
        self.create()
        self.output.with_suffix(".zip.sha256").write_text("wrong", encoding="ascii")
        with patch.object(sys, "argv", ["verify", "--package", str(self.output)]):
            with self.assertRaises(ValueError): verify_alpha.main()


if __name__ == "__main__":
    unittest.main()
