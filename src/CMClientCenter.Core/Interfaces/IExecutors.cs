using CMClientCenter.Core.Models;
using CMClientCenter.Shared.Enums;
using CMClientCenter.Shared.Results;

namespace CMClientCenter.Core.Interfaces;

public interface IExecutorService<T>
{
    Task<Result<T>> ExecuteAsync(CancellationToken ct = default);
}

public interface IActionExecutorService
{
    Task<Result> TriggerAsync(CMActionType action, CancellationToken ct = default);
}

public interface IHealthExecutorService
{
    Task<Result<List<HealthCheck>>> ExecuteAsync(CancellationToken ct = default);
}
