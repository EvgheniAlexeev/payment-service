// FILE: src/PaymentService.Shared/Models/PaymentDocument.cs
// VERSION: 1.0.0

using PaymentService.Shared.Dtos;

namespace PaymentService.Shared.Models;

/// <summary>
/// MongoDB document representing a payment.
/// Uses correlationId as the business key with a unique index.
/// </summary>
public record PaymentDocument
{
    /// <summary>MongoDB _id.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Business key — unique payment correlation identifier.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Full payment request payload.</summary>
    public PaymentRequestDto Request { get; init; } = new();

    /// <summary>
    /// Current payment status.
    /// Valid values: Pending, Validating, Enriching, Settling, Settled, Failed, Compensated.
    /// </summary>
    public string Status { get; init; } = "Pending";

    /// <summary>Saga state machine step (Validating, Enriching, Settling, Notifying, Completed).</summary>
    public string SagaState { get; init; } = "None";

    /// <summary>When the payment was created.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>When the payment was settled (null if not yet settled).</summary>
    public DateTime? SettledAt { get; init; }

    /// <summary>Settlement reference ID (populated after settlement).</summary>
    public string? SettlementId { get; init; }

    /// <summary>When the document was last modified.</summary>
    public DateTime? ModifiedAt { get; init; }
}
