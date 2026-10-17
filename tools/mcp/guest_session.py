"""Temporary vmrun test session with a local, non-persisting Windows credential prompt.

Only vmrun test operations are accepted. Commands and results contain no credential fields.
There is no listener, service install, telemetry, VM power or configuration operation.
"""
import argparse
import json
import pathlib
import subprocess
import time
from datetime import datetime, timedelta, timezone

import win32cred

OPERATIONS = {
    "processes": "listProcessesInGuest",
    "run": "runProgramInGuest",
    "copy_to": "CopyFileFromHostToGuest",
    "copy_from": "CopyFileFromGuestToHost",
    "file_exists": "fileExistsInGuest",
    "dir_exists": "directoryExistsInGuest",
}


def now():
    return datetime.now(timezone(timedelta(hours=8))).isoformat()


def save(path, result):
    temporary = path.with_suffix(".writing")
    temporary.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    temporary.replace(path)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--vmrun", required=True)
    parser.add_argument("--vmx", required=True)
    parser.add_argument("--user", required=True)
    parser.add_argument("--evidence", type=pathlib.Path, required=True)
    args = parser.parse_args()
    commands, results = args.evidence / "commands", args.evidence / "results"
    commands.mkdir(parents=True, exist_ok=True)
    results.mkdir(parents=True, exist_ok=True)
    state = args.evidence / "session.json"
    password = None
    try:
        save(state, {"status": "awaiting_local_credentials", "time": now()})
        flags = (win32cred.CREDUI_FLAGS_GENERIC_CREDENTIALS | win32cred.CREDUI_FLAGS_ALWAYS_SHOW_UI
                 | win32cred.CREDUI_FLAGS_DO_NOT_PERSIST)
        user, password, _ = win32cred.CredUIPromptForCredentials(
            "VMIdentityLabGuestTest", UserName=args.user, Save=False, Flags=flags,
            UiInfo={"CaptionText": "VM Identity Lab 客户机测试",
                    "MessageText": "请输入客户机Windows账户密码，仅用于本轮vmrun测试。不保存凭据。"})
        if not password:
            save(state, {"status": "blocked", "reason": "empty_password", "time": now()})
            return 1

        def call(operation, arguments):
            started = time.monotonic()
            command = [args.vmrun, "-T", "ws", "-gu", user, "-gp", password,
                       OPERATIONS[operation], args.vmx, *arguments]
            try:
                process = subprocess.run(command, capture_output=True, stdin=subprocess.DEVNULL,
                                         timeout=45, creationflags=subprocess.CREATE_NO_WINDOW)
                output = process.stdout.decode("mbcs", errors="replace").replace(password, "[credential]")
                if operation == "processes" and process.returncode == 0:
                    output = "\n".join(line for line in output.splitlines() if "VMIdentityLab.App.exe" in line)
                return {"exitCode": process.returncode,
                        "stdout": output,
                        "stderr": process.stderr.decode("mbcs", errors="replace").replace(password, "[credential]"),
                        "seconds": round(time.monotonic() - started, 3), "time": now()}
            except subprocess.TimeoutExpired:
                return {"exitCode": None, "error": "timeout", "seconds": 45, "time": now()}

        authentication = call("processes", [])
        if authentication["exitCode"] != 0:
            save(state, {"status": "authentication_failed", "time": now(),
                         "exitCode": authentication["exitCode"], "error": authentication.get("error"),
                         "stderr": authentication.get("stderr"), "stdout": authentication.get("stdout")})
            return 1
        # Authentication process listings are not copied into public or private general logs.
        save(state, {"status": "ready", "authenticated": True, "time": now()})
        processed = {path.name for path in results.glob("*.json")}
        paused = False
        last_activity = time.monotonic()
        while time.monotonic() - last_activity < 1800:
            for path in sorted(commands.glob("*.json")):
                if path.name in processed:
                    continue
                item = json.loads(path.read_text(encoding="utf-8-sig"))
                operation = item["operation"]
                if operation == "stop":
                    save(state, {"status": "stopped", "time": now()})
                    return 0
                if operation == "resume":
                    paused = False
                    processed.add(path.name)
                    save(state, {"status": "ready", "authenticated": True, "time": now()})
                    continue
                if operation not in OPERATIONS:
                    raise ValueError("Unsupported test operation")
                if paused and operation not in {"copy_from", "processes", "file_exists", "dir_exists"}:
                    continue
                arguments = item.get("arguments", [])
                if not isinstance(arguments, list) or not all(isinstance(value, str) for value in arguments):
                    raise ValueError("Arguments must be strings")
                result = call(operation, arguments)
                save(results / path.name, {"operation": operation, "arguments": arguments, **result})
                processed.add(path.name)
                last_activity = time.monotonic()
                if result["exitCode"] != 0:
                    paused = True
                    save(state, {"status": "operation_failed", "operation": operation,
                                 "result": path.name, "time": now()})
            time.sleep(0.3)
        save(state, {"status": "idle_timeout", "time": now()})
        return 1
    except Exception as error:
        # Never stringify subprocess exceptions: they can embed the credential-bearing command.
        save(state, {"status": "failed", "errorType": type(error).__name__, "time": now()})
        return 1
    finally:
        password = None


if __name__ == "__main__":
    raise SystemExit(main())
