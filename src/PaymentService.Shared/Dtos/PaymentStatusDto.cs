// FILE: src/PaymentService.Shared/Dtos/PaymentStatusDto.cs
// VERSION: 1.0.0

namespace PaymentService.Shared.Dtos;

/// <summary>
/// Payment status DTO returned by reader API queries.
/// </summary>
public record PaymentStatusDto
{
    /// <summary>Payment correlation identifier.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Current payment status (Pending, Validating, Enriching, Settling, Settled, Failed).</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Saga state machine step.</summary>
    public string SagaState { get; init; } = "None";

    /// <summary>Payment amount.</summary>
    public decimal Amount { get; init; }

    /// <summary>ISO 4217 currency code.</summary>
    public string Currency { get; init; } = "USD";

    /// <summary>When the payment was created.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>When the payment was settled (null if not yet settled).</summary>
    public DateTime? SettledAt { get; init; }

    /// <summary>Sender account.</summary>
    public string SenderAccount { get; init; } = string.Empty;

    /// <summary>Receiver account.</summary>
    public string ReceiverAccount { get; init; } = string.Empty;
}
