// FILE: FundsReserved.cs
// VERSION: 2.0.0
// MODULE: M-WORKER
// PURPOSE: Saga internal or external event
// SEMANTIC_TAG: [SAGA_EVENT, MESSAGE]
// START_MODULE M_WORKER

// START_MODULE M-WORKER
// START_BLOCK_EVENT FundsReserved
// PURPOSE: Wolverine event emitted after ReserveFunds step completes.
//          Carries reservation ID for later settlement or compensation.
// SEMANTIC_TAG: [BLOCK_EVENT] Wolverine IEvent — funds reserved
namespace PaymentService.Workers.Events;

/// <summary>
/// Wolverine event published after the ReserveFunds step handler completes.
/// </summary>
public sealed record FundsReserved
{
    /// <summary>Correlation ID of the payment.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Reservation reference ID from the ledger service.</summary>
    public string ReservationId { get; init; } = string.Empty;

    /// <summary>Amount reserved (in the payment currency).</summary>
    public decimal Amount { get; init; }

    /// <summary>Whether the reservation was successful.</summary>
    public bool IsSuccessful { get; init; }

    /// <summary>Error message if reservation failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>When reservation completed (UTC).</summary>
    public DateTime ReservedAt { get; init; } = DateTime.UtcNow;
}
// END_BLOCK_EVENT
