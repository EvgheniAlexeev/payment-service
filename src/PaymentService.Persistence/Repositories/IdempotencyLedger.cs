// FILE: src/PaymentService.Persistence/Repositories/IdempotencyLedger.cs
// VERSION: 2.0.0
// MODULE: M-MONGO
// PURPOSE: MongoDB repository pattern implementation
// SEMANTIC_TAG: [REPOSITORY, DATA_ACCESS]
// START_MODULE M_MONGO

// FILE: src/PaymentService.Persistence/Repositories/IdempotencyLedger.cs
// VERSION: 1.0.0

using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using PaymentService.Persistence.MongoDB;

namespace PaymentService.Persistence.Repositories;

/// <summary>
/// BLOCK_IDEMPOTENCY MongoDB-backed idempotency ledger.
/// Uses unique compound index for deduplication.
/// </summary>
public class IdempotencyLedger : IIdempotencyLedger
{
    private readonly MongoDbContext _context;
    private readonly ILogger<IdempotencyLedger> _logger;

    public IdempotencyLedger(MongoDbContext context, ILogger<IdempotencyLedger> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> TryMarkCompleteAsync(
        string correlationId, string stepName, IClientSessionHandle? session = null, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[PaymentService.Persistence][IdempotencyLedger][BLOCK_IDEMPOTENCY_MARK] " +
            "Marking step complete {CorrelationId} {StepName}", correlationId, stepName);

        var entry = new IdempotencyEntry
        {
            Id = $"{correlationId}_{stepName}",
            CorrelationId = correlationId,
            StepName = stepName,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            if (session != null)
                await _context.IdempotencyLedger.InsertOneAsync(session, entry, null, ct);
            else
                await _context.IdempotencyLedger.InsertOneAsync(entry, null, ct);

            return true; // Successfully marked
        }
        catch (MongoWriteException ex) when (ex.Message.Contains("E11000"))
        {
            _logger.LogInformation(
                "[PaymentService.Persistence][IdempotencyLedger][BLOCK_IDEMPOTENCY_CHECK] " +
                "Step already complete, skipping {CorrelationId} {StepName}", correlationId, stepName);
            return false; // Already marked
        }
    }

    public async Task<bool> IsStepCompleteAsync(
        string correlationId, string stepName, CancellationToken ct = default)
    {
        var filter = Builders<IdempotencyEntry>.Filter.And(
            Builders<IdempotencyEntry>.Filter.Eq(i => i.CorrelationId, correlationId),
            Builders<IdempotencyEntry>.Filter.Eq(i => i.StepName, stepName));

        var count = await _context.IdempotencyLedger.CountDocumentsAsync(filter, null, ct);
        return count > 0;
    }
}
