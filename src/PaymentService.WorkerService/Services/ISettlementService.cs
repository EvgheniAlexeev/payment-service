// FILE: ISettlementService.cs
// VERSION: 2.0.0
// MODULE: M-WORKER
// PURPOSE: External service dependency
// SEMANTIC_TAG: [SERVICE_ABSTRACTION, DEPENDENCY]
// START_MODULE M_WORKER

// START_MODULE M-WORKER
// START_BLOCK_SERVICE ISettlementService
// PURPOSE: External settlement service contract for final payment settlement.
//          Abstracted behind interface for mock-based and Docker integration testing.
// SEMANTIC_TAG: [BLOCK_SERVICE_INTERFACE] Export: ISettlementService
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
public interface ISettlementService
{
    /// <summary>
    /// Settle a payment with the provided details.
    /// Returns a settlement reference ID, or null if settlement fails.
    /// </summary>
    /// <param name="correlationId">Payment correlation ID.</param>
    /// <param name="reservationId">Reservation reference to settle.</param>
    /// <param name="amount">Amount to settle.</param>
    /// <param name="receiverAccount">Destination account.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Settlement reference ID (transaction ID), or null if failed.</returns>
    Task<string?> SettleAsync(
        string correlationId,
        string reservationId,
        decimal amount,
        string receiverAccount,
        CancellationToken ct = default);
}
// END_BLOCK_SERVICE
