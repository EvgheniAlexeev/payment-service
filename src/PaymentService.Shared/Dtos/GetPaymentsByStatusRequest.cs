// FILE: src/PaymentService.Shared/Dtos/GetPaymentsByStatusRequest.cs
// VERSION: 1.0.0

namespace PaymentService.Shared.Dtos;

/// <summary>
/// Request to query payments by status with optional pagination.
/// </summary>
public record GetPaymentsByStatusRequest : IRequest
{
    /// <summary>Status filter (e.g., Pending, Settled, Failed).</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Page number (1-based).</summary>
    public int Page { get; init; } = 1;

    /// <summary>Page size.</summary>
    public int PageSize { get; init; } = 20;
}
