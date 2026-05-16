// FILE: src/PaymentService.Api.ReaderService/Models/GetPaymentRequest.cs
// VERSION: 2.0.0
// MODULE: M-READER
// PURPOSE: Query request DTO
// SEMANTIC_TAG: [QUERY_DTO, INPUT_VALIDATION]
// START_MODULE M_READER

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
