// FILE: src/PaymentService.Shared/Events/PaymentEnriched.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Intermediate success event for payment enrichment step
// SEMANTIC_TAG: [SAGA_EVENT, ENRICHMENT_STEP]
// START_MODULE M-SHARED-EVENTS

namespace PaymentService.Shared.Events;

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> Intermediate event emitted when EnrichPayment saga step completes</para>
/// <para><strong>@invariant:</strong> CorrelationId matches PaymentCommand</para>
/// <para><strong>@invariant:</strong> EnrichedAt is UTC timestamp</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </summary>
// START_BLOCK_PAYMENT_ENRICHED
public record PaymentEnriched : IEvent
{
    public string CorrelationId { get; init; } = string.Empty;

    public string? SenderName { get; init; }

    public string? ReceiverName { get; init; }

    public DateTime EnrichedAt { get; init; }
}
