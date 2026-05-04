// FILE: src/PaymentService.Shared/Events/PaymentFailed.cs
// VERSION: 1.0.0

namespace PaymentService.Shared.Events;

/// <summary>
/// BLOCK_PAYMENT_FAILED event — emitted when a payment fails at any stage.
/// Routed to DLQ for retry/disposition.
/// </summary>
public record PaymentFailed : IEvent
{
    /// <summary>Correlation ID of the failed payment.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Step that failed (Validate, Enrich, Settle, Notify).</summary>
    public string FailedStep { get; init; } = string.Empty;

    /// <summary>Human-readable error description.</summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>Machine-readable error code.</summary>
    public string ErrorCode { get; init; } = string.Empty;

    /// <summary>Number of retry attempts so far.</summary>
    public int RetryCount { get; init; }

    /// <summary>Timestamp of the failure.</summary>
    public DateTime FailedAt { get; init; }
}
