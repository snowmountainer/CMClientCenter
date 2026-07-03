# Source & License

The scripts in this folder (and its subfolders `Actions`, `Repair`, `Info`, `Install`) were
originally based on the **"Client Center for Configuration Manager"**
project by Roger Zander:

  https://github.com/rzander/sccmclictr
  (Plugins/Plugin_PSScripts/PSScripts)

That project is distributed under the **Microsoft Public License (Ms-PL)**,
which permits reproduction, modification, and redistribution. As of this
modernization pass (2026, targeting Windows 11 + MECM 2509, PowerShell 5.1),
every script here has been rewritten — corrected logic bugs, removed
Windows-XP/7/8.1-era workarounds, removed risky destructive behavior, and
applied a consistent style — rather than carrying forward the original
source verbatim. The full Ms-PL license text is reproduced below, as the
license requires for any reuse of the original project regardless of how
much the code has since changed.

These scripts ship as **built-in** examples for CMClientCenter's "Console →
Run PS" feature, read-only and separate from the user's own custom scripts
folder (`%LOCALAPPDATA%\CMClientCenter\Scripts`). Feel free to add, remove,
or edit files here — they're plain text and not compiled — but note that an
app update may overwrite this folder's contents.

Three scripts that originally lived under a `SCCM-DP` subfolder here
(LEDBAT check, DP content validation, WSUS service check) were moved out to
`..\PSScripts-SiteServer\` and renamed to `Set-LedbatCongestionControl.ps1`,
`Invoke-DpContentValidation.ps1`, and `Repair-WsusServices.ps1`. They target
Distribution Point / Site Server services (WSUS, IIS, the WID database)
rather than the managed client, so they don't belong in a client-facing
"Run PS" list and aren't shipped (no `<Content Include>` entry in the
`.csproj` references that folder). See that folder's own
`LICENSE-and-SOURCE.md` for the same Ms-PL attribution.

---

## Microsoft Public License (Ms-PL)

This license governs use of the accompanying software. If you use the
software, you accept this license. If you do not accept the license, do not
use the software.

### 1. Definitions

The terms "reproduce," "reproduction," "derivative works," and
"distribution" have the same meaning here as under U.S. copyright law.

A "contribution" is the original software, or any additions or changes to
the software.

A "contributor" is any person that distributes its contribution under this
license.

"Licensed patents" are a contributor's patent claims that read directly on
its contribution.

### 2. Grant of Rights

(A) Copyright Grant — Subject to the terms of this license, including the
license conditions and limitations in section 3, each contributor grants
you a non-exclusive, worldwide, royalty-free copyright license to reproduce
its contribution, prepare derivative works of its contribution, and
distribute its contribution or any derivative works that you create.

(B) Patent Grant — Subject to the terms of this license, including the
license conditions and limitations in section 3, each contributor grants
you a non-exclusive, worldwide, royalty-free license under its licensed
patents to make, have made, use, sell, offer for sale, import, and/or
otherwise dispose of its contribution in the software or derivative works
of the contribution in the software.

### 3. Conditions and Limitations

(A) No Trademark License — This license does not grant you rights to use
any contributors' name, logo, or trademarks.

(B) If you bring a patent claim against any contributor over patents that
you claim are infringed by the software, your patent license from such
contributor to the software ends automatically.

(C) If you distribute any portion of the software, you must retain all
copyright, patent, trademark, and attribution notices that are present in
the software.

(D) If you distribute any portion of the software in source code form, you
may do so only under this license by including a complete copy of this
license with your distribution. If you distribute any portion of the
software in compiled or object code form, you may only do so under a
license that complies with this license.

(E) The software is licensed "as-is." You bear the risk of using it. The
contributors give no express warranties, guarantees, or conditions. You may
have additional consumer rights under your local laws which this license
cannot change. To the extent permitted under your local laws, the
contributors exclude the implied warranties of merchantability, fitness for
a particular purpose and non-infringement.
