// FILE: src/PaymentService.Persistence/MongoDB/MongoDbContext.cs
// VERSION: 1.0.0

using MongoDB.Driver;

namespace PaymentService.Persistence.MongoDB;

/// <summary>
/// BLOCK_MONGO_CONTEXT — MongoDB connection context for PaymentService.
/// Provides typed collections for payments, saga state, and idempotency ledger.
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IMongoClient client, string databaseName)
    {
        _database = client.GetDatabase(databaseName);
    }

    public MongoDbContext(IMongoDatabase database)
    {
        _database = database;
    }

    /// <summary>Payments collection.</summary>
    public virtual IMongoCollection<Shared.Models.PaymentDocument> Payments =>
        _database.GetCollection<Shared.Models.PaymentDocument>("payments");

    /// <summary>Saga state collection.</summary>
    public virtual IMongoCollection<Shared.Models.SagaState> SagaStates =>
        _database.GetCollection<Shared.Models.SagaState>("saga_states");

    /// <summary>Idempotency ledger collection.</summary>
    public virtual IMongoCollection<IdempotencyEntry> IdempotencyLedger =>
        _database.GetCollection<IdempotencyEntry>("idempotency_ledger");

    /// <summary>The underlying database instance.</summary>
    public IMongoDatabase Database => _database;
}

/// <summary>
/// Idempotency ledger entry — tracks completed saga steps.
/// </summary>
public record IdempotencyEntry
{
    public string Id { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public string StepName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
