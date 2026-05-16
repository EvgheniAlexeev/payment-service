// FILE: src/PaymentService.Shared/Events/PaymentSettled.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Success event emitted when saga settles payment
// SEMANTIC_TAG: [SAGA_EVENT, SUCCESS_EVENT]
// START_MODULE M-SHARED-EVENTS

namespace PaymentService.Shared.Events;

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@purpose:</strong> Success event emitted by PaymentSaga when settlement completes</para>
/// <para><strong>@module-type:</strong> UTILITY (pure event contract)</para>
/// <para><strong>@depends:</strong> IEvent interface</para>
/// <para><strong>@domain-concept:</strong> PaymentSettled (domain event)</para>
/// <para><strong>@invariant:</strong> CorrelationId matches initiating PaymentCommand</para>
/// <para><strong>@invariant:</strong> SettlementId is unique per settlement attempt</para>
/// <para><strong>@invariant:</strong> SettledAt is always UTC</para>
/// <para><strong>@stability:</strong> STABLE</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </summary>
/// <remarks>
/// <para><strong>Event Trigger:</strong> PaymentSaga.SettlePayment() completes successfully</para>
/// <para><strong>Publishing:</strong> Published to event bus for downstream subscriptions (e.g., notifications)</para>
/// <para><strong>Subscribers:</strong> SettlementNotifier, ReportingService, AuditLog</para>
/// </remarks>
public record PaymentSettled : IEvent
{
    /// <summary><para><strong>@property:</strong> CorrelationId</para><para>Saga identifier, matches PaymentCommand</para></summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary><para><strong>@property:</strong> SettlementId</para><para>Ledger settlement reference ID</para></summary>
    public string SettlementId { get; init; } = string.Empty;

    /// <summary><para><strong>@property:</strong> SettledAt</para><para>Settlement completion timestamp (UTC)</para></summary>
    public DateTime SettledAt { get; init; }

    /// <summary><para><strong>@property:</strong> Status</para><para>Fixed: 'Settled' (invariant)</para></summary>
    public string Status { get; init; } = "Settled";
}
