# Contributing to CMClientCenter

Thanks for considering a contribution — bug reports, feature requests, docs
fixes, and PRs are all welcome.

## Before you start

For anything beyond a small fix (new page, new PSScript category, a
different architecture for something), please open an issue first to
discuss the approach. It's a much smaller time investment for everyone than
a large PR that turns out not to fit the project's direction.

For small, obvious fixes (typo, an actually-broken script, a crash you hit
and fixed), a PR without a prior issue is fine.

## Development setup

**Requires Windows** — this is a WinUI 3 desktop app, it cannot be built or
run on Linux/macOS/WSL.

1. Visual Studio 2022 (17.x) or later, with the **"Windows application
   development"** workload
2. .NET 10 SDK
3. Clone the repo and open `CMClientCenter.sln`

```powershell
dotnet restore CMClientCenter.sln
dotnet build CMClientCenter.sln --configuration Release --arch x64
dotnet run --project src/CMClientCenter.App --arch x64
```

See the [README](README.md#architecture) for the solution structure, and
[CLAUDE.md](CLAUDE.md) for accumulated technical notes — WinUI 3 quirks
that have bitten us before, WMI/CIM findings specific to this project
(e.g. which properties are unreliable or misleadingly named), WinRM
serialization gotchas, and established code patterns. Worth a skim before
you dive into unfamiliar territory; it'll likely save you from re-learning
something the hard way.

## Testing your change

There isn't a full automated test suite yet (see
[README → Tests](README.md#tests) — the scaffolding is there, coverage is
still being built out), so manual verification against a real MECM/SCCM
client matters more here than in most projects:

- If you touch anything that talks to WMI/CIM or the CM client, test
  against a real client, not just that it compiles
- If you add or change a PSScript, run it for real via the Console page's
  "Run PS" and confirm the output is what you'd expect — WinRM has a habit
  of silently mangling nested arrays in returned objects (see CLAUDE.md's
  "WinRM / PowerShell serialization" notes), so "it builds" isn't the same
  as "it works over WinRM"
- If you can add a unit test for the part of your change that doesn't need
  a live client (e.g. parsing, mapping, `Result<T>` logic), please do —
  that's exactly the kind of coverage the test projects are missing right
  now

## Code style

- C# 13 partial-property syntax for ViewModels (`[ObservableProperty]
  public partial T Name { get; set; }`), not the older backing-field style
  — see CLAUDE.md for why
- `Nullable` is enabled on every project; please don't introduce `#nullable
  disable` blocks to work around a warning — fix the actual nullability
- An `.editorconfig` is included and most IDEs will pick it up
  automatically; please don't fight it with per-file overrides

## PSScripts contributions

Scripts under `src/CMClientCenter.App/PSScripts/` are the built-in script
library and originate from the Ms-PL-licensed
[Client Center for Configuration Manager](https://github.com/rzander/sccmclictr)
project (see `PSScripts/LICENSE-and-SOURCE.md`). If you're fixing or adding
to one of these:

- Keep the verb-noun naming convention (`Get-`, `Set-`, `Invoke-`, `Repair-`,
  `Test-`, ...) and the existing folder grouping (`Actions/`, `Repair/`,
  `Info/`, `Install/`)
- Target PowerShell 5.1 compatibility (the app runs scripts in an in-process
  5.1 Runspace) — avoid syntax that only works on PS 7+
- Read-only diagnostic scripts are safest to contribute; anything
  destructive needs extra scrutiny in review

If you're contributing a genuinely new script (not from the original
sccmclictr project), say so in the PR description — it changes the
licensing note that needs to go with it.

## Commit / PR conventions

- Keep PRs focused — one fix or feature per PR is much easier to review
  than a batch of unrelated changes
- Reference the issue you're addressing, if there is one
- A short description of *why*, not just *what*, saves a lot of
  back-and-forth in review

## Questions

Open an issue with your question — no need for a formal proposal first if
you're just trying to understand something before contributing.
