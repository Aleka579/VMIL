"""Offline fault tests. Fake API only; no credentials or VMware processes."""
import importlib.util
import json
import pathlib
import socket
import sys
import tempfile
import threading
import unittest
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from unittest.mock import patch

SPEC = importlib.util.spec_from_file_location("identity_api_probe", pathlib.Path(__file__).with_name("identity_api_probe.py"))
p = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(p)


class FakeApi:
    def __init__(self, path):
        self.path, self.registered, self.identifier, self.power = path, False, "probe-id", "poweredOff"
        self.puts, self.calls = [], []
        self.after_put = None
        self.ignore_put = False
        self.fail_register = False
        self.duplicate_inventory = False

    def request(self, method, path, body=None):
        self.calls.append((method, path, body))
        if method == "GET" and path == "vms":
            result = [{"id": self.identifier, "path": str(self.path.resolve())}] if self.registered else []
            return result * 2 if self.duplicate_inventory else result
        if method == "POST" and path == "vms/registration":
            if pathlib.Path(body["path"]) != self.path.resolve():
                raise AssertionError("Wrong registration target")
            self.registered = True
            if self.fail_register:
                raise TimeoutError("Synthetic registration response loss")
            return {"id": self.identifier}
        if method == "GET" and path.endswith("/power"):
            return {"power_state": self.power}
        if method == "GET" and "/params/" in path:
            field = path.rsplit("/", 1)[1]
            return {"name": field, "value": p.entries(self.path.read_bytes())[field]}
        if method == "PUT" and path.endswith("/params"):
            self.puts.append(body)
            if not self.ignore_put:
                self.replace(body["name"], body["value"])
            if self.after_put:
                self.after_put(len(self.puts))
            return {"code": 0, "message": "This is not proof of a field change"}
        raise AssertionError(f"Unexpected request: {method} {path}")

    def replace(self, field, value):
        text = self.path.read_text(encoding="utf-8")
        lines = text.splitlines(keepends=True)
        self.path.write_text("".join(f'{field} = "{value}"\n' if line.startswith(field + " = ") else line
                                    for line in lines), encoding="utf-8", newline="\n")


class ProbeTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="vmil-m11-probe-")
        self.addCleanup(self.temporary.cleanup)
        self.folder = p.prepare(pathlib.Path(self.temporary.name))
        self.api = FakeApi(self.folder / "probe.vmx")
        self.distribution = {"installedVersion": p.VERSION, "contractSha256": p.CONTRACT_HASH,
                             "vmrestSha256": "synthetic"}
        self.probe = self.open_probe()

    def open_probe(self):
        return p.Probe(self.folder, self.api, lambda: dict(self.distribution), pathlib.Path.read_bytes)

    def registered(self):
        self.probe.register()
        return self.probe

    def assert_no_put(self, function):
        with self.assertRaises((p.Blocked, KeyError)):
            function()
        self.assertEqual([], self.api.puts)

    def change_baseline(self, old, new):
        data = self.probe.baseline.replace(old, new)
        (self.folder / "baseline.vmx").write_bytes(data)
        (self.folder / "probe.vmx").write_bytes(data)
        meta = self.probe.meta.copy()
        meta["originalHash"] = p.digest(data)
        (self.folder / "fixture.json").write_text(json.dumps(meta), encoding="utf-8")

    def test_prepare_does_not_register_or_contact_api(self):
        self.assertEqual([], self.api.calls)
        self.assertEqual(self.probe.baseline, self.api.path.read_bytes())
        self.assertFalse(any("filename" in key for key in self.probe.original))

    def test_round_trip_only_two_existing_fields_four_puts(self):
        self.registered().run()
        self.assertEqual([p.FIELDS[0], p.FIELDS[0], p.FIELDS[1], p.FIELDS[1]],
                         [item["name"] for item in self.api.puts])
        self.assertEqual(self.probe.baseline, self.api.path.read_bytes())
        self.assertEqual("completed", self.probe.events()[-1]["phase"])
        self.assertTrue(self.probe.events()[-1]["baselineBytesEqual"])
        self.assertFalse(any(method == "DELETE" or (method == "PUT" and path.endswith("/power"))
                             for method, path, _ in self.api.calls))

    def test_replay_after_completion_is_rejected(self):
        self.registered().run()
        with self.assertRaises(p.Blocked):
            self.open_probe().run()
        self.assertEqual(4, len(self.api.puts))

    def test_after_intent_state_mapping_file_and_version_changes_block_dispatch(self):
        for mutation in (lambda: setattr(self.api, "power", "poweredOn"),
                         lambda: setattr(self.api, "identifier", "different-id"),
                         lambda: self.api.replace("memsize", "1024"),
                         lambda: self.distribution.update(installedVersion="17.6.4")):
            with self.subTest(mutation=mutation):
                self.setUp()
                self.registered()
                record = self.probe.record

                def after_intent(phase, **details):
                    record(phase, **details)
                    if phase == "apply-intent":
                        mutation()

                self.probe.record = after_intent
                self.assert_no_put(self.probe.run)
                self.assertEqual("apply-intent", self.probe.events()[-1]["phase"])

    def test_non_off_states_never_send_a_put(self):
        for state in ("poweredOn", "suspended", "unknown", None):
            with self.subTest(state=state):
                self.setUp()
                self.registered()
                self.api.power = state
                self.assert_no_put(self.probe.run)

    def test_changed_file_before_preview_is_rejected(self):
        self.registered()
        self.api.replace("memsize", "2048")
        self.assert_no_put(self.probe.run)

    def test_lock_file_blocks_dispatch(self):
        self.registered()
        (self.folder / "probe.vmx.lck").mkdir()
        self.assert_no_put(self.probe.run)

    def test_duplicate_inventory_is_rejected(self):
        self.registered()
        self.api.duplicate_inventory = True
        self.assert_no_put(self.probe.run)

    def test_api_value_disagrees_with_vmx_before_dispatch(self):
        self.registered()
        request = self.api.request

        def mismatched(method, path, body=None):
            if "/params/" in path:
                return {"name": path.rsplit("/", 1)[1], "value": "different API value"}
            return request(method, path, body)

        self.api.request = mismatched
        self.assert_no_put(self.probe.run)

    def test_corrupt_backup_is_rejected(self):
        (self.folder / "baseline.vmx").write_bytes(b"corrupted")
        self.assert_no_put(self.open_probe)

    def test_generated_address_conversion_disk_and_missing_key_are_rejected(self):
        replacements = ((b'addressType = "static"', b'addressType = "generated"'),
                        (b'config.version = "8"\n', b'config.version = "8"\nscsi0:0.fileName = "user.vmdk"\n'),
                        (b'ethernet0.address = ', b'ethernet0.generatedAddress = '),
                        (b'uuid.bios = ', b'uuid.location = '))
        for old, new in replacements:
            with self.subTest(replacement=new):
                self.setUp()
                self.change_baseline(old, new)
                self.assert_no_put(self.open_probe)

    def test_arbitrary_values_and_fields_are_rejected(self):
        for after in ({p.FIELDS[0]: "08:00:27:12:34:56", p.FIELDS[1]: self.probe.meta["after"][p.FIELDS[1]]},
                      {p.FIELDS[0]: self.probe.meta["after"][p.FIELDS[0]], p.FIELDS[1]: "physical template"},
                      {"uuid.action": "create"}):
            with self.subTest(after=after):
                meta = {**self.probe.meta, "after": after}
                (self.folder / "fixture.json").write_text(json.dumps(meta), encoding="utf-8")
                self.assert_no_put(self.open_probe)

    def test_backup_failure_blocks_every_request(self):
        self.registered()
        self.probe.record = lambda *args, **kwargs: (_ for _ in ()).throw(OSError("disk full"))
        with self.assertRaises(OSError):
            self.probe.run()
        self.assertEqual([], self.api.puts)

    def test_http_200_without_change_is_not_success(self):
        self.registered()
        self.api.ignore_put = True
        with self.assertRaises(p.Blocked):
            self.probe.run()
        self.assertEqual("apply-intent", self.probe.events()[-1]["phase"])
        self.open_probe().restore()
        self.assertEqual(1, len(self.api.puts))
        self.assertEqual("reconciled-original", self.probe.events()[-1]["phase"])

    def lose_first_response(self):
        def loss(count):
            if count == 1:
                raise TimeoutError("Synthetic lost response")
        self.api.after_put = loss
        with self.assertRaises(TimeoutError):
            self.registered().run()

    def test_lost_apply_response_requires_reconciliation_not_replay(self):
        self.lose_first_response()
        with self.assertRaises(p.Blocked):
            self.open_probe().run()
        self.assertEqual(1, len(self.api.puts))
        self.api.after_put = None
        self.open_probe().restore()
        self.assertEqual(self.probe.baseline, self.api.path.read_bytes())
        self.assertEqual(2, len(self.api.puts))

    def test_lost_response_when_not_applied_requires_no_inverse_write(self):
        self.api.ignore_put = True
        self.lose_first_response()
        self.open_probe().restore()
        self.assertEqual(1, len(self.api.puts))

    def test_lost_restore_response_is_not_restored_twice(self):
        def loss(count):
            if count == 2:
                raise TimeoutError()
        self.api.after_put = loss
        with self.assertRaises(TimeoutError):
            self.registered().run()
        self.open_probe().restore()
        self.assertEqual(2, len(self.api.puts))
        self.assertEqual(self.probe.baseline, self.api.path.read_bytes())

    def test_uncertain_unapplied_restore_is_not_repeated(self):
        def lost_restore(count):
            if count == 2:
                self.api.replace(p.FIELDS[0], self.probe.meta["after"][p.FIELDS[0]])
                raise TimeoutError()
        self.api.after_put = lost_restore
        with self.assertRaises(TimeoutError):
            self.registered().run()
        with self.assertRaisesRegex(p.Blocked, "prior_restore_not_confirmed_no_replay"):
            self.open_probe().restore()
        self.assertEqual(2, len(self.api.puts))

    def test_explicit_rejection_is_closed_only_after_independent_unchanged_check(self):
        self.api.ignore_put = True
        self.api.after_put = lambda _: (_ for _ in ()).throw(p.ApiRejected(404, "PUT", "params"))
        with self.assertRaises(p.ApiRejected):
            self.registered().run()
        last = self.probe.events()[-1]
        self.assertEqual("rejected-unchanged", last["phase"])
        self.assertFalse(last["reverse"])
        self.assertEqual(1, len(self.api.puts))
        self.assertEqual(self.probe.baseline, self.api.path.read_bytes())

    def test_rejected_response_with_applied_side_effect_remains_pending(self):
        self.api.after_put = lambda _: (_ for _ in ()).throw(p.ApiRejected(404, "PUT", "params"))
        with self.assertRaises(p.ApiRejected):
            self.registered().run()
        self.assertEqual("apply-intent", self.probe.events()[-1]["phase"])
        self.assertEqual(1, len(self.api.puts))

    def test_uuid_continuation_checks_mac_original_without_reapplying_it(self):
        self.api.ignore_put = True
        self.api.after_put = lambda _: (_ for _ in ()).throw(p.Blocked("legacy_unknown_404"))
        with self.assertRaises(p.Blocked):
            self.registered().run()
        self.api.ignore_put, self.api.after_put = False, None
        self.open_probe().continue_uuid()
        self.assertEqual([p.FIELDS[0], p.FIELDS[1], p.FIELDS[1]], [item["name"] for item in self.api.puts])
        self.assertEqual("uuid-completed", self.probe.events()[-1]["phase"])
        self.assertEqual(self.probe.baseline, self.api.path.read_bytes())

    def test_uuid_continuation_after_proven_rejection_does_not_retry_mac(self):
        self.api.ignore_put = True
        self.api.after_put = lambda _: (_ for _ in ()).throw(p.ApiRejected(404, "PUT", "params"))
        with self.assertRaises(p.ApiRejected):
            self.registered().run()
        self.api.ignore_put, self.api.after_put = False, None
        self.open_probe().continue_uuid()
        self.assertEqual([p.FIELDS[0], p.FIELDS[1], p.FIELDS[1]], [item["name"] for item in self.api.puts])

    def test_uuid_continuation_refuses_external_change_or_fresh_probe(self):
        self.assert_no_put(self.probe.continue_uuid)
        self.lose_first_response()
        self.api.replace("memsize", "2048")
        with self.assertRaises(p.Blocked):
            self.open_probe().continue_uuid()
        self.assertEqual(1, len(self.api.puts))

    def test_guard_404_is_distinguished_from_write_rejection_and_keeps_intent(self):
        for stage in ("before-put", "readback"):
            with self.subTest(stage=stage):
                self.setUp()
                self.registered()
                record, request = self.probe.record, self.api.request
                fail_guard = False

                def record_then_fail(phase, **details):
                    nonlocal fail_guard
                    record(phase, **details)
                    if phase == "apply-intent":
                        fail_guard = stage == "before-put"

                def after_put(_):
                    nonlocal fail_guard
                    fail_guard = True

                def guard_reject(method, path, body=None):
                    if fail_guard and method == "GET" and path.endswith("/power"):
                        raise p.ApiRejected(404, method, "power", "api_error", 1000)
                    return request(method, path, body)

                self.probe.record, self.api.request, self.api.after_put = record_then_fail, guard_reject, after_put
                with self.assertRaises(p.ApiRejected) as raised:
                    self.probe.run()
                self.assertEqual({"method": "GET", "resource": "power", "httpStatus": 404,
                                  "responseKind": "api_error", "vendorCode": 1000, "stage": stage,
                                  "field": p.FIELDS[0], "reverse": False}, raised.exception.diagnostics())
                self.assertEqual("apply-intent", self.probe.events()[-1]["phase"])
                self.assertEqual(0 if stage == "before-put" else 1, len(self.api.puts))

    def test_unknown_value_and_later_unrelated_changes_are_not_overwritten(self):
        for key, value in ((p.FIELDS[0], "unrecognized third value"), ("memsize", "2048")):
            with self.subTest(key=key):
                self.setUp()
                self.lose_first_response()
                self.api.replace(key, value)
                before = self.api.path.read_bytes()
                with self.assertRaises(p.Blocked):
                    self.open_probe().restore()
                self.assertEqual(before, self.api.path.read_bytes())
                self.assertEqual(1, len(self.api.puts))

    def test_known_applied_receipt_rejects_later_byte_changes(self):
        probe = self.registered()
        identifier, current_hash = probe.guard(probe.original)
        probe.change(p.FIELDS[0], False, current_hash, identifier)
        self.api.path.write_bytes(b"\n" + self.api.path.read_bytes())
        with self.assertRaises(p.Blocked):
            self.open_probe().restore()
        self.assertEqual(1, len(self.api.puts))

    def test_register_is_not_retried_after_response_loss(self):
        self.api.fail_register = True
        with self.assertRaises(TimeoutError):
            self.probe.register()
        with self.assertRaises(p.Blocked):
            self.open_probe().register()
        self.open_probe().run()
        self.assertEqual(1, sum(method == "POST" for method, _, _ in self.api.calls))

    def test_native_lease_blocks_a_second_process_handle(self):
        with p.lease(self.folder):
            with self.assertRaises(OSError):
                with p.lease(self.folder):
                    self.fail("Second lease acquired")

    def test_native_lease_exposes_own_handle_for_archiving_locked_byte(self):
        with p.lease(self.folder) as stream:
            stream.seek(0)
            self.assertEqual(b"0", stream.read())
            with self.assertRaises(OSError):
                (self.folder / "lease.lock").read_bytes()
        self.assertEqual(b"0", (self.folder / "lease.lock").read_bytes())

    def test_native_exclusive_read_matches_backup_and_releases_handle(self):
        self.assertEqual(self.probe.baseline, p.read_exclusive(self.api.path))
        self.api.replace("memsize", "1024")
        self.assertNotEqual(self.probe.baseline, p.read_exclusive(self.api.path))

    def test_native_listener_owner_matches_exact_process_image(self):
        with socket.socket() as listener:
            listener.bind(("127.0.0.1", 0))
            listener.listen(1)
            port = listener.getsockname()[1]
            self.assertEqual(p.os.getpid(), p.server_identity(port, pathlib.Path(sys.executable)))
            with self.assertRaisesRegex(p.Blocked, "listener_is_not_reviewed_vmrest"):
                p.server_identity(port, self.api.path)

    def test_installed_build_requires_string_contract_and_binary_hashes(self):
        for raw_version in ("17.5.2.59081", "17.5.2 build-23775572", "17.6.4 build-123"):
            with self.subTest(raw_version=raw_version), patch.object(p, "file_version", return_value=raw_version):
                # No installed VMware or network dependency in this test.
                with patch.object(p.zipfile, "ZipFile") as archive:
                    archive.return_value.__enter__.return_value.read.return_value = b"synthetic contract"
                    with self.assertRaisesRegex(p.Blocked, "unreviewed_installed_build_or_contract"):
                        p.fingerprint(pathlib.Path("synthetic-installation"))

    def test_exact_version_with_changed_binary_is_not_admitted(self):
        contract = b"synthetic contract"
        with (patch.object(p, "file_version", return_value="17.5.2 build-23775571"),
              patch.object(p, "CONTRACT_HASH", p.digest(contract)),
              patch.object(p.zipfile, "ZipFile") as archive,
              patch.object(pathlib.Path, "read_bytes", return_value=b"unreviewed binary")):
            archive.return_value.__enter__.return_value.read.return_value = contract
            with self.assertRaisesRegex(p.Blocked, "unreviewed_distribution_binaries"):
                p.fingerprint(pathlib.Path("synthetic-installation"))


class TransportTests(unittest.TestCase):
    def test_empty_success_is_allowed_only_for_parameter_put_not_reads_or_malformed_json(self):
        with patch.object(p.http.client, "HTTPConnection") as factory:
            response = factory.return_value.getresponse.return_value
            response.status, response.read.return_value = 200, b""
            api = p.Api(8697, "user", "password")
            self.assertIsNone(api.request("PUT", "vms/test-vm/params", {}))
            self.assertEqual({"httpStatus": 200, "bodyBytes": 0, "bodyKind": "empty"}, api.last_response)
            for method, path in (("GET", "vms"), ("GET", "vms/test-vm/power"),
                                 ("GET", "vms/test-vm/params/uuid.bios"), ("POST", "vms/registration")):
                with self.subTest(method=method, path=path), self.assertRaises(p.json.JSONDecodeError):
                    # Status 201 is still required for registration.
                    response.status = 201 if method == "POST" else 200
                    api.request(method, path, {} if method == "POST" else None)
            response.status, response.read.return_value = 200, b"malformed non-empty reply"
            with self.assertRaises(p.json.JSONDecodeError):
                api.request("PUT", "vms/test-vm/params", {})

    def test_duplicate_json_and_oversize_input_are_rejected(self):
        for data in (b'{"name":"x","name":"y"}', b" " * (p.LIMIT + 1)):
            with self.subTest(size=len(data)), self.assertRaises(p.Blocked):
                p.decode(data)

    def test_request_uses_exact_vendor_type_loopback_and_one_attempt(self):
        with patch.object(p.http.client, "HTTPConnection") as factory:
            response = factory.return_value.getresponse.return_value
            response.status = 200
            response.read.return_value = b'{"code":0,"message":"ok"}'
            api = p.Api(8697, "synthetic-user", "synthetic-password")
            api.request("PUT", "vms/probe/params", {"name": p.FIELDS[0], "value": "00:50:56:01:02:03"})
            factory.assert_called_once_with("127.0.0.1", 8697, timeout=10)
            args = factory.return_value.request.call_args.args
            self.assertEqual("application/vnd.vmware.vmw.rest-v1+json", args[3]["Content-Type"])
            self.assertNotIn("synthetic-password", args[2].decode())
            self.assertEqual(1, factory.return_value.request.call_count)
            factory.return_value.close.assert_called_once()

    def test_rejected_response_reads_only_bounded_diagnostics_without_echo_or_retry(self):
        with patch.object(p.http.client, "HTTPConnection") as factory:
            response = factory.return_value.getresponse.return_value
            response.status = 401
            response.read.return_value = b'{"code":401,"message":"private-password-and-path"}'
            with self.assertRaisesRegex(p.Blocked, "^api_http_401$") as raised:
                p.Api(8697, "user", "password").request("GET", "vms")
            response.read.assert_called_once_with(p.ERROR_LIMIT + 1)
            self.assertEqual("api_error", raised.exception.response_kind)
            self.assertEqual(401, raised.exception.vendor_code)
            self.assertNotIn("private-password-and-path", json.dumps(raised.exception.diagnostics()))
            self.assertEqual(1, factory.return_value.request.call_count)

    def test_rejection_identifies_method_and_resource_without_vm_id_or_body(self):
        with patch.object(p.http.client, "HTTPConnection") as factory:
            response = factory.return_value.getresponse.return_value
            response.status = 404
            response.read.return_value = b"404 page not found\n"
            with self.assertRaises(p.ApiRejected) as raised:
                p.Api(8697, "user", "password").request("PUT", "vms/private-vm-id/configparams", {})
            self.assertEqual((404, "PUT", "configparams"),
                             (raised.exception.status, raised.exception.method, raised.exception.resource))
            self.assertNotIn("private-vm-id", str(raised.exception))
            self.assertEqual("router_not_found", raised.exception.response_kind)
            response.read.assert_called_once_with(p.ERROR_LIMIT + 1)

    def test_vendor_resource_404_is_distinguished_from_missing_route_without_fallback(self):
        with patch.object(p.http.client, "HTTPConnection") as factory:
            response = factory.return_value.getresponse.return_value
            response.status = 404
            response.read.return_value = b'{"code":1000,"message":"private-vm-id private-password"}'
            with self.assertRaises(p.ApiRejected) as raised:
                p.Api(8697, "user", "password").request("PUT", "vms/private-vm-id/params", {})
            self.assertEqual("api_error", raised.exception.response_kind)
            self.assertEqual(1000, raised.exception.vendor_code)
            self.assertNotIn("private", json.dumps(raised.exception.__dict__))
            factory.return_value.request.assert_called_once()
            self.assertEqual("/api/vms/private-vm-id/params", factory.return_value.request.call_args.args[1])

    def test_untrusted_or_oversize_error_body_does_not_replace_http_failure(self):
        cases = ((b"x" * (p.ERROR_LIMIT + 1), "oversize_error_body"),
                 (b'{"code":true,"message":"secret"}', "unrecognized_error_body"),
                 (b'{"code":1,"code":2,"message":"secret"}', "unrecognized_error_body"),
                 (b"not json: secret", "unrecognized_error_body"))
        for body, kind in cases:
            with self.subTest(kind=kind), patch.object(p.http.client, "HTTPConnection") as factory:
                response = factory.return_value.getresponse.return_value
                response.status, response.read.return_value = 404, body
                with self.assertRaises(p.ApiRejected) as raised:
                    p.Api(8697, "user", "password").request("PUT", "vms/test-vm/params", {})
                self.assertEqual(404, raised.exception.status)
                self.assertEqual(kind, raised.exception.response_kind)
                self.assertIsNone(raised.exception.vendor_code)
                self.assertNotIn("secret", json.dumps(raised.exception.__dict__))
                response.read.assert_called_once_with(p.ERROR_LIMIT + 1)
                factory.return_value.request.assert_called_once()
                factory.return_value.close.assert_called_once()

    def test_error_body_read_failure_keeps_status_and_never_retries(self):
        with patch.object(p.http.client, "HTTPConnection") as factory:
            response = factory.return_value.getresponse.return_value
            response.status, response.read.side_effect = 404, TimeoutError("private details")
            with self.assertRaises(p.ApiRejected) as raised:
                p.Api(8697, "user", "password").request("PUT", "vms/test-vm/params", {})
            self.assertEqual((404, "unreadable_error_body"),
                             (raised.exception.status, raised.exception.response_kind))
            self.assertNotIn("private", json.dumps(raised.exception.__dict__))
            factory.return_value.request.assert_called_once()
            factory.return_value.close.assert_called_once()


class VmrestRouteTests(unittest.TestCase):
    """Exercise actual HTTP bytes against the route bound in vmrest build 23775571.

    Unlike FakeApi, the server does not accept the Swagger-only /configparams.
    All files, accounts and VM identifiers below are synthetic test data.
    """
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="vmil-m11-http-")
        self.addCleanup(temporary.cleanup)
        self.folder = p.prepare(pathlib.Path(temporary.name))
        state = self
        self.registered, self.requests, self.puts = False, [], []

        class Handler(BaseHTTPRequestHandler):
            def log_message(self, *_):
                pass

            def respond(self, status, body):
                self.send_response(status)
                self.send_header("Content-Type", "application/vnd.vmware.vmw.rest-v1+json")
                self.end_headers()
                if body is not None:
                    self.wfile.write(json.dumps(body).encode())

            def handle_request(self):
                state.requests.append((self.command, self.path))
                raw = self.rfile.read(int(self.headers.get("Content-Length", "0")))
                body = json.loads(raw) if raw else None
                if self.headers.get("Accept") != "application/vnd.vmware.vmw.rest-v1+json":
                    return self.respond(406, {"code": 406, "message": "Invalid Accept"})
                if body is not None and self.headers.get("Content-Type") != "application/vnd.vmware.vmw.rest-v1+json":
                    return self.respond(406, {"code": 406, "message": "Invalid Content-Type"})
                path = state.folder / "probe.vmx"
                if self.command == "GET" and self.path == "/api/vms":
                    return self.respond(200, [{"id": "test-vm", "path": str(path.resolve())}] if state.registered else [])
                if self.command == "POST" and self.path == "/api/vms/registration":
                    if body["path"] != str(path.resolve()) or state.registered:
                        return self.respond(400, {"code": 400, "message": "Wrong registration"})
                    state.registered = True
                    return self.respond(201, {"id": "test-vm"})
                if state.registered and self.command == "GET" and self.path == "/api/vms/test-vm/power":
                    return self.respond(200, {"power_state": "poweredOff"})
                if state.registered and self.command == "GET" and self.path.startswith("/api/vms/test-vm/params/"):
                    field = self.path.rsplit("/", 1)[1]
                    return self.respond(200, {"name": field, "value": p.entries(path.read_bytes())[field]})
                # Actual RestServer registration, not the installed Swagger path.
                if state.registered and self.command == "PUT" and self.path == "/api/vms/test-vm/params":
                    if (set(body) != {"name", "value"} or body["name"] not in p.FIELDS
                            or not isinstance(body["value"], str)):
                        return self.respond(400, {"code": 400, "message": "Wrong name/value body"})
                    state.puts.append(body)
                    lines = path.read_text(encoding="utf-8").splitlines(keepends=True)
                    path.write_text("".join(f'{body["name"]} = "{body["value"]}"\n'
                                            if line.startswith(body["name"] + " = ") else line for line in lines),
                                    encoding="utf-8", newline="\n")
                    # The pinned runtime calls WriteResponseJSON(200, nil).
                    return self.respond(200, None)
                self.send_response(404)
                self.send_header("Content-Type", "text/plain; charset=utf-8")
                self.end_headers()
                self.wfile.write(b"404 page not found\n")

            do_GET = do_POST = do_PUT = handle_request

        self.server = ThreadingHTTPServer(("127.0.0.1", 0), Handler)
        self.thread = threading.Thread(target=self.server.serve_forever, kwargs={"poll_interval": 0.02})
        self.thread.start()
        self.addCleanup(self.close_server)
        self.api = p.Api(self.server.server_port, "synthetic-user", "synthetic-password")

    def close_server(self):
        self.server.shutdown()
        self.server.server_close()
        self.thread.join(timeout=2)
        self.assertFalse(self.thread.is_alive())

    def test_real_transport_round_trips_both_fields_through_runtime_route(self):
        probe = p.Probe(self.folder, self.api, lambda: {"installedVersion": p.VERSION}, pathlib.Path.read_bytes)
        probe.execute()
        self.assertEqual([{"name": field, "value": value} for field in p.FIELDS
                          for value in (probe.meta["after"][field], probe.original[field])], self.puts)
        self.assertEqual(probe.baseline, (self.folder / "probe.vmx").read_bytes())
        self.assertEqual("completed", probe.events()[-1]["phase"])
        self.assertEqual(4, self.requests.count(("PUT", "/api/vms/test-vm/params")))
        self.assertFalse(any("configparams" in path for _, path in self.requests))
        applied = [event for event in probe.events() if event["phase"] in ("applied", "restored")]
        self.assertEqual(4, len(applied))
        self.assertTrue(all(event["response"] == {"httpStatus": 200, "bodyBytes": 0, "bodyKind": "empty"}
                            for event in applied))

    def test_swagger_only_path_returns_router_404_without_mutation(self):
        self.registered = True
        before = (self.folder / "probe.vmx").read_bytes()
        with self.assertRaises(p.ApiRejected) as raised:
            self.api.request("PUT", "vms/test-vm/configparams", {"name": p.FIELDS[0], "value": "00:50:56:01:02:03"})
        self.assertEqual(404, raised.exception.status)
        self.assertEqual("router_not_found", raised.exception.response_kind)
        self.assertEqual([], self.puts)
        self.assertEqual(before, (self.folder / "probe.vmx").read_bytes())


if __name__ == "__main__":
    unittest.main()
