// FILE: src/PaymentService.Api.ReaderService/Models/GetPaymentRequest.cs
// VERSION: 1.0.0

using PaymentService.Shared;

namespace PaymentService.Api.ReaderService.Models;

/// <summary>
/// Request to retrieve a payment by correlation ID.
/// </summary>
public record GetPaymentRequest : IRequest
{
    /// <summary>Payment correlation identifier.</summary>
    public string CorrelationId { get; init; } = string.Empty;
}
