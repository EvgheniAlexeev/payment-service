// FILE: src/PaymentService.Api.WriterService/Models/CreatePaymentResponse.cs
// VERSION: 2.0.0
// MODULE: M-WRITER
// PURPOSE: Command response DTO
// SEMANTIC_TAG: [RESPONSE_DTO]
// START_MODULE M_WRITER

// FILE: src/PaymentService.Api.WriterService/Models/CreatePaymentResponse.cs
// VERSION: 1.0.0

using PaymentService.Shared;

namespace PaymentService.Api.WriterService.Models;

/// <summary>
/// Response for payment creation (202 Accepted pattern).
/// </summary>
public record CreatePaymentResponse : IResponse
{
    /// <summary>The correlationId of the created payment.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Human-readable status message.</summary>
    public string Message { get; init; } = "Payment accepted for processing";

    /// <summary>Timestamp of acceptance.</summary>
    public DateTime AcceptedAt { get; init; } = DateTime.UtcNow;
}
