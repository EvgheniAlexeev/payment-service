// FILE: src/PaymentService.Shared/Events/PaymentSettled.cs
// VERSION: 1.0.0

namespace PaymentService.Shared.Events;

/// <summary>
/// BLOCK_PAYMENT_SETTLED event — emitted when a payment successfully settles.
/// </summary>
public record PaymentSettled : IEvent
{
    /// <summary>Correlation ID of the settled payment.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Settlement reference ID.</summary>
    public string SettlementId { get; init; } = string.Empty;

    /// <summary>When settlement occurred.</summary>
    public DateTime SettledAt { get; init; }

    /// <summary>Final status: "Settled".</summary>
    public string Status { get; init; } = "Settled";
}
