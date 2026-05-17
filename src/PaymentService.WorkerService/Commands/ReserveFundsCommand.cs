// FILE: ReserveFundsCommand.cs
// VERSION: 2.0.0
// MODULE: M-WORKER
// PURPOSE: Wolverine saga command
// SEMANTIC_TAG: [SAGA_COMMAND, MESSAGE]
// START_MODULE M_WORKER

// START_MODULE M-WORKER
// START_BLOCK_COMMAND ReserveFundsCommand
// PURPOSE: Wolverine command dispatched from saga to the ReserveFundsHandler.
//          Carries amount and sender account for ledger reservation.
// SEMANTIC_TAG: [BLOCK_COMMAND] Wolverine ICommand
namespace PaymentService.Workers.Commands;

/// <summary>
/// Wolverine command for saga step execution in the M-WORKER module
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (Wolverine command, immutable value object)</para>
/// <para><strong>@purpose:</strong> Wolverine command for saga step execution in the M-WORKER module</para>
/// <para><strong>@invariant:</strong> Immutable Wolverine command; all properties set at construction</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>

public sealed record ReserveFundsCommand
{
    /// <summary>Correlation ID of the payment.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Amount to reserve (in the payment currency).</summary>
    public decimal Amount { get; init; }

    /// <summary>Sender account identifier (IBAN or internal).</summary>
    public string SenderAccount { get; init; } = string.Empty;

    /// <summary>When the command was created (UTC).</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
// END_BLOCK_COMMAND
