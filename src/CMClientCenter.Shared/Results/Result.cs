namespace CMClientCenter.Shared.Results;

/// <summary>
/// Discriminated union for success/failure without exceptions in the happy path.
/// </summary>
public sealed class Result<T>
{
    public T? Value { get; }
    public string? ErrorMessage { get; }
    public Exception? Exception { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private Result(T value)                              { Value = value; IsSuccess = true; }
    private Result(string error, Exception? ex = null)  { ErrorMessage = error; Exception = ex; }

    // Aliase: Success/Failure UND Ok/Fail — beide funktionieren
    public static Result<T> Success(T value)                                => new(value);
    public static Result<T> Failure(string error, Exception? ex = null)     => new(error, ex);
    public static Result<T> Ok(T value)                                     => new(value);
    public static Result<T> Fail(string error, Exception? ex = null)        => new(error, ex);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<string, TOut> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(ErrorMessage!);

    public override string ToString() =>
        IsSuccess ? $"Ok({Value})" : $"Fail({ErrorMessage})";
}

/// <summary>
/// Non-generic Result for void operations (e.g. TriggerAction).
/// Eigener Typ — kein Konflikt mit System.Threading.Tasks.
/// </summary>
public sealed class Result
{
    public string? ErrorMessage { get; }
    public Exception? Exception { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private Result()                                    { IsSuccess = true; }
    private Result(string error, Exception? ex = null) { ErrorMessage = error; Exception = ex; }

    public static Result Success()                                      => new();
    public static Result Failure(string error, Exception? ex = null)   => new(error, ex);
    public static Result Ok()                                           => new();
    public static Result Fail(string error, Exception? ex = null)      => new(error, ex);

    public override string ToString() =>
        IsSuccess ? "Ok" : $"Fail({ErrorMessage})";
}
