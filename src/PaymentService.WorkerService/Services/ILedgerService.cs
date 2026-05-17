// FILE: ILedgerService.cs
// VERSION: 2.0.0
// MODULE: M-WORKER
// PURPOSE: External service dependency
// SEMANTIC_TAG: [SERVICE_ABSTRACTION, DEPENDENCY]
// START_MODULE M_WORKER

// START_MODULE M-WORKER
// START_BLOCK_SERVICE ILedgerService
// PURPOSE: External ledger service contract for fund reservation and settlement.
//          Abstracted behind interface for mock-based integration testing.
// SEMANTIC_TAG: [BLOCK_SERVICE_INTERFACE] Export: ILedgerService
namespace PaymentService.Workers.Services;

/// <summary>
/// Service abstraction contract for the M-WORKER module
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (service abstraction, dependency injection contract)</para>
/// <para><strong>@purpose:</strong> Service abstraction contract for the M-WORKER module</para>
/// <para><strong>@invariant:</strong> All implementations must be thread-safe and respect cancellation tokens</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>
public interface ILedgerService
{
    /// <summary>
    /// Reserve funds for a payment. Returns a reservation reference ID.
    /// </summary>
    /// <param name="correlationId">Payment correlation ID.</param>
    /// <param name="amount">Amount to reserve.</param>
    /// <param name="senderAccount">Sender account identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Reservation reference ID, or null if reservation fails.</returns>
    Task<string?> ReserveFundsAsync(
        string correlationId,
        decimal amount,
        string senderAccount,
        CancellationToken ct = default);

    /// <summary>
    /// Commit (settle) a previously reserved amount.
    /// Returns true if settlement succeeds.
    /// </summary>
    /// <param name="correlationId">Payment correlation ID.</param>
    /// <param name="reservationId">Reservation reference to settle.</param>
    /// <param name="amount">Amount to settle.</param>
    /// <param name="receiverAccount">Destination account.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> SettleFundsAsync(
        string correlationId,
        string reservationId,
        decimal amount,
        string receiverAccount,
        CancellationToken ct = default);

    /// <summary>
    /// Release a previously reserved amount without settling.
    /// Used for compensation (though currently manual DLQ review, this is available).
    /// </summary>
    /// <param name="correlationId">Payment correlation ID.</param>
    /// <param name="reservationId">Reservation reference to release.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ReleaseReservationAsync(
        string correlationId,
        string reservationId,
        CancellationToken ct = default);
}
// END_BLOCK_SERVICE
