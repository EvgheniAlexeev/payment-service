// FILE: src/PaymentService.Shared/Events/PaymentEnriched.cs
// VERSION: 1.0.0

namespace PaymentService.Shared.Events;

/// <summary>
/// BLOCK_PAYMENT_ENRICHED event — emitted when payment enrichment completes.
/// </summary>
public record PaymentEnriched : IEvent
{
    /// <summary>Correlation ID of the enriched payment.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Enriched sender details.</summary>
    public string? SenderName { get; init; }

    /// <summary>Enriched receiver details.</summary>
    public string? ReceiverName { get; init; }

    /// <summary>When enrichment occurred.</summary>
    public DateTime EnrichedAt { get; init; }
}
