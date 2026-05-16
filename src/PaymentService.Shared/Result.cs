// FILE: src/PaymentService.Shared/Result.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Generic result wrapper for handler operations
// SEMANTIC_TAG: [RESULT_TYPE, ERROR_HANDLING]
// START_MODULE M-SHARED-RESULT

using Microsoft.Extensions.Logging;

namespace PaymentService.Shared;

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> Generic result wrapper encapsulating success/failure state with optional data</para>
/// <para><strong>@invariant:</strong> IsSuccess AND Error mutually exclusive</para>
/// <para><strong>@invariant:</strong> Data populated only when IsSuccess=true</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </summary>
/// <remarks>
/// <para><strong>Pattern:</strong> Railway-oriented programming — Success or Failure, never throws</para>
/// <para><strong>Usage:</strong> All handler methods return Result&lt;T&gt; for consistent error handling</para>
/// </remarks>
public class Result<T>
{
    /// <summary><para><strong>@property:</strong> IsSuccess</para><para>Operation succeeded</para></summary>
    public bool IsSuccess { get; private init; }

    /// <summary><para><strong>@property:</strong> Data</para><para>Return value on success, null on failure</para></summary>
    public T? Data { get; private init; }

    /// <summary><para><strong>@property:</strong> Error</para><para>Error message on failure, null on success</para></summary>
    public string? Error { get; private init; }

    /// <summary><para><strong>@property:</strong> IsNotFound</para><para>Failure due to 404 (not found)</para></summary>
    public bool IsNotFound { get; private init; }

    /// <summary><para><strong>@method:</strong> Success</para><para>Create successful result</para></summary>
    public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };

    /// <summary><para><strong>@method:</strong> Failure</para><para>Create failure result</para></summary>
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };

    /// <summary><para><strong>@method:</strong> NotFound</para><para>Create 404 failure result</para></summary>
    public static Result<T> NotFound(string error) => new() { IsSuccess = false, Error = error, IsNotFound = true };
}

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> Non-generic result for void operations</para>
/// <para><strong>@invariant:</strong> IsSuccess AND Error mutually exclusive</para>
/// </summary>
public class Result
{
    /// <summary><para><strong>@property:</strong> IsSuccess</para><para>Operation succeeded</para></summary>
    public bool IsSuccess { get; private init; }

    /// <summary><para><strong>@property:</strong> Error</para><para>Error message on failure, null on success</para></summary>
    public string? Error { get; private init; }

    /// <summary><para><strong>@method:</strong> Success</para><para>Create successful result</para></summary>
    public static Result Success() => new() { IsSuccess = true };

    /// <summary><para><strong>@method:</strong> Failure</para><para>Create failure result</para></summary>
    public static Result Failure(string error) => new() { IsSuccess = false, Error = error };
}
