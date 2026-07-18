# Security Policy

## Why this matters for CMClientCenter specifically

CMClientCenter runs with **local admin rights** and connects to remote
machines over **WinRM with pass-through Kerberos/NTLM**, typically from an
admin workstation or jump server with broad reach into a Configuration
Manager (MECM/SCCM) environment. A vulnerability here — remote code
execution, credential leakage, a PowerShell injection path via crafted
script/log input, etc. — could have an outsized blast radius compared to a
typical desktop app. Please report responsibly rather than filing a public
issue.

## Reporting a vulnerability

**Please do not open a public GitHub issue for security vulnerabilities.**

Instead, use GitHub's private vulnerability reporting for this repository:

1. Go to the [Security tab](https://github.com/snowmountainer/CMClientCenter/security) of this repository
2. Click **"Report a vulnerability"**

This opens a private advisory visible only to the maintainer and you, and
lets us coordinate a fix and disclosure timeline before any public details
are posted.

If you're unable to use GitHub's private reporting for some reason, open a
regular issue asking for an alternative contact method — without describing
the vulnerability itself — and we'll follow up.

## What to include

Whatever detail helps reproduce and assess the issue, for example:

- Which version (see Settings → About in the app, or the release tag)
- Steps to reproduce, or a minimal PoC
- What you'd expect to happen vs. what actually happens
- Impact as you see it (e.g. "arbitrary command execution on the connected
  client", "credential exposure in logs", ...)

## Scope

In scope:
- The CMClientCenter application itself (`CMClientCenter.App`,
  `.Core`, `.PowerShell`, `.Shared`)
- The WiX installer (`installer/`)
- The built-in PSScripts library shipped with the app (`PSScripts/`)

Out of scope:
- Vulnerabilities in Configuration Manager (MECM/SCCM) itself, Windows, or
  the .NET/Windows App SDK runtime — please report those to Microsoft
  instead
- Issues that require an attacker to already have local admin rights on the
  machine running CMClientCenter (at that point they don't need this app)

## Response expectations

This is a small open-source project maintained in spare time — there's no
SLA, but security reports get priority over regular issues/PRs. Expect an
initial acknowledgment within a few days.

## Supported versions

Only the latest released version is supported with security fixes. Given
the project is still pre-1.1, there's no long-term-support branch at this
time.
