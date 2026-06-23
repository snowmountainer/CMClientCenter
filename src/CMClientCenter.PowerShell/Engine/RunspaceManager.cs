using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using CMClientCenter.Shared.Results;
using Microsoft.Extensions.Logging;

namespace CMClientCenter.PowerShell.Engine;

public class RunspaceManager(ILogger<RunspaceManager> logger)
    : IRunspaceManagerService
{
    private IRunspaceWrapper? _current;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public bool IsReady => _current?.IsOpen ?? false;

    public async Task<Result<RunspaceInitResult>> InitializeAsync(
        TargetComputer target,
        string? password = null,
        CancellationToken ct = default)
    {
        await DisposeAsync();

        logger.LogInformation("Initializing runspace for {Target} (IsLocal={IsLocal})",
            target.Hostname, target.IsLocal);

        IRunspaceWrapper wrapper = target.IsLocal
            ? new LocalRunspace(logger)
            : new RemoteRunspace(target, password, logger);

        var result = await wrapper.OpenAsync(ct);

        if (!result.IsSuccess)
            return Result<RunspaceInitResult>.Failure(result.ErrorMessage!);

        _current = wrapper;
        return result;
    }

    public async Task<List<System.Management.Automation.PSObject>> InvokeAsync(
        string script, CancellationToken ct = default)
    {
        if (_current is null || !_current.IsOpen)
            throw new InvalidOperationException("No active runspace. Please connect first.");

        await _lock.WaitAsync(ct);
        try
        {
            using var ps = System.Management.Automation.PowerShell.Create();
            ps.Runspace = _current.Runspace;
            ps.AddScript(script);
            var results = await ps.InvokeAsync().WaitAsync(ct);

            if (ps.HadErrors)
            {
                var errors = string.Join("; ", ps.Streams.Error.Select(e => e.ToString()));
                logger.LogWarning("PS errors: {Errors}", errors);
            }

            return [.. results];
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Like InvokeAsync, but also surfaces the PowerShell error stream in the
    /// result instead of only logging it — used by the "Console" page's
    /// "Run PS" feature, where arbitrary user-supplied scripts run and any
    /// errors need to be visible to the person who wrote the script, not just
    /// in the app's debug log.
    /// </summary>
    public async Task<(List<System.Management.Automation.PSObject> Output, List<string> Errors)> InvokeRawAsync(
        string script, CancellationToken ct = default)
    {
        if (_current is null || !_current.IsOpen)
            throw new InvalidOperationException("No active runspace. Please connect first.");

        await _lock.WaitAsync(ct);
        try
        {
            using var ps = System.Management.Automation.PowerShell.Create();
            ps.Runspace = _current.Runspace;
            ps.AddScript(script);
            var results = await ps.InvokeAsync().WaitAsync(ct);

            var errors = ps.Streams.Error.Select(e => e.ToString()).ToList();
            if (ps.HadErrors)
                logger.LogWarning("PS errors: {Errors}", string.Join("; ", errors));

            return ([.. results], errors);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_current is not null)
        {
            await _current.CloseAsync();
            _current = null;
        }
    }
}
