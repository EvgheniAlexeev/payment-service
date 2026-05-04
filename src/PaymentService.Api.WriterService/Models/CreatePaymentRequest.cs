// FILE: src/PaymentService.Api.WriterService/Models/CreatePaymentRequest.cs
// VERSION: 1.0.0

using PaymentService.Shared;

namespace PaymentService.Api.WriterService.Models;

/// <summary>
/// BLOCK_CREATE_PAYMENT Request to create a new payment.
/// Contains PII (Description) — must be redacted in logs.
/// </summary>
public record CreatePaymentRequest : IRequest
{
    /// <summary>Unique payment correlation identifier.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Source account identifier.</summary>
    public string SenderAccount { get; init; } = string.Empty;

    /// <summary>Destination account identifier.</summary>
    public string ReceiverAccount { get; init; } = string.Empty;

    /// <summary>Payment amount in the specified currency.</summary>
    public decimal Amount { get; init; }

    /// <summary>ISO 4217 currency code (e.g., USD, EUR).</summary>
    public string Currency { get; init; } = "USD";

    /// <summary>Settlement value date.</summary>
    public DateTime? ValueDate { get; init; }

    /// <summary>Payment purpose/description (PII — redact in logs).</summary>
    public string Description { get; init; } = string.Empty;
}
