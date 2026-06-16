namespace Logistics.Application.Common.Models;

/// <summary>
/// Lightweight result wrapper so handlers can return success/failure without throwing
/// for expected, non-exceptional outcomes.
/// </summary>
public class Result
{
    public bool Succeeded { get; }
    public string? Error { get; }

    protected Result(bool succeeded, string? error)
    {
        Succeeded = succeeded;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool succeeded, T? value, string? error) : base(succeeded, error)
        => Value = value;

    public static Result<T> Success(T value) => new(true, value, null);
    public static new Result<T> Failure(string error) => new(false, default, error);
}
