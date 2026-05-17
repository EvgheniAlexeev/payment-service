// FILE: src/PaymentService.Persistence/Repositories/PaymentDocumentRepository.cs
// VERSION: 2.0.0
// MODULE: M-MONGO
// PURPOSE: MongoDB repository pattern implementation
// SEMANTIC_TAG: [REPOSITORY, DATA_ACCESS]
// START_MODULE M_MONGO

// FILE: src/PaymentService.Persistence/Repositories/PaymentDocumentRepository.cs
// VERSION: 1.0.0

using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using PaymentService.Persistence.MongoDB;
using PaymentService.Shared.Models;

namespace PaymentService.Persistence.Repositories;

/// <summary>
/// BLOCK_PAYMENT_REPOSITORY — MongoDB payment document repository.
/// Provides CRUD operations with transaction support.
/// </summary>
public class PaymentDocumentRepository : IPaymentDocumentRepository
{
    private readonly MongoDbContext _context;
    private readonly ILogger<PaymentDocumentRepository> _logger;

    public PaymentDocumentRepository(MongoDbContext context, ILogger<PaymentDocumentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PaymentDocument?> GetByCorrelationIdAsync(
        string correlationId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[PaymentService.Persistence][PaymentDocumentRepository][BLOCK_QUERY_BY_ID] " +
            "Querying payment {CorrelationId}", correlationId);

        var filter = Builders<PaymentDocument>.Filter.Eq(p => p.CorrelationId, correlationId);
        var result = await _context.Payments.Find(filter).FirstOrDefaultAsync(ct);

        if (result == null)
        {
            _logger.LogInformation(
                "[PaymentService.Persistence][PaymentDocumentRepository][BLOCK_QUERY_NOT_FOUND] " +
                "Payment not found {CorrelationId}", correlationId);
        }

        return result;
    }

    public async Task InsertAsync(
        PaymentDocument document, IClientSessionHandle? session = null, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[PaymentService.Persistence][PaymentDocumentRepository][BLOCK_INSERT] " +
            "Inserting payment {CorrelationId}", document.CorrelationId);

        if (session != null)
            await _context.Payments.InsertOneAsync(session, document, null, ct);
        else
            await _context.Payments.InsertOneAsync(document, null, ct);
    }

    public async Task UpdateAsync(
        PaymentDocument document, IClientSessionHandle? session = null, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[PaymentService.Persistence][PaymentDocumentRepository][BLOCK_UPDATE] " +
            "Updating payment {CorrelationId}", document.CorrelationId);

        var filter = Builders<PaymentDocument>.Filter.Eq(p => p.CorrelationId, document.CorrelationId);

        if (session != null)
            await _context.Payments.ReplaceOneAsync(session, filter, document, cancellationToken: ct);
        else
            await _context.Payments.ReplaceOneAsync(filter, document, cancellationToken: ct);
    }

    public async Task<List<PaymentDocument>> GetByStatusAsync(
        string status, int skip = 0, int limit = 20, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[PaymentService.Persistence][PaymentDocumentRepository][BLOCK_QUERY_BY_STATUS] " +
            "Querying payments by status {Status} skip={Skip} limit={Limit}",
            status, skip, limit);

        var filter = Builders<PaymentDocument>.Filter.Eq(p => p.Status, status);
        var sort = Builders<PaymentDocument>.Sort.Descending(p => p.CreatedAt);

        return await _context.Payments
            .Find(filter)
            .Sort(sort)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync(ct);
    }

    public async Task<List<PaymentDocument>> GetBatchAsync(
        List<string> correlationIds, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[PaymentService.Persistence][PaymentDocumentRepository][BLOCK_QUERY_BATCH] " +
            "Querying batch payments count={Count}", correlationIds.Count);

        var filter = Builders<PaymentDocument>.Filter.In(p => p.CorrelationId, correlationIds);
        return await _context.Payments.Find(filter).ToListAsync(ct);
    }

    public async Task<bool> ExistsByCorrelationIdAsync(
        string correlationId, CancellationToken ct = default)
    {
        var filter = Builders<PaymentDocument>.Filter.Eq(p => p.CorrelationId, correlationId);
        var count = await _context.Payments.CountDocumentsAsync(filter, null, ct);
        return count > 0;
    }

    public async Task<(List<PaymentDocument> Payments, long TotalCount)> GetByAccountAsync(
        string accountId, DateTime? dateFrom = null, DateTime? dateTo = null,
        int skip = 0, int limit = 20, CancellationToken ct = default)
    {
        // START_BLOCK_GET_BY_ACCOUNT
        _logger.LogInformation(
            "[PaymentService.Persistence][PaymentDocumentRepository][BLOCK_GET_BY_ACCOUNT] " +
            "Querying payments for account {AccountId} " +
            "(dateFrom={DateFrom}, dateTo={DateTo}, skip={Skip}, limit={Limit})",
            accountId,
            dateFrom?.ToString("yyyy-MM-dd") ?? "null",
            dateTo?.ToString("yyyy-MM-dd") ?? "null",
            skip, limit);

        var senderFilter = Builders<PaymentDocument>.Filter.Eq(p => p.Request.SenderAccount, accountId);
        var receiverFilter = Builders<PaymentDocument>.Filter.Eq(p => p.Request.ReceiverAccount, accountId);
        var accountFilter = Builders<PaymentDocument>.Filter.Or(senderFilter, receiverFilter);

        // Add date range filter
        var finalFilter = accountFilter;
        if (dateFrom.HasValue || dateTo.HasValue)
        {
            var dateFilters = new List<FilterDefinition<PaymentDocument>>();

            if (dateFrom.HasValue)
                dateFilters.Add(Builders<PaymentDocument>.Filter.Gte(p => p.CreatedAt, dateFrom.Value));

            if (dateTo.HasValue)
            {
                // Inclusive end-of-day for DateTo
                var endOfDay = dateTo.Value.Date.AddDays(1).AddTicks(-1);
                dateFilters.Add(Builders<PaymentDocument>.Filter.Lte(p => p.CreatedAt, endOfDay));
            }

            finalFilter = accountFilter & Builders<PaymentDocument>.Filter.And(dateFilters);
        }

        var totalCount = await _context.Payments.CountDocumentsAsync(finalFilter, null, ct);

        var sort = Builders<PaymentDocument>.Sort.Descending(p => p.CreatedAt);

        var payments = await _context.Payments
            .Find(finalFilter)
            .Sort(sort)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync(ct);

        _logger.LogInformation(
            "[PaymentService.Persistence][PaymentDocumentRepository][BLOCK_GET_BY_ACCOUNT] " +
            "Found {Count} payments for account {AccountId} (total: {Total})",
            payments.Count, accountId, totalCount);

        return (payments, totalCount);
        // END_BLOCK_GET_BY_ACCOUNT
    }
}
