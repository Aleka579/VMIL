"""Offline checks for native credential staging, vmrun arguments and secret redaction."""
import json
from pathlib import Path
import subprocess
import sys
import tempfile
from types import SimpleNamespace
import unittest
from unittest import mock

import guest_session


class GuestSessionTests(unittest.TestCase):
    VM_SECRET = "synthetic-vm-password"
    GUEST_SECRET = "synthetic-guest-password"

    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="vmil-guest-session-test-")
        self.addCleanup(temporary.cleanup)
        self.folder = Path(temporary.name)
        self.commands = []
        self.prompt_states = []

    def command(self, name, operation, arguments=()):
        folder = self.folder / "commands"
        folder.mkdir(exist_ok=True)
        (folder / name).write_text(json.dumps({"operation": operation, "arguments": list(arguments)}), encoding="utf-8")

    def run_session(self, encrypted=True, credentials=None, process=None):
        if credentials is None:
            credentials = ([("ignored", self.VM_SECRET, False)] if encrypted else [])
            credentials += [("guest", self.GUEST_SECRET, False)]
        credentials = iter(credentials)

        def prompt(*args, **kwargs):
            self.prompt_states.append(json.loads((self.folder / "session.json").read_text())["status"])
            self.assertFalse(kwargs["Save"])
            self.assertTrue(kwargs["Flags"] & guest_session.win32cred.CREDUI_FLAGS_DO_NOT_PERSIST)
            item = next(credentials)
            if isinstance(item, Exception):
                raise item
            return item

        def execute(command, **kwargs):
            self.commands.append(command)
            self.assertEqual(kwargs["stdin"], subprocess.DEVNULL)
            if process:
                return process(command)
            return SimpleNamespace(returncode=0, stdout=b"", stderr=b"")

        arguments = ["guest_session.py", "--vmrun", r"C:\tools\vmrun.exe", "--vmx", r"C:\vm\test.vmx",
                     "--evidence", str(self.folder)]
        if encrypted:
            arguments.append("--prompt-vm-password")
        with mock.patch.object(sys, "argv", arguments), \
             mock.patch.object(guest_session.win32cred, "CredUIPromptForCredentials", side_effect=prompt), \
             mock.patch.object(guest_session.subprocess, "run", side_effect=execute):
            result = guest_session.main()
        for path in self.folder.rglob("*.json"):
            text = path.read_text(encoding="utf-8")
            self.assertNotIn(self.VM_SECRET, text)
            self.assertNotIn(self.GUEST_SECRET, text)
        return result, json.loads((self.folder / "session.json").read_text())

    def test_unencrypted_flow_keeps_single_guest_prompt(self):
        self.command("0001.json", "stop")
        result, state = self.run_session(encrypted=False)
        self.assertEqual((result, state["status"]), (0, "stopped"))
        self.assertEqual(self.prompt_states, ["awaiting_local_credentials"])
        self.assertNotIn("-vp", self.commands[0])
        self.assertEqual(self.commands[0][3:9], ["-gu", "guest", "-gp", self.GUEST_SECRET,
                                                "listProcessesInGuest", r"C:\vm\test.vmx"])

    def test_encrypted_flow_prompts_in_order_and_places_auth_before_operation(self):
        self.command("0001.json", "stop")
        result, state = self.run_session()
        self.assertEqual((result, state["status"]), (0, "stopped"))
        self.assertEqual(self.prompt_states, ["awaiting_vm_credentials", "awaiting_local_credentials"])
        self.assertEqual(self.commands[0][3:11], ["-vp", self.VM_SECRET, "-gu", "guest", "-gp",
                                                 self.GUEST_SECRET, "listProcessesInGuest", r"C:\vm\test.vmx"])

    def test_failed_authentication_redacts_both_secrets(self):
        def fail(_):
            text = f"{self.VM_SECRET} {self.GUEST_SECRET}".encode("ascii")
            return SimpleNamespace(returncode=1, stdout=text, stderr=text)
        result, state = self.run_session(process=fail)
        self.assertEqual((result, state["status"]), (1, "authentication_failed"))
        self.assertEqual(state["stdout"], "[credential] [credential]")
        self.assertEqual(state["stderr"], "[credential] [credential]")

    def test_empty_vm_password_stops_before_guest_prompt(self):
        result, state = self.run_session(credentials=[("ignored", "", False)])
        self.assertEqual((result, state["reason"]), (1, "empty_vm_password"))
        self.assertEqual(self.prompt_states, ["awaiting_vm_credentials"])
        self.assertFalse(self.commands)

    def test_empty_guest_password_never_executes_vmrun(self):
        result, state = self.run_session(credentials=[("ignored", self.VM_SECRET, False), ("guest", "", False)])
        self.assertEqual((result, state["reason"]), (1, "empty_password"))
        self.assertFalse(self.commands)

    def test_timeout_never_persists_credential_bearing_argv(self):
        def timeout(command):
            raise subprocess.TimeoutExpired(command, 45)
        result, state = self.run_session(process=timeout)
        self.assertEqual((result, state["status"], state["error"]), (1, "authentication_failed", "timeout"))

    def test_prompt_exception_records_only_type(self):
        result, state = self.run_session(credentials=[RuntimeError(self.VM_SECRET + self.GUEST_SECRET)])
        self.assertEqual((result, state["status"], state["errorType"]), (1, "failed", "RuntimeError"))
        self.assertFalse(self.commands)

    def test_queued_operation_uses_vm_password_without_persisting_it(self):
        self.command("0001.json", "run", [r"C:\tools\test.exe"])
        self.command("0002.json", "stop")

        def execute(command):
            text = f"{self.VM_SECRET} {self.GUEST_SECRET}".encode("ascii") if "runProgramInGuest" in command else b""
            return SimpleNamespace(returncode=0, stdout=text, stderr=text)

        result, state = self.run_session(process=execute)
        self.assertEqual((result, state["status"]), (0, "stopped"))
        self.assertEqual(len(self.commands), 2)
        self.assertIn("-vp", self.commands[1])
        record = json.loads((self.folder / "results/0001.json").read_text())
        self.assertEqual(record["arguments"], [r"C:\tools\test.exe"])
        self.assertEqual(record["stdout"], "[credential] [credential]")
        self.assertEqual(record["stderr"], "[credential] [credential]")


if __name__ == "__main__":
    unittest.main(verbosity=2)
