using System.Diagnostics;
using System.Text;
using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using CMClientCenter.PowerShell.Engine;
using CMClientCenter.Shared.Results;
using Microsoft.Extensions.Logging;

namespace CMClientCenter.PowerShell.Executors;

// Backs the "Console" page — the equivalent of the old "Client Center for
// Configuration Manager" tool's "Open Console" and "Run PS" buttons:
//   - OpenConsole spawns a *separate* interactive powershell.exe window
//     running Enter-PSSession against the connected host.
//   - GetCustomScriptsAsync / RunCustomScriptAsync let the user drop their
//     own .ps1 files into a folder and run them against whichever target
//     (local or remote) is already connected via the app's own runspace —
//     no second process, no separate credentials.
public class ConsoleExecutor(
    RunspaceManager runspace,
    IConnectionService connectionService,
    IAppSettingsService settingsService,
    ILogger<ConsoleExecutor> logger) : IConsoleService
{
    public Result OpenConsole(string hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname))
            return Result.Failure("No host specified.");

        try
        {
            var isLocal = hostname.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                          hostname == "127.0.0.1" ||
                          hostname.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);

            // -NoExit keeps the window open after Enter-PSSession ends (e.g. on
            // error) so the user can see what happened instead of a window that
            // flashes shut. Pass-through Kerberos/NTLM (current Windows identity) —
            // same auth model the app's own WinRM runspace uses when no explicit
            // credential was supplied (see RemoteRunspace.BuildConnectionInfo).
            // Single quotes inside the hostname are escaped ('' ) since the
            // hostname is embedded in a single-quoted PowerShell string literal.
            var escapedHost = hostname.Replace("'", "''");
            var command = isLocal
                ? "Write-Host 'Already local — opening a normal PowerShell session.' -ForegroundColor Yellow"
                : $"Enter-PSSession -ComputerName '{escapedHost}' -Authentication Negotiate";

            var psi = new ProcessStartInfo
            {
                FileName        = "powershell.exe",
                UseShellExecute = true
            };
            psi.ArgumentList.Add("-NoExit");
            psi.ArgumentList.Add("-Command");
            psi.ArgumentList.Add(command);

            logger.LogInformation("Opening interactive console to {Host}", hostname);
            Process.Start(psi);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to open console for {Host}", hostname);
            return Result.Failure($"Could not open console: {ex.Message}", ex);
        }
    }

    // Scripts shipped with the app (PSScripts\ next to the .exe — see
    // CMClientCenter.App.csproj's <Content Include="PSScripts\**\*.ps1">).
    // AppContext.BaseDirectory is the app's own folder regardless of how/where
    // it was launched from — the correct anchor for an unpackaged, self-contained
    // deployment (no installed-package root to resolve against).
    public static string BuiltinScriptsFolder =>
        Path.Combine(AppContext.BaseDirectory, "PSScripts");

    public Task<Result<List<CustomScriptInfo>>> GetCustomScriptsAsync(CancellationToken ct = default)
    {
        try
        {
            var scripts = new List<CustomScriptInfo>();

            // Built-in first (shipped with the app — see PSScripts/LICENSE-and-SOURCE.md),
            // then the user's own custom folder. Missing built-in folder is not an
            // error (e.g. a dev build that hasn't copied it yet) — just yields no
            // built-in scripts instead of failing the whole list. The custom folder
            // IS created if missing, so it shows up for "Open Folder" right away.
            scripts.AddRange(ScanFolder(BuiltinScriptsFolder, isBuiltin: true, createIfMissing: false));
            scripts.AddRange(ScanFolder(settingsService.EffectiveScriptsFolder, isBuiltin: false, createIfMissing: true));

            return Task.FromResult(Result<List<CustomScriptInfo>>.Success(scripts));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list custom scripts");
            return Task.FromResult(Result<List<CustomScriptInfo>>.Failure(ex.Message, ex));
        }
    }

    private static List<CustomScriptInfo> ScanFolder(string folder, bool isBuiltin, bool createIfMissing)
    {
        if (!Directory.Exists(folder))
        {
            if (!createIfMissing) return [];
            Directory.CreateDirectory(folder);
        }

        // Recursive — mirrors the old "Client Center for Configuration
        // Manager" tool, where scripts could be organized in subfolders
        // for an overview when there are many of them.
        return new DirectoryInfo(folder)
            .GetFiles("*.ps1", SearchOption.AllDirectories)
            .OrderBy(f => GetGroupName(f, folder) == "(Root)" ? "" : GetGroupName(f, folder), StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(f => f.LastWriteTime)
            .Select(f => new CustomScriptInfo(f.Name, f.FullName, f.LastWriteTime, GetGroupName(f, folder), isBuiltin))
            .ToList();
    }

    // Subfolder path relative to the scripts root, used as the group header
    // in the UI. Top-level scripts (directly in the root folder) get a
    // synthetic "(Root)" group so they're not lost among subfolder groups.
    private static string GetGroupName(FileInfo file, string rootFolder)
    {
        var relativeDir = Path.GetRelativePath(rootFolder, file.DirectoryName ?? rootFolder);
        return relativeDir == "." ? "(Root)" : relativeDir;
    }

    public async Task<Result<string>> RunCustomScriptAsync(string scriptPath, CancellationToken ct = default)
    {
        if (!connectionService.IsConnected)
            return Result<string>.Failure("Not connected to a target.");

        if (!File.Exists(scriptPath))
            return Result<string>.Failure($"Script not found: {scriptPath}");

        try
        {
            // PS 5.1-compatible content expected (same constraint as the
            // app's embedded scripts) — read as-is, no transformation.
            var scriptContent = await File.ReadAllTextAsync(scriptPath, ct);

            var (output, errors) = await runspace.InvokeRawAsync(scriptContent, ct);

            var sb = new StringBuilder();
            foreach (var r in output)
            {
                if (r is null) continue;
                sb.AppendLine(r.ToString());
            }
            foreach (var err in errors)
                sb.AppendLine($"ERROR: {err}");

            return Result<string>.Success(sb.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to run custom script {Path}", scriptPath);
            return Result<string>.Failure(ex.Message, ex);
        }
    }
}
