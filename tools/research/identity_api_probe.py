"""M11 research only: existing-key round trips on an unpowered, diskless fixture.

Not an App backend. No arbitrary VM/key/value input, VM power, DELETE, retries,
direct VMX replacement, or automatic recovery after an uncertain request.
"""
import argparse
import base64
import ctypes
import hashlib
import http.client
import json
import os
import pathlib
import re
import secrets
import socket
import uuid
import zipfile
from contextlib import contextmanager
from ctypes import wintypes as w
from datetime import datetime, timezone
from urllib.parse import quote

VERSION = "17.5.2.23775571"
CONTRACT_HASH = "71c8094a4ab765151111ac4fd68a2bf8ace1061d470ef4ac2c5dc6f86aebb3f7"
VMWARE_HASH = "4eecd8126621472db77d89bb6b83ab40799404890fbea5b20d8978680aea79d2"
VMREST_HASH = "72afedfc0403a5e21ec44c9176241096077d74f6d0fc8e8e862bfd8b6ee57646"
FIELDS = ("ethernet0.address", "uuid.bios")
LIMIT = 2 * 1024 * 1024
ERROR_LIMIT = 4096
# vmrest 1.3.1 build-23775571 binds UpdateConfigParamsHandler here.
# Its bundled Swagger incorrectly calls this resource "configparams". This
# mapping is research-only and remains pinned to the reviewed binary above.
PARAMETER_WRITE_RESOURCE = "params"
ENTRY = re.compile(r'^([A-Za-z0-9_.]+) = "([^"\r\n]*)"$')


class Blocked(RuntimeError):
    """Messages are fixed reason codes; never vendor bodies or credentials."""


class ApiRejected(Blocked):
    def __init__(self, status, method, resource, response_kind="not_captured", vendor_code=None):
        super().__init__("api_http_" + str(status))
        self.status, self.method, self.resource = status, method, resource
        self.response_kind, self.vendor_code = response_kind, vendor_code
        self.stage = self.field = self.reverse = None

    def diagnostics(self):
        result = {"method": self.method, "resource": self.resource, "httpStatus": self.status,
                  "responseKind": self.response_kind}
        if self.vendor_code is not None:
            result["vendorCode"] = self.vendor_code
        if self.stage is not None:
            result.update(stage=self.stage, field=self.field, reverse=self.reverse)
        return result


def rejection_details(response):
    """Classify a bounded error without retaining vendor messages, IDs or paths."""
    try:
        data = response.read(ERROR_LIMIT + 1)
    except (OSError, http.client.HTTPException):
        return "unreadable_error_body", None
    if len(data) > ERROR_LIMIT:
        return "oversize_error_body", None
    if response.status == 404 and data == b"404 page not found\n":
        return "router_not_found", None
    try:
        value = decode(data)
    except (ValueError, UnicodeError, Blocked):
        return "unrecognized_error_body", None
    if (isinstance(value, dict) and type(value.get("code")) is int
            and -(2 ** 31) <= value["code"] < 2 ** 31 and isinstance(value.get("message"), str)):
        return "api_error", value["code"]
    return "unrecognized_error_body", None


def digest(data):
    return hashlib.sha256(data).hexdigest()


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise Blocked("ambiguous_json")
        result[key] = value
    return result


def decode(data):
    if len(data) > LIMIT:
        raise Blocked("oversize_input")
    return json.loads(data, object_pairs_hook=unique_object)


def new_json(path, value):
    with path.open("x", encoding="utf-8", newline="\n") as stream:
        json.dump(value, stream, ensure_ascii=False, indent=2)
        stream.flush()
        os.fsync(stream.fileno())


def entries(data):
    if len(data) > LIMIT:
        raise Blocked("oversize_vmx")
    result = {}
    for line in data.decode("utf-8").splitlines():
        match = ENTRY.fullmatch(line)
        if not match or match[1].lower() in result:
            raise Blocked("unsupported_fixture_syntax")
        result[match[1].lower()] = match[2]
    return result


def mac():
    return "00:50:56:" + ":".join(f"{value:02x}" for value in
                                  (secrets.randbelow(64), secrets.randbelow(256), secrets.randbelow(256)))


def bios_uuid():
    raw = b"\x56\x4d" + secrets.token_bytes(14)
    return " ".join(f"{value:02x}" for value in raw[:8]) + "-" + " ".join(f"{value:02x}" for value in raw[8:])


def prepare(root):
    """Only create new synthetic project files; no VMware process/API is used."""
    identifier = str(uuid.uuid4())
    folder = root / identifier
    folder.mkdir(parents=True, exist_ok=False)
    values = {".encoding": "UTF-8", "config.version": "8", "virtualHW.version": "19",
              "displayName": "VMIL M11 diskless probe " + identifier, "guestOS": "other-64",
              "memsize": "512", "numvcpus": "1", "uuid.bios": bios_uuid(),
              "ethernet0.present": "TRUE", "ethernet0.virtualDev": "e1000e",
              "ethernet0.connectionType": "hostonly", "ethernet0.startConnected": "FALSE",
              "ethernet0.addressType": "static", "ethernet0.address": mac()}
    data = "".join(f'{key} = "{value}"\n' for key, value in values.items()).encode("utf-8")
    for name in ("probe.vmx", "baseline.vmx"):
        with (folder / name).open("xb") as stream:
            stream.write(data)
            stream.flush()
            os.fsync(stream.fileno())
    (folder / "lease.lock").write_bytes(b"0")
    new_json(folder / "fixture.json", {"schema": 1, "id": identifier, "originalHash": digest(data),
                                      "after": {FIELDS[0]: mac(), FIELDS[1]: bios_uuid()}})
    return folder


def file_version(path):
    version = ctypes.WinDLL("version", use_last_error=True)
    version.GetFileVersionInfoSizeW.argtypes = [w.LPCWSTR, ctypes.POINTER(w.DWORD)]
    version.GetFileVersionInfoSizeW.restype = w.DWORD
    version.GetFileVersionInfoW.argtypes = [w.LPCWSTR, w.DWORD, w.DWORD, w.LPVOID]
    version.VerQueryValueW.argtypes = [w.LPCVOID, w.LPCWSTR, ctypes.POINTER(w.LPVOID), ctypes.POINTER(w.UINT)]
    size = version.GetFileVersionInfoSizeW(str(path), None)
    if not size:
        raise ctypes.WinError(ctypes.get_last_error())
    data, pointer, length = ctypes.create_string_buffer(size), w.LPVOID(), w.UINT()
    if (not version.GetFileVersionInfoW(str(path), 0, size, data)
            or not version.VerQueryValueW(data, "\\VarFileInfo\\Translation", ctypes.byref(pointer), ctypes.byref(length))
            or length.value == 0 or length.value % 4 or length.value > 128):
        raise Blocked("invalid_file_version_resource")
    # The fixed resource's fourth WORD is NOT the Workstation build number
    # (this installed build has FilePrivatePart=59081). Read the full string.
    translations = list(ctypes.cast(pointer, ctypes.POINTER(w.WORD * (length.value // 2))).contents)
    values = set()
    for index in range(0, len(translations), 2):
        query = f"\\StringFileInfo\\{translations[index]:04x}{translations[index + 1]:04x}\\FileVersion"
        if not version.VerQueryValueW(data, query, ctypes.byref(pointer), ctypes.byref(length)):
            raise Blocked("missing_file_version_string")
        values.add(ctypes.wstring_at(pointer))
    if len(values) != 1:
        raise Blocked("ambiguous_file_version_string")
    return values.pop()


def server_identity(port, expected_path):
    """Read the IPv4 listener owner, not just an arbitrary installed EXE version."""
    ip = ctypes.WinDLL("iphlpapi", use_last_error=True)
    ip.GetExtendedTcpTable.argtypes = [w.LPVOID, ctypes.POINTER(w.DWORD), w.BOOL, w.ULONG, w.INT, w.ULONG]
    ip.GetExtendedTcpTable.restype = w.DWORD
    size = w.DWORD()
    if ip.GetExtendedTcpTable(None, ctypes.byref(size), False, 2, 3, 0) != 122:
        raise Blocked("listener_table_unavailable")
    data = ctypes.create_string_buffer(size.value)
    if ip.GetExtendedTcpTable(data, ctypes.byref(size), False, 2, 3, 0):
        raise Blocked("listener_table_changed")
    count = w.DWORD.from_buffer(data).value
    if 4 + count * 24 > size.value:
        raise Blocked("invalid_listener_table")
    owners = []
    for index in range(count):
        row = (w.DWORD * 6).from_buffer(data, 4 + index * 24)
        if socket.ntohs(row[2] & 0xffff) == port:
            if row[1] != 0x0100007f:
                raise Blocked("listener_not_ipv4_loopback_only")
            owners.append(row[5])
    if len(owners) != 1:
        raise Blocked("no_unique_local_listener")
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel.OpenProcess.argtypes = [w.DWORD, w.BOOL, w.DWORD]
    kernel.OpenProcess.restype = w.HANDLE
    kernel.QueryFullProcessImageNameW.argtypes = [w.HANDLE, w.DWORD, w.LPWSTR, ctypes.POINTER(w.DWORD)]
    kernel.CloseHandle.argtypes = [w.HANDLE]
    handle = kernel.OpenProcess(0x1000, False, owners[0])
    if not handle:
        raise ctypes.WinError(ctypes.get_last_error())
    try:
        path, length = ctypes.create_unicode_buffer(32768), w.DWORD(32768)
        if not kernel.QueryFullProcessImageNameW(handle, 0, path, ctypes.byref(length)):
            raise ctypes.WinError(ctypes.get_last_error())
        if pathlib.Path(path.value).resolve() != expected_path.resolve():
            raise Blocked("listener_is_not_reviewed_vmrest")
        return owners[0]
    finally:
        kernel.CloseHandle(handle)


def fingerprint(installation, port=None):
    """Pin installed distribution, not a claim about every 17.x release/server."""
    version = file_version(installation / "vmware.exe")
    with zipfile.ZipFile(installation / "swagger.zip") as archive:
        contract = archive.read("json/swagger_WS.json")
    if version != "17.5.2 build-23775571" or digest(contract) != CONTRACT_HASH:
        raise Blocked("unreviewed_installed_build_or_contract")
    result = {"installedVersion": VERSION, "fileVersionString": version, "contractSha256": digest(contract),
              "vmwareSha256": digest((installation / "vmware.exe").read_bytes()),
              "vmrestSha256": digest((installation / "vmrest.exe").read_bytes())}
    if result["vmwareSha256"] != VMWARE_HASH or result["vmrestSha256"] != VMREST_HASH:
        raise Blocked("unreviewed_distribution_binaries")
    if port is not None:
        result["listenerPid"] = server_identity(port, installation / "vmrest.exe")
    return result


def read_exclusive(path):
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel.CreateFileW.argtypes = [w.LPCWSTR, w.DWORD, w.DWORD, w.LPVOID, w.DWORD, w.DWORD, w.HANDLE]
    kernel.CreateFileW.restype = w.HANDLE
    kernel.ReadFile.argtypes = [w.HANDLE, w.LPVOID, w.DWORD, ctypes.POINTER(w.DWORD), w.LPVOID]
    kernel.GetFileInformationByHandle.argtypes = [w.HANDLE, w.LPVOID]
    kernel.CloseHandle.argtypes = [w.HANDLE]
    handle = kernel.CreateFileW(str(path), 0x80000000, 0, None, 3, 0x00200000, None)
    if handle == w.HANDLE(-1).value:
        raise ctypes.WinError(ctypes.get_last_error())
    try:
        info = ctypes.create_string_buffer(52)  # BY_HANDLE_FILE_INFORMATION
        if not kernel.GetFileInformationByHandle(handle, info):
            raise ctypes.WinError(ctypes.get_last_error())
        if w.DWORD.from_buffer(info).value & 0x400:
            raise Blocked("reparse_vmx")
        data, count = ctypes.create_string_buffer(LIMIT + 1), w.DWORD()
        if not kernel.ReadFile(handle, data, LIMIT + 1, ctypes.byref(count), None):
            raise ctypes.WinError(ctypes.get_last_error())
        return data.raw[:count.value]
    finally:
        kernel.CloseHandle(handle)


def credentials():
    # Native CredUI; no save checkbox, persistence, subprocess or password argument.
    class Info(ctypes.Structure):
        _fields_ = [("size", w.DWORD), ("owner", w.HWND), ("message", w.LPCWSTR),
                    ("caption", w.LPCWSTR), ("banner", w.HBITMAP)]
    credui = ctypes.WinDLL("credui", use_last_error=True)
    credui.CredUIPromptForCredentialsW.argtypes = [ctypes.POINTER(Info), w.LPCWSTR, w.LPVOID, w.DWORD,
                                                 w.LPWSTR, w.ULONG, w.LPWSTR, w.ULONG,
                                                 ctypes.POINTER(w.BOOL), w.DWORD]
    credui.CredUIPromptForCredentialsW.restype = w.DWORD
    user32 = ctypes.WinDLL("user32", use_last_error=True)
    user32.GetForegroundWindow.restype = w.HWND
    info = Info(ctypes.sizeof(Info), user32.GetForegroundWindow(),
                "仅用于已授权的无磁盘探针。本机 API 专用账号；不保存凭据。",
                "VM Identity Lab M11 API 探针", None)
    user, password, save = ctypes.create_unicode_buffer(514), ctypes.create_unicode_buffer(257), w.BOOL(False)
    try:
        result = credui.CredUIPromptForCredentialsW(ctypes.byref(info), "VMIdentityLabM11ApiProbe", None,
                                                   0, user, len(user), password, len(password),
                                                   ctypes.byref(save), 0x40000 | 0x80 | 0x2)
        if result:
            raise Blocked("local_credential_prompt_cancelled_or_failed")
        return user.value, password.value
    finally:
        ctypes.memset(ctypes.addressof(user), 0, ctypes.sizeof(user))
        ctypes.memset(ctypes.addressof(password), 0, ctypes.sizeof(password))


@contextmanager
def lease(folder):
    import msvcrt
    with (folder / "lease.lock").open("r+b") as stream:
        msvcrt.locking(stream.fileno(), msvcrt.LK_NBLCK, 1)
        try:
            yield stream
        finally:
            stream.seek(0)
            msvcrt.locking(stream.fileno(), msvcrt.LK_UNLCK, 1)


class Api:
    def __init__(self, port, user, password):
        if type(port) is not int or not 1 <= port <= 65535:
            raise Blocked("invalid_port")
        self.port = port
        self.authorization = "Basic " + base64.b64encode(f"{user}:{password}".encode()).decode("ascii")
        self.last_response = None

    def request(self, method, path, body=None):
        # http.client uses no proxy/redirect mechanism. One connection, one attempt.
        connection = http.client.HTTPConnection("127.0.0.1", self.port, timeout=10)
        headers = {"Authorization": self.authorization,
                   "Accept": "application/vnd.vmware.vmw.rest-v1+json"}
        if body is not None:
            headers["Content-Type"] = "application/vnd.vmware.vmw.rest-v1+json"
        try:
            connection.request(method, "/api/" + path,
                               None if body is None else json.dumps(body).encode("utf-8"), headers)
            response = connection.getresponse()
            if response.status != (201 if method == "POST" else 200):
                resource = ("configparams" if path.endswith("/configparams") else
                            "params" if "/params/" in path or path.endswith("/params") else
                            "power" if path.endswith("/power") else
                            "registration" if path == "vms/registration" else "inventory" if path == "vms" else "other")
                kind, code = rejection_details(response)
                raise ApiRejected(response.status, method, resource, kind, code)
            data = response.read(LIMIT + 1)
            self.last_response = {"httpStatus": response.status, "bodyBytes": len(data),
                                  "bodyKind": "empty" if not data else "json"}
            # This exact build calls WriteResponseJSON(200, nil) for a successful
            # parameter update. Empty is allowed only here; Probe still requires
            # independent API/whole-VMX readback before recording any success.
            if not data and method == "PUT" and path.endswith("/" + PARAMETER_WRITE_RESOURCE):
                return None
            return decode(data)
        finally:
            connection.close()


class Probe:
    def __init__(self, folder, api, installation_check, read_vmx=read_exclusive):
        self.folder, self.api, self.installation_check, self.read_vmx = folder, api, installation_check, read_vmx
        if folder.is_symlink() or any(part.is_symlink() for part in folder.parents):
            raise Blocked("reparse_fixture_path")
        self.meta = decode((folder / "fixture.json").read_bytes())
        if self.meta["schema"] != 1 or str(uuid.UUID(self.meta["id"])) != folder.name:
            raise Blocked("invalid_fixture_identity")
        for name in ("fixture.json", "baseline.vmx", "probe.vmx", "lease.lock"):
            if (folder / name).is_symlink():
                raise Blocked("reparse_fixture_file")
        self.baseline = (folder / "baseline.vmx").read_bytes()
        if digest(self.baseline) != self.meta["originalHash"]:
            raise Blocked("backup_hash_mismatch")
        self.original = entries(self.baseline)
        # Restrict the entire fixture, including its resource/network declarations.
        allowed = {".encoding", "config.version", "virtualhw.version", "displayname", "guestos",
                   "memsize", "numvcpus", "uuid.bios", "ethernet0.present", "ethernet0.virtualdev",
                   "ethernet0.connectiontype", "ethernet0.startconnected", "ethernet0.addresstype",
                   "ethernet0.address"}
        required = {"ethernet0.addresstype": "static", "ethernet0.startconnected": "FALSE",
                    "ethernet0.connectiontype": "hostonly", "virtualhw.version": "19",
                    "displayname": "VMIL M11 diskless probe " + folder.name}
        if set(self.original) != allowed or any(self.original[key] != value for key, value in required.items()):
            raise Blocked("not_a_diskless_probe")
        if set(self.meta["after"]) != set(FIELDS):
            raise Blocked("unsupported_probe_fields")
        for field in FIELDS:
            pattern = (r"00:50:56:[0-3][0-9a-f]:[0-9a-f]{2}:[0-9a-f]{2}" if field == FIELDS[0]
                       else r"56 4d(?: [0-9a-f]{2}){6}-(?:[0-9a-f]{2} ){7}[0-9a-f]{2}")
            if any(not re.fullmatch(pattern, value) for value in
                   (self.original[field], self.meta["after"][field])):
                raise Blocked("unsupported_probe_value")
            if self.original[field] == self.meta["after"][field]:
                raise Blocked("no_probe_change")
        self.path = folder / "probe.vmx"
        self.installation = installation_check()

    def events(self):
        files = sorted(self.folder.glob("event-*.json"))
        if [path.name for path in files] != [f"event-{index:03}.json" for index in range(len(files))]:
            raise Blocked("incomplete_probe_journal")
        return [decode(path.read_bytes()) for path in files]

    def record(self, phase, **details):
        new_json(self.folder / f"event-{len(self.events()):03}.json",
                 {"phase": phase, "time": datetime.now(timezone.utc).isoformat(), **details})

    def resolve(self, required=True):
        inventory = self.api.request("GET", "vms")
        if not isinstance(inventory, list):
            raise Blocked("invalid_inventory")
        ids, matches = set(), []
        for item in inventory:
            identifier, path = item["id"], item["path"]
            if not isinstance(identifier, str) or not identifier or identifier in ids or not pathlib.Path(path).is_absolute():
                raise Blocked("ambiguous_inventory")
            ids.add(identifier)
            if os.path.normcase(os.path.abspath(path)) == os.path.normcase(str(self.path.resolve())):
                matches.append(identifier)
        if len(matches) > 1 or (required and not matches):
            raise Blocked("no_unique_registration")
        return matches[0] if matches else None

    @staticmethod
    def vm_path(identifier):
        if identifier in (".", ".."):
            raise Blocked("invalid_vm_id")
        return "vms/" + quote(identifier, safe="")

    def guard(self, expected, expected_hash=None, identifier=None):
        if self.installation_check() != self.installation:
            raise Blocked("installed_distribution_changed")
        current_id = self.resolve()
        if identifier is not None and current_id != identifier:
            raise Blocked("registration_changed")
        prefix = self.vm_path(current_id)
        if self.api.request("GET", prefix + "/power") != {"power_state": "poweredOff"}:
            raise Blocked("not_powered_off")
        for field in FIELDS:
            if self.api.request("GET", prefix + "/params/" + field) != {"name": field, "value": expected[field]}:
                raise Blocked("parameter_readback_mismatch")
        if any(self.folder.glob("*.lck")) or any(self.folder.glob("*.vmss")):
            raise Blocked("vm_locked_or_suspended")
        data = self.read_vmx(self.path)
        if entries(data) != expected or (expected_hash is not None and digest(data) != expected_hash):
            raise Blocked("fixture_changed")
        return current_id, digest(data)

    def register(self):
        if self.events() or self.resolve(required=False) is not None:
            raise Blocked("registration_already_attempted")
        if self.read_vmx(self.path) != self.baseline:
            raise Blocked("fixture_changed")
        self.record("registration-intent")
        if (self.installation_check() != self.installation or self.read_vmx(self.path) != self.baseline
                or self.resolve(required=False) is not None):
            raise Blocked("registration_context_changed")
        self.api.request("POST", "vms/registration", {"name": self.original["displayname"], "path": str(self.path.resolve())})
        self.guard(self.original)
        self.record("registered")

    def change(self, field, reverse, expected_hash, identifier):
        before, after = dict(self.original), dict(self.original)
        after[field] = self.meta["after"][field]
        if reverse:
            before, after = after, before
        stage = "before-intent"
        try:
            self.guard(before, expected_hash, identifier)
            # Durable intent BEFORE the final guards and dispatch. Any later
            # failure leaves a reconciliation requirement; never blind-retry PUT.
            self.record("restore-intent" if reverse else "apply-intent", field=field, inputHash=expected_hash,
                        vmId=identifier, installation=self.installation)
            stage = "before-put"
            self.guard(before, expected_hash, identifier)
            stage = "put"
            self.api.request("PUT", self.vm_path(identifier) + "/" + PARAMETER_WRITE_RESOURCE,
                             {"name": field, "value": after[field]})
            response_info = getattr(self.api, "last_response", None)
            stage = "readback"
            _, observed_hash = self.guard(after, identifier=identifier)
        except ApiRejected as rejected:
            rejected.stage, rejected.field, rejected.reverse = stage, field, reverse
            if stage == "put" and 400 <= rejected.status < 500:
                try:
                    self.guard(before, expected_hash, identifier)
                except Exception:
                    # Preserve the intent. A rejection alone proves no outcome.
                    raise rejected from None
                self.record("rejected-unchanged", field=field, reverse=reverse, status=rejected.status,
                            method=rejected.method, resource=rejected.resource, responseKind=rejected.response_kind,
                            vendorCode=rejected.vendor_code, vmId=identifier, observedHash=expected_hash)
            raise
        self.record("restored" if reverse else "applied", field=field, observedHash=observed_hash,
                    vmId=identifier, response=response_info)
        return observed_hash

    def run(self):
        history = self.events()
        if not history or history[-1]["phase"] not in ("registered", "registration-intent"):
            raise Blocked("run_not_fresh_or_not_registered")
        identifier, current_hash = self.guard(self.original)
        for field in FIELDS:
            current_hash = self.change(field, False, current_hash, identifier)
            current_hash = self.change(field, True, current_hash, identifier)
        self.record("completed", installation=self.installation,
                    baselineBytesEqual=current_hash == self.meta["originalHash"],
                    scope="existing static MAC and BIOS UUID; config-only, no power/guest evidence")

    def execute(self):
        self.register()
        self.run()

    def continue_uuid(self):
        # A field-specific continuation, never a retry of MAC or registration.
        history = self.events()
        if (not history or history[-1].get("field") != FIELDS[0]
                or history[-1]["phase"] not in ("apply-intent", "applied", "restore-intent", "rejected-unchanged")
                or (history[-1]["phase"] == "rejected-unchanged" and history[-1].get("reverse"))):
            raise Blocked("no_pending_mac_check_for_uuid_continuation")
        if history[-1]["phase"] != "rejected-unchanged":
            self.restore()
        identifier, current_hash = self.guard(self.original)
        current_hash = self.change(FIELDS[1], False, current_hash, identifier)
        current_hash = self.change(FIELDS[1], True, current_hash, identifier)
        self.record("uuid-completed", installation=self.installation,
                    baselineBytesEqual=current_hash == self.meta["originalHash"],
                    scope="BIOS UUID only; previous MAC attempt is not approved or replayed")

    def restore(self):
        history = self.events()
        if not history or history[-1]["phase"] not in ("apply-intent", "applied", "restore-intent"):
            raise Blocked("no_pending_field_to_reconcile")
        last = history[-1]
        field = last["field"]
        if field not in FIELDS:
            raise Blocked("unsupported_probe_fields")
        intended = dict(self.original)
        intended[field] = self.meta["after"][field]
        data = self.read_vmx(self.path)
        observed = entries(data)
        if observed == self.original:
            self.guard(self.original, digest(data), last["vmId"])
            self.record("reconciled-original", field=field)
            return
        if observed != intended:
            raise Blocked("recovery_external_change")
        if last["phase"] == "restore-intent":
            raise Blocked("prior_restore_not_confirmed_no_replay")
        if last["phase"] == "applied" and digest(data) != last["observedHash"]:
            raise Blocked("recovery_external_change")
        self.guard(intended, digest(data), last["vmId"])
        # A fresh, explicit restore invocation reverses the observed applied value,
        # never repeats an uncertain apply or writes an absent/unknown value.
        self.change(field, True, digest(data), last["vmId"])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=("prepare", "register", "run", "restore", "execute", "continue-uuid"))
    parser.add_argument("--fixture", type=pathlib.Path)
    parser.add_argument("--confirm", help="Exact fixture UUID; authorization must be obtained separately.")
    parser.add_argument("--installation", type=pathlib.Path)
    parser.add_argument("--port", type=int, default=8697)
    args = parser.parse_args()
    root = pathlib.Path(__file__).resolve().parents[2] / "local-data" / "m11-api-probes"
    if args.action == "prepare":
        print(prepare(root))
        return 0
    if (args.fixture is None or args.installation is None or args.fixture.parent.resolve() != root.resolve()
            or args.confirm != args.fixture.name):
        raise Blocked("exact_owned_fixture_and_confirmation_required")
    # No prompt/network before local identity and installation checks succeed.
    fingerprint(args.installation, args.port)
    user = password = api = None
    try:
        check = lambda: fingerprint(args.installation, args.port)
        probe = Probe(args.fixture, None, check)
        with lease(args.fixture):
            print(json.dumps({"status": "awaiting_local_credentials", "action": args.action}), flush=True)
            user, password = credentials()
            if not user or not password or ":" in user:
                raise Blocked("invalid_local_credentials")
            api = Api(args.port, user, password)
            probe.api = api
            getattr(probe, args.action.replace("-", "_"))()
            print(json.dumps({"result": "passed", "action": args.action, "fixtureId": args.fixture.name}))
        return 0
    finally:
        if api is not None:
            api.authorization = None
        user = password = None


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:
        # Exceptions can embed request/identity/credential data. No traceback/body.
        result = {"result": "blocked", "reason": str(error) if isinstance(error, Blocked) else type(error).__name__}
        if isinstance(error, ApiRejected):
            result.update(error.diagnostics())
        print(json.dumps(result))
        raise SystemExit(1)
