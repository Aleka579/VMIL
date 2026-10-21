# User guide — v0.3.0-alpha.1 local candidate

This version has not been pushed or published. The existing v0.2.0-alpha.1 Release retains its contents. `BUILD-INFO.json` identifies each package; [readiness](../RELEASE-READINESS.md) separates local, CI and target evidence. [简体中文](USER-GUIDE.md).

## Run and inspect

Extract the complete Windows x64 folder and run `VMIdentityLab.App.exe` as an ordinary user. No development SDK, system .NET or Python is required. The sidebar provides Overview, Detection, Configuration, Comparison and Reports; switch Chinese/English at the bottom.

Default detection collects 14 findings. Opting into UUID, MAC and BIOS serial adds three findings, still redacted in diagnostic reports. Missing, denied, failed and timed-out observations remain distinct. VMX declarations do not automatically identify the current Windows system.

```powershell
.\VMIdentityLab.App.exe --collect --output .\report.json --html .\report.html
.\VMIdentityLab.App.exe --collect --include-identifiers --output .\identity.json
```

CLI exports refuse existing output; normal diagnostic GUI exports ask before replacing files. Import diagnostic JSON on Comparison; redacted placeholders do not prove raw-value equality. Optional power-state queries require a previously configured loopback-only official vmrest API and a request-scoped API account, distinct from guest Windows credentials. The App does not configure/start the API or control VM power.

## Apply and restore

Numeric writes retain the exact Workstation17.5.2.23775571/hardware19/two-cores-per-socket scope: 4096↔6144 MiB and 2↔4 vCPU. Identity writes require that exact build/hardware, valid existing MAC and BIOS UUID, and one static host-only NIC disconnected at startup. Only one existing identity field is applied or restored per transaction. See [policy](VMWARE-CONFIG-POLICY.md) for formats and recovery limits.

Generate a fresh preview, review its target/differences/impact and explicitly confirm. Enter API credentials only in the local connection window. The backend rechecks power-off, identity, version, file occupancy and contents; verifies an original-byte backup and expected configuration; writes durable intent; then updates and verifies through the official API. It never directly replaces VMX. Changed inputs or external edits invalidate earlier previews.

Refresh Backup and recovery history, select the actual record and generate a restore preview, including after restarting the App. Restoration reverses recorded parameters and preserves other subsequent contents; incompatible field/topology changes prevent restoration. Backups do not restore disks, snapshots, NVRAM, guest state or entire VMs. Preserve unresolved-operation records, backups and expected files, verify the service result first and do not blindly retry or delete pending records. There is no automatic pending reconciliation. The App lease is not a global VMware lock.

Other builds, automatic addresses, connected/multiple NICs, missing keys, generated/copied identities and multi-field restoration are not admitted. Experiment plans remain unexecuted previews of intent, risks and gaps; their export does not generate identities or authorize application.

## Validate three stages

1. In Comparison → Identity validation, select MAC or BIOS UUID and one session UUID. Capture private before/after/restored snapshots in the intended Windows system, not on the host for a guest.
2. Confirm the private-data prompt and choose a new JSON file. Use the same interface GUID in every phase when selecting one NIC. Missing selected interfaces fail without fallback; default multiple addresses remain incomparable.
3. Import all three snapshots and matching apply/restore records. Configuration, Windows observations and VMIL evidence remain separate. Mismatched sessions/scopes, failed observations, missing records and old redacted identifiers cannot establish raw-value equality.
4. Export redacted JSON/HTML conclusions to new files. Raw snapshots, records and paths are not embedded. Rejected replacement inputs clear earlier conclusions.

Private CLI capture uses `--capture-private-validation --session SESSION-GUID --phase before --output .\VMIL-PRIVATE-before.json`, optionally `--adapter INTERFACE-GUID`. Replace placeholders with valid UUIDs. Phases are before/after/restored; keep one session and adapter scope. These snapshots contain real MAC/UUID and recovery records contain paths. Keep them in private storage, out of Git, public issues and attachments. Normal diagnostics do not create private snapshots; managed-memory secure erasure is not guaranteed.

## Compatibility and security

Windows10/11 x64 targets are assessed by edition/build, with no project host-OS version gate. Vendor support and tested compatibility are separate. Earlier [M11](V03-M11-APP-ACCEPTANCE.md)/[M12](V03-M12-REAL-ACCEPTANCE.md) module packages passed finite checks on an existing Windows10 Education build18363 copy; they do not certify this candidate, final CI bytes, connectivity or a fresh OS. See [compatibility](COMPATIBILITY.md).

Processing is local with no telemetry or automatic upload. Diagnostic/validation exports are redacted; explicit private snapshots and recovery records remain sensitive. Other VMware evidence remains after identity changes; physical-machine classification is not guaranteed. Do not evade anti-cheat, anti-fraud, bans, licensing or security protections. Submit vulnerabilities through the [private form](https://github.com/Aleka579/VMIL/security/advisories/new), maintained by Daniel Waltz. If Defender blocks an operation, stop and preserve the alert; do not disable protection or add exclusions.
