// FILE: src/PaymentService.Persistence/Repositories/IPaymentDocumentRepository.cs
// VERSION: 2.0.0
// MODULE: M-MONGO
// PURPOSE: MongoDB repository pattern implementation
// SEMANTIC_TAG: [REPOSITORY, DATA_ACCESS]
// START_MODULE M_MONGO

// FILE: src/PaymentService.Persistence/Repositories/IPaymentDocumentRepository.cs
// VERSION: 1.0.0

using MongoDB.Driver;
using PaymentService.Shared.Models;

namespace PaymentService.Persistence.Repositories;

/// <summary>
/// Repository for PaymentDocument persistence operations.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-MONGO</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> Data access interface for payment documents with MongoDB persistence</para>
/// <para><strong>@invariant:</strong> CorrelationId is unique key across all payments</para>
/// <para><strong>@invariant:</strong> All operations support transactional semantics via IClientSessionHandle</para>
/// <para><strong>@verification-ref:</strong> V-M-MONGO</para>
/// </remarks>
public interface IPaymentDocumentRepository
{
    /// <summary>
    /// Retrieve a payment by its correlation ID.
    /// Returns null if not found.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> GetByCorrelationIdAsync</para>
    /// <para><strong>@param correlationId:</strong> Unique payment identifier</para>
    /// <para><strong>@return:</strong> PaymentDocument or null if not found</para>
    /// <para><strong>@throws:</strong> TimeoutException — query exceeded timeout</para>
    /// <para><strong>@log-event:</strong> mongo.repository.get-by-correlation-id-start {correlationId}</para>
    /// <para><strong>@log-event:</strong> mongo.repository.get-by-correlation-id-found {correlationId}</para>
    /// <para><strong>@log-event:</strong> mongo.repository.get-by-correlation-id-not-found {correlationId}</para>
    /// <para><strong>@trace-span:</strong> mongo.repo.get-by-correlation-id</para>
    /// <para><strong>@complexity:</strong> O(1) (index lookup)</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// <para><strong>@pure:</strong> NO (I/O: database read)</para>
    /// </remarks>
    Task<PaymentDocument?> GetByCorrelationIdAsync(string correlationId, CancellationToken ct = default);

    /// <summary>
    /// Insert a new payment document (within optional transaction).
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> InsertAsync</para>
    /// <para><strong>@param document:</strong> PaymentDocument to insert</para>
    /// <para><strong>@throws:</strong> DuplicateKeyException — CorrelationId already exists</para>
    /// <para><strong>@log-event:</strong> mongo.repository.insert-start {correlationId}</para>
    /// <para><strong>@log-event:</strong> mongo.repository.insert-success {correlationId}</para>
    /// <para><strong>@trace-span:</strong> mongo.repo.insert</para>
    /// <para><strong>@complexity:</strong> O(1) (direct write)</para>
    /// <para><strong>@idempotent:</strong> NO (creates new document)</para>
    /// <para><strong>@pure:</strong> NO (I/O: database write)</para>
    /// </remarks>
    Task InsertAsync(PaymentDocument document, IClientSessionHandle? session = null, CancellationToken ct = default);

    /// <summary>
    /// Update an existing payment document (within optional transaction).
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> UpdateAsync</para>
    /// <para><strong>@param document:</strong> PaymentDocument with updates</para>
    /// <para><strong>@log-event:</strong> mongo.repository.update-start {correlationId}</para>
    /// <para><strong>@log-event:</strong> mongo.repository.update-success {correlationId}</para>
    /// <para><strong>@trace-span:</strong> mongo.repo.update</para>
    /// <para><strong>@complexity:</strong> O(1) (direct write)</para>
    /// <para><strong>@idempotent:</strong> NO (modifies document state)</para>
    /// <para><strong>@pure:</strong> NO (I/O: database write)</para>
    /// </remarks>
    Task UpdateAsync(PaymentDocument document, IClientSessionHandle? session = null, CancellationToken ct = default);

    /// <summary>
    /// Query payments by status with pagination.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> GetByStatusAsync</para>
    /// <para><strong>@param status:</strong> Payment status filter</para>
    /// <para><strong>@param skip:</strong> Result offset</para>
    /// <para><strong>@param limit:</strong> Result count limit</para>
    /// <para><strong>@return:</strong> List of PaymentDocuments matching status</para>
    /// <para><strong>@complexity:</strong> O(log n + k) where k = result set size</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// </remarks>
    Task<List<PaymentDocument>> GetByStatusAsync(
        string status, int skip = 0, int limit = 20, CancellationToken ct = default);

    /// <summary>
    /// Query payments by list of correlation IDs.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> GetBatchAsync</para>
    /// <para><strong>@param correlationIds:</strong> List of payment identifiers</para>
    /// <para><strong>@return:</strong> List of matching PaymentDocuments</para>
    /// <para><strong>@complexity:</strong> O(log n + k) where k = result set size</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// </remarks>
    Task<List<PaymentDocument>> GetBatchAsync(List<string> correlationIds, CancellationToken ct = default);

    /// <summary>
    /// Check if a payment exists by correlation ID.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> ExistsByCorrelationIdAsync</para>
    /// <para><strong>@param correlationId:</strong> Unique payment identifier</para>
    /// <para><strong>@return:</strong> true if payment exists, false otherwise</para>
    /// <para><strong>@complexity:</strong> O(1) (existence check)</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// </remarks>
    Task<bool> ExistsByCorrelationIdAsync(string correlationId, CancellationToken ct = default);
}
