// FILE: src/PaymentService.Api.ReaderService/Features/QueryPayments/QueryPaymentsResponse.cs
// VERSION: 1.0.0

using PaymentService.Shared.Dtos;

namespace PaymentService.Api.ReaderService.Features.QueryPayments;

/// <summary>
/// Response wrapper for QueryPayments feature.
/// VSA feature: QueryPayments (ReaderService)
/// </summary>
public class QueryPaymentsResponse
{
    public List<PaymentStatusDto> Payments { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
