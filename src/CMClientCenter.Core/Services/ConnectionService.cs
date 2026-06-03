using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using CMClientCenter.Shared.Enums;
using CMClientCenter.Shared.Results;
using Microsoft.Extensions.Logging;

namespace CMClientCenter.Core.Services;

public class ConnectionService(IRunspaceManagerService runspaceManager, ILogger<ConnectionService> logger)
    : IConnectionService
{
    public bool IsConnected { get; private set; }
    public TargetComputer? CurrentTarget { get; private set; }
    public event EventHandler<ConnectionResult>? ConnectionStateChanged;

    public async Task<Result<ConnectionResult>> ConnectAsync(
        string target,
        string? username = null,
        string? password = null,
        CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Connecting to {Target}", target);

            var computer = new TargetComputer(target) { Username = username };
            var result   = await runspaceManager.InitializeAsync(computer, password, ct);

            if (!result.IsSuccess)
            {
                IsConnected = false;
                var failConn = new ConnectionResult(false, ConnectionMode.AutoDetect, result.ErrorMessage);
                ConnectionStateChanged?.Invoke(this, failConn);
                return Result<ConnectionResult>.Failure(result.ErrorMessage!);
            }

            IsConnected     = true;
            CurrentTarget   = computer;

            var connResult = new ConnectionResult(
                IsConnected: true,
                Mode:        computer.IsLocal ? ConnectionMode.Local : ConnectionMode.Remote,
                OSVersion:   result.Value?.OSVersion,
                PSVersion:   result.Value?.PSVersion
            );

            ConnectionStateChanged?.Invoke(this, connResult);
            return Result<ConnectionResult>.Success(connResult);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Connection to {Target} failed", target);
            IsConnected = false;
            return Result<ConnectionResult>.Failure(ex.Message, ex);
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            await runspaceManager.DisposeAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error during disconnect");
        }
        finally
        {
            IsConnected   = false;
            CurrentTarget = null;
            ConnectionStateChanged?.Invoke(this,
                new ConnectionResult(false, ConnectionMode.AutoDetect));
        }
    }
}
