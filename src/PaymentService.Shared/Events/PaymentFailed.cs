// FILE: src/PaymentService.Shared/Events/PaymentFailed.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Failure event with DLQ routing and retry information
// SEMANTIC_TAG: [SAGA_EVENT, FAILURE_EVENT, DLQ_PATTERN]
// START_MODULE M-SHARED-EVENTS

namespace PaymentService.Shared.Events;

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@purpose:</strong> Failure event emitted when any saga step fails, routes to DLQ</para>
/// <para><strong>@invariant:</strong> CorrelationId matches PaymentCommand</para>
/// <para><strong>@invariant:</strong> FailedStep ∈ {Validate, Enrich, Settle, Notify}</para>
/// <para><strong>@invariant:</strong> RetryCount ≥ 0</para>
/// <para><strong>@invariant:</strong> FailedAt is UTC timestamp</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </summary>
// START_BLOCK_PAYMENT_FAILED
public record PaymentFailed : IEvent
{
    public string CorrelationId { get; init; } = string.Empty;

    public string FailedStep { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;

    public string ErrorCode { get; init; } = string.Empty;

    public int RetryCount { get; init; }

    public DateTime FailedAt { get; init; }
}
