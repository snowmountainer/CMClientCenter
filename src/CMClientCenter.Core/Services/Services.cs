using CMClientCenter.Core.Interfaces;
using CMClientCenter.Core.Models;
using CMClientCenter.Shared.Enums;
using CMClientCenter.Shared.Results;
using Microsoft.Extensions.Logging;

namespace CMClientCenter.Core.Services;

public class CMAgentService(IExecutorService<CMAgentInfo> executor, ILogger<CMAgentService> logger)
    : ICMAgentService
{
    public async Task<Result<CMAgentInfo>> GetAgentInfoAsync(CancellationToken ct = default)
    {
        try { return await executor.ExecuteAsync(ct); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve CM Agent info");
            return Result<CMAgentInfo>.Failure(ex.Message, ex);
        }
    }
}

public class HardwareService(IExecutorService<HardwareInfo> executor, ILogger<HardwareService> logger)
    : IHardwareService
{
    public async Task<Result<HardwareInfo>> GetHardwareInfoAsync(CancellationToken ct = default)
    {
        try { return await executor.ExecuteAsync(ct); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve hardware info");
            return Result<HardwareInfo>.Failure(ex.Message, ex);
        }
    }
}

public class SoftwareService(IExecutorService<List<SoftwareItem>> executor, ILogger<SoftwareService> logger)
    : ISoftwareService
{
    public async Task<Result<List<SoftwareItem>>> GetInstalledSoftwareAsync(CancellationToken ct = default)
    {
        try { return await executor.ExecuteAsync(ct); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve software inventory");
            return Result<List<SoftwareItem>>.Failure(ex.Message, ex);
        }
    }
}

public class ActionService(IActionExecutorService executor, ILogger<ActionService> logger)
    : IActionService
{
    public IReadOnlyList<CMAction> GetAvailableActions() => CMAction.AllActions;

    public async Task<Result> TriggerActionAsync(CMActionType action, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Triggering CM action: {Action}", action);
            return await executor.TriggerAsync(action, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to trigger CM action {Action}", action);
            return Result.Failure(ex.Message, ex);
        }
    }
}

public class AgentHealthService(IHealthExecutorService executor, ILogger<AgentHealthService> logger)
    : IAgentHealthService
{
    public async Task<Result<List<HealthCheck>>> GetHealthChecksAsync(CancellationToken ct = default)
    {
        try { return await executor.ExecuteAsync(ct); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get health checks");
            return Result<List<HealthCheck>>.Failure(ex.Message, ex);
        }
    }
}

