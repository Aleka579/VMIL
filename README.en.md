# VM Identity Lab

Local Windows diagnostics for virtualization evidence, VMware configuration declarations, and report comparison.

Current source is **0.3.0-alpha.1, a candidate**, public commit [26656db](https://github.com/Aleka579/VMIL/tree/codex/v03-m13-ci-20261021). [CI39616530060](https://github.com/Aleka579/VMIL/actions/runs/39616530060) passed 240 unit and 206 simulated integration tests with zero build warnings/errors. The exact original ZIP passed 14 CLI/PNG and 43 bilingual normal-App checks on an ordinary-user Windows11 build26100 host, plus [28/50 checks on a Windows10 Education build18363 x64 guest](docs/V03-M13-CI-GUEST-ACCEPTANCE.md). All 413 product files and the original ZIP remained unchanged; test files and credentials were cleaned up. M13 is 4/5, with prerelease publication and server receipts pending. The published [0.2.0-alpha.1 baseline](https://github.com/Aleka579/VMIL/releases/tag/v0.2.0-alpha.1) retains its own scope. Fresh-OS, other-build, topology and connectivity claims remain unverified.

The [v0.3 plan](docs/V03-PLAN.md) and [M08 feasibility report](docs/V03-M08-FEASIBILITY.md) are available in Simplified Chinese. [M09 read-only identity analysis](docs/V03-M09-RESULTS.md) and acceptance for the declared environments are complete: ordinary-user 14/17-item CLI, bilingual GUI, [actual CI](https://github.com/Aleka579/VMIL/actions/runs/39196591655) with 140 unit / 113 simulated integration tests, original-package host 9/9 and existing Win10 guest 28/28 passed. The [acceptance record](docs/V03-M09-CI-GUEST-ACCEPTANCE.md) keeps the first PNG failure, local fix, final original package and cleanup separate. UUID/MAC/BIOS serial collection is opt-in; results remain redacted and schema 1.0 is retained. M09 remains unpublished and is absent from the e27543f Release assets. Fresh-OS testing remains unverified; M09 itself added no identity writes.

[M10 experiment planning](docs/V03-M10-RESULTS.md) originally passed5/5 module criteria and local synthetic acceptance: Release with no warnings/errors,164 unit/115 simulated integration tests and20 ordinary-user bilingual GUI checks. It previews MAC uniqueness and UUID copy-lifecycle intent, evidence gaps, risks and recovery with redacted JSON export. Plans remain non-executable and explicitly unexecuted; context changes invalidate review. M10 is now included in public26656db and its new CI, while remaining absent from e27543f assets. Its original module-stage records correctly retain the absence of guest/remote CI testing at that time.

[M11 App acceptance and finite admission](docs/V03-M11-APP-ACCEPTANCE.md) passed 5/5 criteria: existing static MAC/BIOS UUID application, history reopening, guest checks, confirmed restoration and original-value checks; 75/75 guest and 36/36 receipt checks passed. Default policy admits one existing field at a time only on Workstation17.5.2.23775571/hardware19 with both valid identities and one static host-only NIC disconnected at startup. Reliable power-off, confirmation, backup, durable intent and independent verification remain required. That run passed 199 unit / 192 integration tests and eight bilingual normal-App checks; its copy was deleted and original VM restored to suspended. M12 progress is below; no new remote CI or Release is claimed.

[M12 three-stage validation](docs/V03-M12-REAL-ACCEPTANCE.md) passed 5/5 criteria within the tested scope; the copy was deleted and the original VM restored to suspended. Explicit private MAC/UUID snapshots support an optional network interface GUID. Configuration records, Windows observations and VMIL classification are presented separately, with redacted JSON/HTML exports. 237 unit / 200 integration tests, 150 real guest snapshot checks, 36 receipt checks, and 34 bilingual UI/export checks on each of the host and Windows 10 guest passed. VMware evidence remains present and network connectivity is untested. Keep private snapshots out of public repositories; legacy identifiers remain incomparable, M11 write scope is unchanged; M13 local candidate, CI and target acceptance are recorded separately.

[Official evidence](docs/V03-M11-MAC-UUID-EVIDENCE.md), [exact-build research](docs/V03-M11-REAL-API-RETEST.md) and App/guest acceptance retain separate scopes. The [configuration policy](docs/VMWARE-CONFIG-POLICY.md) defines formats and restoration limits. Other builds, automatic addresses, connected/multiple NICs, absent-key restoration and vendor generation/copy workflows remain unapproved. Other virtualization evidence remains after MAC/UUID changes; the project does not guarantee physical-machine classification by third-party software.

The historical Alpha ZIP at `artifacts/releases/VMIdentityLab-0.2.0-alpha.1-win-x64.zip` retains source 9cfa28a and its [read-only acceptance scope](docs/V02-ACCEPTANCE-RESULTS-2026-10-14.md). The accepted local validation ZIP is `artifacts/test-packages/VMIdentityLab-5de4ec4-validation-20261017.zip`. Both retain their original scope and are separate from the published e27543f CI package.

Existing v0.1 package: `artifacts/releases/VMIdentityLab-0.1.0-alpha.2-win-x64.zip`, with a `.zip.sha256` checksum. Its folder is `artifacts/publish/0.1.0-alpha.2-win-x64/`, and `BUILD-INFO.json` records ab177a7. Earlier packages retain their original contents and versions.

## Run

Extract the complete Windows x64 package and run `VMIdentityLab.App.exe`. The self-contained package includes its runtime; keep the full folder together.

The five pages provide overview, detection details, VMX configuration, report comparison, and redacted JSON/static HTML export. Switch between Chinese and English in the sidebar. Run the same collector on a guest and manually import its JSON report on the host. VMX inspection optionally connects to an already configured local official VMware API for power-state queries; unconfigured connections remain unknown. Limited configuration changes require a fresh preview, explicit confirmation, a powered-off VM and a separate request-scoped API connection. The App creates and verifies backups before updating through the official API; it does not control VM power.

```powershell
.\VMIdentityLab.App.exe --collect --output .\guest-report.json --html .\guest-report.html
```

CLI exports refuse to overwrite existing files. GUI exports ask for confirmation. VMX declarations are separate from guest observations; no configuration changes are available in v0.1.

## Build and test

Use the stable .NET 10 SDK selected by `global.json`.

```powershell
dotnet build VMIdentityLab.sln -c Release -m:1 --disable-build-servers
dotnet test --project tests/VMIdentityLab.UnitTests/VMIdentityLab.UnitTests.csproj -c Release --parallel none
dotnet test --project tests/VMIdentityLab.IntegrationTests/VMIdentityLab.IntegrationTests.csproj -c Release --parallel none
dotnet publish src/VMIdentityLab.App/VMIdentityLab.App.csproj -c Release -r win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o artifacts/publish/win-x64
```

Set `DOTNET_CLI_TELEMETRY_OPTOUT=1` and `TESTINGPLATFORM_TELEMETRY_OPTOUT=1` when verifying. Tests use synthetic inputs and independently created temporary files; they do not operate real VMs.

See the [English user guide](docs/USER-GUIDE.en.md) for finite identity changes, recovery and private three-stage snapshots. Current candidate status is in [readiness](RELEASE-READINESS.md); the following v0.1/v0.2 counts retain their historical scope.

## Validation and support

All 25 v0.1 requirements, six limited v0.2 configuration requirements and six release preparation requirements passed; overall readiness is 37/37. The original e27543f CI package passed strict 23/23 checks on Windows 10 Education x64, Build 18363, including ordinary-user CLI/GUI collection (14/14), independent observations and runtime checks. It uses SDK10.0.402/Runtime10.0.13 and needs no system SDK or .NET on the tested target. Existing VC++ packages and other software remain, so this is not a fresh OS; Windows 11 and other builds remain unverified. Earlier finite configuration tests retain their exact version/value scope. The independent test copy was deleted; the guest acceptance session ended with the original VM running at the user's request. This M08 research did not query its current state. Report security issues through the enabled [private reporting form](https://github.com/Aleka579/VMIL/security/advisories/new), maintained by Daniel Waltz. See [CPU acceptance](docs/CPU-FIX-ACCEPTANCE-2026-10-18.md), [release readiness](RELEASE-READINESS.md), and [compatibility](docs/COMPATIBILITY.md). Historical packages and evidence retain their original scope.

Host OS versions have no project version or lifecycle gate. Guest and clean targets may use Windows 10/11 x64, with edition/build-specific validation. The user-defined VMware Workstation Pro minimum is 17.5.2; the separate MCP test tool requires Python >=3.14. Other SDKs, dependencies and CI retain the maintenance policy. Official vendor support and project-tested compatibility are recorded separately. See the complete [dependency requirements](docs/DEPENDENCIES.md).

Local VMware/vmrun metadata is 17.5.2 build-23775571; bundled vmrest reports 1.3.1 build-23775571. Project-local Python3.14.8, MCP SDK1.30.0, a dependency lock and seven read-only MCP tools have been configured. Native vmrun with a temporary local credential session completed guest authentication, execution and file transfer. No additional MCP methods were enabled. The separate application uses the .NET HTTP client for its optional local API queries; credentials are entered locally for each request and are not saved in application settings or reports. Test processes, the credential session, and the temporary API have ended. See [environment setup](docs/ENVIRONMENT-SETUP.md) and [reviewed VMware evidence](docs/VMWARE-17.5.2-EVIDENCE.md). Existing Alpha ZIPs retain their original snapshot. The application uses no telemetry or automatic report upload.

Original source is licensed under [MIT](LICENSE), Copyright (c) 2026 Daniel Waltz, as explicitly confirmed by the owner. The private security reporting channel is enabled. See [license decision](docs/LICENSE-DECISION.md), [third-party notices](THIRD-PARTY-NOTICES.md), and [security policy](SECURITY.md). New remote pushes and releases require authorization for their specific contents.
