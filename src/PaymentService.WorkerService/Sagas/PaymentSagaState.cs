// START_MODULE M-WORKER
// START_BLOCK_SAGA_STATE PaymentSagaState
// PURPOSE: Wolverine saga state document persisted in MongoDB.
//          Tracks full saga lifecycle: correlation ID, status, request, reservation, errors, timestamps.
//          TTL index auto-expires after 7 days (defined in IndexConfiguration).
// SEMANTIC_TAG: [BLOCK_SAGA_STATE] persisted to MongoDB "payment_saga_states" collection
namespace PaymentService.Workers.Sagas;

using PaymentService.Shared.Dtos;

/// <summary>
/// Wolverine saga state document for the Payment orchestration saga.
/// Persisted in MongoDB with 7-day TTL auto-expiry.
/// </summary>
public sealed class PaymentSagaState
{
    /// <summary>
    /// Unique saga identifier — uses CorrelationId for deterministic saga lookup.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Correlation ID linking to the payment document.</summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Current saga status:
    /// Validating → ReservingFunds → Settling → Settled | Failed
    /// </summary>
    public string Status { get; set; } = "Validating";

    /// <summary>The original payment request payload.</summary>
    public PaymentRequestDto? PaymentRequest { get; set; }

    /// <summary>Reservation reference ID from the ledger service (null until reserved).</summary>
    public string? ReservationId { get; set; }

    /// <summary>Human-readable error reason (populated on failure).</summary>
    public string? ErrorReason { get; set; }

    /// <summary>Machine-readable error code for classification.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Retry count at the saga level.</summary>
    public int RetryCount { get; set; }

    /// <summary>When the saga was created (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the saga completed (UTC, null if still in progress).</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Optimistic concurrency version.</summary>
    public int Version { get; set; }
}
// END_BLOCK_SAGA_STATE
