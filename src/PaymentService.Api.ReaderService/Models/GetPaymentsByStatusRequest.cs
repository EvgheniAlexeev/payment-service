// FILE: src/PaymentService.Api.ReaderService/Models/GetPaymentsByStatusRequest.cs
// VERSION: 1.0.0

using PaymentService.Shared;

namespace PaymentService.Api.ReaderService.Models;

/// <summary>
/// Request to query payments filtered by status.
/// </summary>
public record GetPaymentsByStatusRequest : IRequest
{
    /// <summary>Payment status to filter by.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Page number (1-based, default 1).</summary>
    public int Page { get; init; } = 1;

    /// <summary>Page size (default 20, max 100).</summary>
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Response wrapper for paginated status queries.
/// </summary>
public record PagedPaymentStatusResponse : IResponse
{
    public List<Shared.Dtos.PaymentStatusDto> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages =>
        PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasNextPage => Page < TotalPages;
}
