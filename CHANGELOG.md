# Changelog

All notable changes to CMClientCenter are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project aims to follow [Semantic Versioning](https://semver.org/)
once past 1.0.0 (pre-1.0 minor bumps may still contain breaking changes).

## [Unreleased]

## [1.0.0] — first public release

### Added

- **Dashboard** — CM Agent version, Site Code, Management Point, Cache,
  Hardware overview
- **Agent Status** — grouped health checks (Service, Client, Network, Cache,
  Inventory, Updates, System)
- **Hardware** — System, CPU, RAM slots, GPU, Disks, OS incl. full UBR build
  number
- **Software** — installed applications with filter and install date
- **Actions** — 13 standard CM client schedule triggers plus a collapsible
  "Advanced" section with 13 further troubleshooting schedules
- **Software Center** — Applications (Install/Repair/Uninstall) and
  Operating Systems (Task Sequences, incl. OSD with a high-impact
  confirmation dialog)
- **Updates** — All/Pending Updates with per-update Install action
- **Tools** — CCM cache clearing, client repair/reinstall, policy reset,
  pending reboot handling
- **Console** — interactive remote PowerShell session (pass-through
  Kerberos/NTLM) plus a built-in "Run PS" script library (~70 scripts,
  reorganized from the original Client Center for Configuration Manager
  project) and support for your own custom scripts folder
- **Logs** — CMTrace-format log viewer with filtering, color-coded
  severity, and per-field/line copy support
- **Settings** — Light/Dark/System theme, custom scripts folder location,
  and an About section showing app version + git commit hash
- **Installer** — WiX v5 MSI (`Program Files\snowmountainer\CMClientCenter`,
  All Users Start Menu shortcut, silent-install support for
  Intune/MECM/GPO deployment) alongside the existing portable ZIP

### Fixed

- WinRM nested-array serialization issue in the Tools page's Applications
  section (dedicated single-purpose script invocation instead of a
  multi-property `PSCustomObject` return)

### Known limitations

- CCM Cache, CCM Client, and Reboot sections of the Tools page may still be
  affected by the same WinRM nested-array serialization issue that was
  fixed for Applications — tracked for a follow-up release
- No automated update check — reinstalling a newer release's MSI/ZIP is a
  manual step for now
- Test coverage is minimal; the test project scaffolding is in place but
  most of the app is currently verified through manual testing against a
  real MECM environment (see `PSScripts-Testprotokoll.md`)

[Unreleased]: https://github.com/snowmountainer/CMClientCenter/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/snowmountainer/CMClientCenter/releases/tag/v1.0.0
