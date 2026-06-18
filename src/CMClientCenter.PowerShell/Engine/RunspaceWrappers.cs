using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using CMClientCenter.Shared.Results;
using Microsoft.Extensions.Logging;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;

namespace CMClientCenter.PowerShell.Engine;

public interface IRunspaceWrapper
{
    Runspace Runspace { get; }
    bool IsOpen { get; }
    Task<Result<RunspaceInitResult>> OpenAsync(CancellationToken ct = default);
    Task CloseAsync();
}

// ─── Local Runspace ────────────────────────────────────────────────────────

public class LocalRunspace(ILogger logger) : IRunspaceWrapper
{
    private Runspace? _runspace;
    public Runspace Runspace => _runspace ?? throw new InvalidOperationException("Runspace not open");
    public bool IsOpen => _runspace?.RunspaceStateInfo.State == RunspaceState.Opened;

    public async Task<Result<RunspaceInitResult>> OpenAsync(CancellationToken ct = default)
    {
        try
        {
            _runspace = RunspaceFactory.CreateRunspace();
            _runspace.Open();
            string? osVer = null, psVer = null;
            try { (osVer, psVer) = await QueryVersionsAsync(ct); }
            catch (Exception ex) { logger.LogWarning(ex, "Version query failed (non-fatal)"); }
            return Result<RunspaceInitResult>.Success(new RunspaceInitResult(osVer, psVer));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to open local runspace");
            return Result<RunspaceInitResult>.Failure($"Lokaler Runspace: {ex.Message}", ex);
        }
    }

    public Task CloseAsync()
    {
        try { _runspace?.Close(); _runspace?.Dispose(); } catch { }
        _runspace = null;
        return Task.CompletedTask;
    }

    private async Task<(string? os, string? ps)> QueryVersionsAsync(CancellationToken ct)
    {
        using var ps = System.Management.Automation.PowerShell.Create();
        ps.Runspace = _runspace;
        ps.AddScript("[System.Environment]::OSVersion.VersionString");
        ps.AddStatement().AddScript("$PSVersionTable.PSVersion.ToString()");
        var r = await ps.InvokeAsync().WaitAsync(ct);
        return r.Count >= 2 ? (r[0]?.ToString(), r[1]?.ToString()) : (null, null);
    }
}

// ─── Remote Runspace (WinRM) ───────────────────────────────────────────────

public class RemoteRunspace(TargetComputer target, string? password, ILogger logger) : IRunspaceWrapper
{
    private Runspace? _runspace;
    public Runspace Runspace => _runspace ?? throw new InvalidOperationException("Runspace not open");
    public bool IsOpen => _runspace?.RunspaceStateInfo.State == RunspaceState.Opened;

    public async Task<Result<RunspaceInitResult>> OpenAsync(CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Opening WinRM connection to {Host}", target.Hostname);

            // Check TrustedHosts and add the host if needed (local PS runspace)
            await EnsureTrustedHostAsync(target.Hostname, ct);

            var info = BuildConnectionInfo();
            _runspace = RunspaceFactory.CreateRunspace(info);
            await Task.Run(() => _runspace.Open(), ct);

            string? osVer = null, psVer = null;
            try { (osVer, psVer) = await QueryVersionsAsync(ct); }
            catch (Exception ex) { logger.LogWarning(ex, "Version query failed (non-fatal)"); }

            return Result<RunspaceInitResult>.Success(new RunspaceInitResult(osVer, psVer));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "WinRM connection to {Host} failed", target.Hostname);
            return Result<RunspaceInitResult>.Failure(BuildErrorMessage(ex), ex);
        }
    }

    public Task CloseAsync()
    {
        try { _runspace?.Close(); _runspace?.Dispose(); } catch { }
        _runspace = null;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds the host to WSMan:\localhost\Client\TrustedHosts if not already present.
    /// Requires local admin — same prerequisite as the original Client Center.
    /// </summary>
    private static async Task EnsureTrustedHostAsync(string hostname, CancellationToken ct)
    {
        try
        {
            using var localPs = System.Management.Automation.PowerShell.Create();
            localPs.AddScript($@"
                $current = (Get-Item WSMan:\localhost\Client\TrustedHosts).Value
                $host    = '{hostname}'
                if ($current -eq '*') {{ return }}
                $hosts = $current -split ',' | ForEach-Object {{ $_.Trim() }} | Where-Object {{ $_ -ne '' }}
                if ($hosts -notcontains $host) {{
                    $newVal = if ($hosts.Count -gt 0) {{ ($hosts + $host) -join ',' }} else {{ $host }}
                    Set-Item WSMan:\localhost\Client\TrustedHosts -Value $newVal -Force
                }}
            ");
            await localPs.InvokeAsync().WaitAsync(ct);
        }
        catch
        {
            // Non-critical — attempt the connection anyway
        }
    }

    private WSManConnectionInfo BuildConnectionInfo()
    {
        var uri = new Uri($"http://{target.Hostname}:5985/wsman");

        var info = new WSManConnectionInfo(
            uri,
            "http://schemas.microsoft.com/powershell/Microsoft.PowerShell",
            credential: null
        );

        if (!string.IsNullOrEmpty(target.Username) && !string.IsNullOrEmpty(password))
        {
            var secure = new SecureString();
            foreach (var c in password) secure.AppendChar(c);
            secure.MakeReadOnly();
            info.Credential = new PSCredential(target.Username, secure);
        }

        info.AuthenticationMechanism = AuthenticationMechanism.Negotiate;
        info.OpenTimeout              = 15_000;
        info.OperationTimeout         = 120_000;
        return info;
    }

    private string BuildErrorMessage(Exception ex)
    {
        var msg = ex.Message;
        if (msg.Contains("TrustedHosts") || msg.Contains("implicit credentials"))
            return $"TrustedHosts: Please run this once in PowerShell (Admin):\n" +
                   $"Set-Item WSMan:\\localhost\\Client\\TrustedHosts -Value '*' -Force";
        if (msg.Contains("0x80090322") || msg.Contains("target principal"))
            return $"Kerberos error for '{target.Hostname}' — use FQDN instead of IP.";
        if (msg.Contains("AccessDenied") || msg.Contains("Access is denied"))
            return $"Access denied to '{target.Hostname}' — check admin rights.";
        if (msg.Contains("No such host") || msg.Contains("0x80338126"))
            return $"WinRM unreachable on '{target.Hostname}'.";
        return $"Connection to '{target.Hostname}' failed: {msg}";
    }

    private async Task<(string? os, string? ps)> QueryVersionsAsync(CancellationToken ct)
    {
        using var ps = System.Management.Automation.PowerShell.Create();
        ps.Runspace = _runspace;
        ps.AddScript("[System.Environment]::OSVersion.VersionString");
        ps.AddStatement().AddScript("$PSVersionTable.PSVersion.ToString()");
        var r = await ps.InvokeAsync().WaitAsync(ct);
        return r.Count >= 2 ? (r[0]?.ToString(), r[1]?.ToString()) : (null, null);
    }
}
