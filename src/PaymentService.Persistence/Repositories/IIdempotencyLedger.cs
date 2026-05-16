// FILE: src/PaymentService.Persistence/Repositories/IIdempotencyLedger.cs
// VERSION: 2.0.0
// MODULE: M-MONGO
// PURPOSE: MongoDB repository pattern implementation
// SEMANTIC_TAG: [REPOSITORY, DATA_ACCESS]
// START_MODULE M_MONGO

// FILE: src/PaymentService.Persistence/Repositories/IIdempotencyLedger.cs
// VERSION: 1.0.0

using MongoDB.Driver;

namespace PaymentService.Persistence.Repositories;

/// <summary>
/// Idempotency ledger for step-level deduplication in saga processing.
/// </summary>
public interface IIdempotencyLedger
{
    /// <summary>
    /// Attempt to mark a step as complete. Returns true if newly marked,
    /// false if already marked (idempotent skip).
    /// </summary>
    Task<bool> TryMarkCompleteAsync(
        string correlationId, string stepName, IClientSessionHandle? session = null, CancellationToken ct = default);

    /// <summary>
    /// Check if a step is already marked as complete.
    /// </summary>
    Task<bool> IsStepCompleteAsync(string correlationId, string stepName, CancellationToken ct = default);
}
