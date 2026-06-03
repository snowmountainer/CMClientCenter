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
            throw new InvalidOperationException("Kein aktiver Runspace. Bitte zuerst verbinden.");

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

    public async ValueTask DisposeAsync()
    {
        if (_current is not null)
        {
            await _current.CloseAsync();
            _current = null;
        }
    }
}
