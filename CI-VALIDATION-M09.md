# M09 CI validation branch

This branch validates unpublished read-only identity analysis and the PNG preview fix.
The application version is still 0.2.0-alpha.1; it is not a new Release or the published v0.2 package.

- Optional UUID, BIOS serial and MAC collection is off by default and stays redacted.
- Configuration declarations and system observations remain separate.
- No new identity writes are approved; the existing CPU/memory policy is unchanged.
- Local Release, 140 unit tests and 113 synthetic integration tests passed.
- 24 local PNG cases passed pixel checks (two languages, six views, two cold processes).
- Guest, clean-OS and new remote CI results remain pending until actually executed.
- Preview example: --render-preview --output new.png --language en-US --page 1 --preview-tab identity.
- Development-only pixel check: VMIdentityLab.DesktopTest verify-preview new.png identity.

This source-only export contains no private logs, real reports, credentials, VM data or test ZIPs.
Windows CI builds, tests and retains a self-contained App artifact and four matching guest-driver entries.
The driver shares the App runtime during validation and is excluded from the App artifact.
The workflow does not publish a Release.
