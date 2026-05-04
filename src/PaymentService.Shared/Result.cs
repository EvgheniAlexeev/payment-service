using Microsoft.Extensions.Logging;

namespace PaymentService.Shared;

/// <summary>
/// Generic result wrapper for handler operations.
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; private init; }
    public T? Data { get; private init; }
    public string? Error { get; private init; }
    public bool IsNotFound { get; private init; }

    public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
    public static Result<T> NotFound(string error) => new() { IsSuccess = false, Error = error, IsNotFound = true };
}

/// <summary>
/// Non-generic result for void operations.
/// </summary>
public class Result
{
    public bool IsSuccess { get; private init; }
    public string? Error { get; private init; }

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string error) => new() { IsSuccess = false, Error = error };
}
