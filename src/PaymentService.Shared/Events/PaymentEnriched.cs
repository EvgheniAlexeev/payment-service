// FILE: src/PaymentService.Shared/Events/PaymentEnriched.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Intermediate success event for payment enrichment step
// SEMANTIC_TAG: [SAGA_EVENT, ENRICHMENT_STEP]
// START_MODULE M-SHARED-EVENTS

namespace PaymentService.Shared.Events;

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@purpose:</strong> Intermediate event emitted when EnrichPayment saga step completes</para>
/// <para><strong>@module-type:</strong> UTILITY (pure event contract)</para>
/// <para><strong>@depends:</strong> IEvent interface</para>
/// <para><strong>@domain-concept:</strong> PaymentEnriched (saga milestone event)</para>
/// <para><strong>@invariant:</strong> CorrelationId matches PaymentCommand</para>
/// <para><strong>@invariant:</strong> EnrichedAt is UTC timestamp</para>
/// <para><strong>@stability:</strong> STABLE</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </summary>
/// <remarks>
/// <para><strong>Event Trigger:</strong> PaymentSaga.EnrichPayment() completes successfully</para>
/// <para><strong>Publishing:</strong> Published to event bus, continues to SettlePayment step</para>
/// <para><strong>Subscribers:</strong> Audit log, enrichment metrics</para>
/// </remarks>
public record PaymentEnriched : IEvent
{
    /// <summary><para><strong>@property:</strong> CorrelationId</para><para>Saga identifier</para></summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary><para><strong>@property:</strong> SenderName</para><para>Enriched sender information from reference data</para></summary>
    public string? SenderName { get; init; }

    /// <summary><para><strong>@property:</strong> ReceiverName</para><para>Enriched receiver information from reference data</para></summary>
    public string? ReceiverName { get; init; }

    /// <summary><para><strong>@property:</strong> EnrichedAt</para><para>Enrichment completion timestamp (UTC)</para></summary>
    public DateTime EnrichedAt { get; init; }
}
