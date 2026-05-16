// FILE: src/PaymentService.Shared/Events/PaymentSettled.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Success event emitted when saga settles payment
// SEMANTIC_TAG: [SAGA_EVENT, SUCCESS_EVENT]
// START_MODULE M-SHARED-EVENTS

namespace PaymentService.Shared.Events;

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> Success event emitted by PaymentSaga when settlement completes</para>
/// <para><strong>@invariant:</strong> CorrelationId matches initiating PaymentCommand</para>
/// <para><strong>@invariant:</strong> SettlementId is unique per settlement attempt</para>
/// <para><strong>@invariant:</strong> SettledAt is always UTC</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </summary>
// START_BLOCK_PAYMENT_SETTLED
public record PaymentSettled : IEvent
{
    public string CorrelationId { get; init; } = string.Empty;

    public string SettlementId { get; init; } = string.Empty;

    public DateTime SettledAt { get; init; }

    public string Status { get; init; } = "Settled";
}
