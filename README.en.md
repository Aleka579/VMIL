# VM Identity Lab

[简体中文](README.md)

Local Windows virtualization diagnostics, VMware Workstation configuration management, report comparison and three-stage identity validation. Chinese and English UI, ordinary-user operation, redacted exports and no telemetry.

Current source: **1.0.0**. Exact CI, artifact acceptance and publication status are recorded in the [release record](docs/V1-RELEASE.md). Earlier [v0.3 prerelease assets](https://github.com/Aleka579/VMIL/releases/tag/v0.3.0-alpha.1) remain unchanged.

## Features

- Collect SMBIOS, CPU, PnP, VMware Tools and system evidence: 14 default observations, 17 with explicit identity collection.
- Inspect VMX declarations and explain configuration, Windows observations and virtualization evidence separately.
- Compare host/guest reports and export redacted JSON and static HTML.
- Preview, confirm, back up, apply and restore finite settings through the official local VMware API, with persistent history.
- Import private before/after/restored snapshots and matching records for three-stage validation.

Other virtualization evidence remains after MAC/UUID changes. Physical-machine classification by third-party software is not guaranteed. Do not evade anti-cheat, anti-fraud, bans, licensing or security protections.

## Download and run

Download the Windows x64 ZIP and checksums from [Releases](https://github.com/Aleka579/VMIL/releases), extract the complete folder and run `VMIdentityLab.App.exe`. No development SDK, system .NET or Python is required. Keep the included runtime files. `BUILD-INFO.json` identifies version/source/runtime; `PACKAGE-FILES.json` lists file hashes.

```powershell
.\VMIdentityLab.App.exe --collect --output .\report.json --html .\report.html
.\VMIdentityLab.App.exe --collect --include-identifiers --output .\identity.json
```

CLI exports refuse overwrites; GUI replacement requires confirmation. Collect on each intended system and manually import reports. Private identity snapshots contain raw MAC/UUID, and recovery history contains local paths: keep them out of public repositories and issues.

## Compatibility and writes

Read-only collection targets Windows 10/11 x64. Exact v1.0 acceptance is tracked in the [release record](docs/V1-RELEASE.md); older-package results retain their original scope. See the [compatibility matrix](docs/COMPATIBILITY.md) for Windows 10 and encrypted Windows 11 guest evidence, upstream support and unverified environments.

Writes remain restricted to Workstation17.5.2 build23775571/hardware19. Memory/CPU values are4096↔6144MiB and2↔4vCPU, with two cores per socket. Identity operations require the exact validated Workstation/vmrest build, both valid existing identities and one static host-only NIC disconnected at startup; one existing MAC or BIOS UUID per transaction. Other builds, values, topologies, automatic/generated/copied identities and absent keys are not admitted.

Apply and restore require power-off, a fresh preview, explicit confirmation, verified backup, durable intent and API readback. The App does not manage power or replace VMX directly. Configuration backups do not restore the whole VM; the App lease is not a global VMware lock. Preserve unresolved records and do not blindly retry. See the [configuration policy](docs/VMWARE-CONFIG-POLICY.md).

## Build and test

C#14 / .NET10 LTS / WPF / MVVM. `global.json` selects a stable SDK patch; bundled runtimes and collection dependencies use10.0.14.

```powershell
dotnet build VMIdentityLab.sln -c Release -m:1 --disable-build-servers
dotnet test --project tests/VMIdentityLab.UnitTests/VMIdentityLab.UnitTests.csproj -c Release --no-build --parallel none
dotnet test --project tests/VMIdentityLab.IntegrationTests/VMIdentityLab.IntegrationTests.csproj -c Release --no-build --parallel none
```

Set `DOTNET_CLI_TELEMETRY_OPTOUT=1` and `TESTINGPLATFORM_TELEMETRY_OPTOUT=1`. Default tests use synthetic inputs and temporary files. CI and real-guest acceptance are recorded separately.

[User guide](docs/USER-GUIDE.en.md) · [Dependencies](docs/DEPENDENCIES.md) · [Architecture](docs/ARCHITECTURE.md) · [Release record](docs/V1-RELEASE.md) · [Changelog](CHANGELOG.md)

Original code: [MIT](LICENSE), Copyright(c)2026 Daniel Waltz. Retain [third-party notices](THIRD-PARTY-NOTICES.md). Daniel Waltz maintains the [private security reporting channel](https://github.com/Aleka579/VMIL/security/advisories/new). Do not upload credentials, real VMX files or private reports to public issues.
