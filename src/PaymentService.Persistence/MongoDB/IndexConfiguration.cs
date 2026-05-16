// FILE: src/PaymentService.Persistence/MongoDB/IndexConfiguration.cs
// VERSION: 2.0.0
// MODULE: M-MONGO
// PURPOSE: MongoDB collection indexing configuration
// SEMANTIC_TAG: [INDEX_CONFIG, SCHEMA]
// START_MODULE M_MONGO

// FILE: src/PaymentService.Persistence/MongoDB/IndexConfiguration.cs
// VERSION: 1.0.0

using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using PaymentService.Shared.Models;

namespace PaymentService.Persistence.MongoDB;

/// <summary>
/// BLOCK_INDEX_SETUP — Ensures MongoDB indexes for PaymentService collections.
/// </summary>
public static class IndexConfiguration
{
    /// <summary>
    /// Ensure all required indexes exist.
    /// </summary>
    public static async Task EnsureIndexesAsync(
        IMongoDatabase database,
        ILogger? logger = null,
        CancellationToken ct = default)
    {
        logger?.LogInformation(
            "[PaymentService.Persistence][IndexConfiguration][BLOCK_INDEX_SETUP] " +
            "Ensuring MongoDB indexes");

        await EnsurePaymentIndexesAsync(database, logger, ct);
        await EnsureSagaStateIndexesAsync(database, logger, ct);
        await EnsureIdempotencyIndexesAsync(database, logger, ct);

        logger?.LogInformation(
            "[PaymentService.Persistence][IndexConfiguration][BLOCK_INDEX_SETUP] " +
            "MongoDB indexes ensured successfully");
    }

    private static async Task EnsurePaymentIndexesAsync(
        IMongoDatabase database, ILogger? logger, CancellationToken ct)
    {
        var collection = database.GetCollection<PaymentDocument>("payments");

        // Unique index on CorrelationId (query key)
        var correlationIndex = new CreateIndexModel<PaymentDocument>(
            Builders<PaymentDocument>.IndexKeys.Ascending(p => p.CorrelationId),
            new CreateIndexOptions { Unique = true, Name = "idx_correlation_id" });

        // Index on Status for query filtering
        var statusIndex = new CreateIndexModel<PaymentDocument>(
            Builders<PaymentDocument>.IndexKeys
                .Ascending(p => p.Status)
                .Ascending(p => p.CreatedAt),
            new CreateIndexOptions { Name = "idx_status_created" });

        // Index on CreatedAt for time-range queries
        var createdIndex = new CreateIndexModel<PaymentDocument>(
            Builders<PaymentDocument>.IndexKeys.Descending(p => p.CreatedAt),
            new CreateIndexOptions { Name = "idx_created_desc" });

        await collection.Indexes.CreateManyAsync(
            new[] { correlationIndex, statusIndex, createdIndex }, ct);

        logger?.LogInformation(
            "[PaymentService.Persistence][IndexConfiguration][BLOCK_INDEX_SETUP] " +
            "Payment indexes created");
    }

    private static async Task EnsureSagaStateIndexesAsync(
        IMongoDatabase database, ILogger? logger, CancellationToken ct)
    {
        var collection = database.GetCollection<SagaState>("saga_states");

        var correlationIndex = new CreateIndexModel<SagaState>(
            Builders<SagaState>.IndexKeys.Ascending(s => s.CorrelationId),
            new CreateIndexOptions { Unique = true, Name = "idx_saga_correlation_id" });

        await collection.Indexes.CreateOneAsync(correlationIndex, null, ct);

        logger?.LogInformation(
            "[PaymentService.Persistence][IndexConfiguration][BLOCK_INDEX_SETUP] " +
            "Saga state indexes created");
    }

    private static async Task EnsureIdempotencyIndexesAsync(
        IMongoDatabase database, ILogger? logger, CancellationToken ct)
    {
        var collection = database.GetCollection<IdempotencyEntry>("idempotency_ledger");

        // Compound index for idempotency check
        var stepIndex = new CreateIndexModel<IdempotencyEntry>(
            Builders<IdempotencyEntry>.IndexKeys
                .Ascending(i => i.CorrelationId)
                .Ascending(i => i.StepName),
            new CreateIndexOptions { Unique = true, Name = "idx_correlation_step" });

        // TTL index: auto-delete after 30 days
        var ttlIndex = new CreateIndexModel<IdempotencyEntry>(
            Builders<IdempotencyEntry>.IndexKeys.Ascending(i => i.CreatedAt),
            new CreateIndexOptions
            {
                ExpireAfter = TimeSpan.FromDays(30),
                Name = "idx_idempotency_ttl"
            });

        await collection.Indexes.CreateManyAsync(new[] { stepIndex, ttlIndex }, ct);

        logger?.LogInformation(
            "[PaymentService.Persistence][IndexConfiguration][BLOCK_INDEX_SETUP] " +
            "Idempotency indexes created");
    }
}
