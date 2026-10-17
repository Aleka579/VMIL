# VM Identity Lab

Local Windows diagnostics for virtualization evidence, VMware configuration declarations, and report comparison.

Current source version: **0.2.0-alpha.1**. Actual App apply, history after restart and parameter-only restore passed: configuration acceptance is 6/6, overall readiness is 37/37. The 2026-10-17 Release build, 127 unit tests, 100 simulated integration tests and 16 latest-package guest checks passed. [GitHub Actions](https://github.com/Aleka579/VMIL/actions/runs/39024064418) also passed on the hosted Windows runner, and private vulnerability reporting is enabled. The official API policy accepts only registered version 17.5.2.23775571, hardware 19, two cores per socket, and 4096↔6144 MiB / 2↔4 vCPU. State/content conflicts and unresolved-operation preservation were verified; the App lease is not a global VMware lock. Source is public; no GitHub Release has been published. See [results and limits](docs/V02-FULL-TEST-RESULTS-2026-10-17.md) and [GitHub validation](docs/GITHUB-READINESS-2026-10-17.md).

The existing Alpha ZIP at `artifacts/releases/VMIdentityLab-0.2.0-alpha.1-win-x64.zip` retains source 9cfa28a and its [read-only acceptance scope](docs/V02-ACCEPTANCE-RESULTS-2026-10-14.md); it does not include the latest backend. The accepted local validation ZIP is `artifacts/test-packages/VMIdentityLab-5de4ec4-validation-20261017.zip`, with a checksum. Historical packages remain unchanged; no public Release has been created.

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

## Validation and support

All 25 v0.1 requirements, six limited v0.2 configuration requirements and six release preparation requirements passed; overall readiness is 37/37. The latest local App passed actual apply, history after restart, restore, and independent post-boot CPU/memory checks. Its Windows 10 Education x64 guest, Build 18363, passed ordinary-user CLI/GUI collection (14/14), Chinese/English UI, real report comparison/export and runtime checks. Standard directories, PATH and x64/x86 registration showed no system dotnet; all four loaded .NET runtime modules came from the package. Existing VC++ packages and other software remain, so this is not a fresh OS. The independent test copy was deleted, temporary test processes ended, and the original VM remains suspended at the user's final request. Remote CI and the private security channel are verified. CI artifacts have their own build identity and have not undergone guest acceptance; no GitHub Release has been published. Report security issues through the [private reporting form](https://github.com/Aleka579/VMIL/security/advisories/new), maintained by Daniel Waltz. See [latest results and limits](docs/V02-FULL-TEST-RESULTS-2026-10-17.md), [release readiness](RELEASE-READINESS.md), and [compatibility](docs/COMPATIBILITY.md). Historical packages and their evidence retain their original versions and scope.

Host OS versions have no project version or lifecycle gate. Guest and clean targets may use Windows 10/11 x64, with edition/build-specific validation. The user-defined VMware Workstation Pro minimum is 17.5.2; the separate MCP test tool requires Python >=3.14. Other SDKs, dependencies and CI retain the maintenance policy. Official vendor support and project-tested compatibility are recorded separately. See the complete [dependency requirements](docs/DEPENDENCIES.md).

Local VMware/vmrun metadata is 17.5.2 build-23775571; bundled vmrest reports 1.3.1 build-23775571. Project-local Python3.14.8, MCP SDK1.30.0, a dependency lock and seven read-only MCP tools have been configured. Native vmrun with a temporary local credential session completed guest authentication, execution and file transfer. No additional MCP methods were enabled. The separate application uses the .NET HTTP client for its optional local API queries; credentials are entered locally for each request and are not saved in application settings or reports. Test processes, the credential session, and the temporary API have ended. See [environment setup](docs/ENVIRONMENT-SETUP.md) and [reviewed VMware evidence](docs/VMWARE-17.5.2-EVIDENCE.md). Existing Alpha ZIPs retain their original snapshot. The application uses no telemetry or automatic report upload.

Original source is licensed under [MIT](LICENSE), Copyright (c) 2026 Daniel Waltz, as explicitly confirmed by the owner. The private security reporting channel is not yet configured. See [license decision](docs/LICENSE-DECISION.md), [third-party notices](THIRD-PARTY-NOTICES.md), and [security policy](SECURITY.md). Public repository creation, pushes, and releases require separate authorization.
