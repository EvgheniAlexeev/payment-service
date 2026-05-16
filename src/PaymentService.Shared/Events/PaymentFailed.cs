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
/// <para><strong>@module-type:</strong> UTILITY (pure event contract)</para>
/// <para><strong>@depends:</strong> IEvent interface</para>
/// <para><strong>@domain-concept:</strong> PaymentFailed (failure envelope)</para>
/// <para><strong>@invariant:</strong> CorrelationId matches PaymentCommand</para>
/// <para><strong>@invariant:</strong> FailedStep in {Validate, Enrich, Settle, Notify}</para>
/// <para><strong>@invariant:</strong> RetryCount ≥ 0</para>
/// <para><strong>@invariant:</strong> FailedAt is UTC timestamp</para>
/// <para><strong>@stability:</strong> STABLE</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </summary>
/// <remarks>
/// <para><strong>Event Trigger:</strong> Any PaymentSaga step fails, exception caught</para>
/// <para><strong>Routing:</strong> Published to Dead-Letter Queue topic for operator review</para>
/// <para><strong>Subscribers:</strong> DLQHandler (manual disposition), monitoring/alerting</para>
/// <para><strong>Operator Action:</strong> Review FailedStep, ErrorCode, retry or manual intervention</para>
/// </remarks>
public record PaymentFailed : IEvent
{
    /// <summary><para><strong>@property:</strong> CorrelationId</para><para>Saga identifier</para></summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary><para><strong>@property:</strong> FailedStep</para><para>Saga step where failure occurred</para></summary>
    public string FailedStep { get; init; } = string.Empty;

    /// <summary><para><strong>@property:</strong> ErrorMessage</para><para>Human-readable error description</para></summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary><para><strong>@property:</strong> ErrorCode</para><para>Machine-readable error classification</para></summary>
    public string ErrorCode { get; init; } = string.Empty;

    /// <summary><para><strong>@property:</strong> RetryCount</para><para>Number of retry attempts so far</para></summary>
    public int RetryCount { get; init; }

    /// <summary><para><strong>@property:</strong> FailedAt</para><para>Failure timestamp (UTC)</para></summary>
    public DateTime FailedAt { get; init; }
}
