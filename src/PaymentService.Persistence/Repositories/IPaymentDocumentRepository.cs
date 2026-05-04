// FILE: src/PaymentService.Persistence/Repositories/IPaymentDocumentRepository.cs
// VERSION: 1.0.0

using MongoDB.Driver;
using PaymentService.Shared.Models;

namespace PaymentService.Persistence.Repositories;

/// <summary>
/// Repository for PaymentDocument persistence operations.
/// </summary>
public interface IPaymentDocumentRepository
{
    /// <summary>
    /// Retrieve a payment by its correlation ID.
    /// Returns null if not found.
    /// </summary>
    Task<PaymentDocument?> GetByCorrelationIdAsync(string correlationId, CancellationToken ct = default);

    /// <summary>
    /// Insert a new payment document (within optional transaction).
    /// </summary>
    Task InsertAsync(PaymentDocument document, IClientSessionHandle? session = null, CancellationToken ct = default);

    /// <summary>
    /// Update an existing payment document (within optional transaction).
    /// </summary>
    Task UpdateAsync(PaymentDocument document, IClientSessionHandle? session = null, CancellationToken ct = default);

    /// <summary>
    /// Query payments by status with pagination.
    /// </summary>
    Task<List<PaymentDocument>> GetByStatusAsync(
        string status, int skip = 0, int limit = 20, CancellationToken ct = default);

    /// <summary>
    /// Query payments by list of correlation IDs.
    /// </summary>
    Task<List<PaymentDocument>> GetBatchAsync(List<string> correlationIds, CancellationToken ct = default);

    /// <summary>
    /// Check if a payment exists by correlation ID.
    /// </summary>
    Task<bool> ExistsByCorrelationIdAsync(string correlationId, CancellationToken ct = default);
}
