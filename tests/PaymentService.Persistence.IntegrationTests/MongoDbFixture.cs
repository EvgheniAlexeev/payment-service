// FILE: tests/PaymentService.Persistence.IntegrationTests/MongoDbFixture.cs
// VERSION: 2.0.0
// MODULE: M-INTEGRATION
// PURPOSE: Test specification
// SEMANTIC_TAG: [TEST]
// START_MODULE M_INTEGRATION

// FILE: tests/PaymentService.Persistence.IntegrationTests/MongoDbFixture.cs
// VERSION: 1.0.0

using FakeItEasy;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using PaymentService.Persistence.MongoDB;
using Testcontainers.MongoDb;

namespace PaymentService.Persistence.IntegrationTests;

/// <summary>
/// Test fixture providing a real MongoDB container via Testcontainers.
/// Implements IAsyncLifetime for xUnit lifecycle.
/// </summary>
public class MongoDbFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container;
    private IMongoClient? _client;

    public MongoDbContext? Context { get; private set; }
    public IMongoDatabase? Database { get; private set; }
    public ILogger<MongoDbFixture>? Logger { get; private set; }

    public MongoDbFixture()
    {
        _container = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .WithCleanUp(true)
            .Build();

        Logger = A.Fake<ILogger<MongoDbFixture>>();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();
        _client = new MongoClient(connectionString);
        Database = _client.GetDatabase($"payment_service_test_{Guid.NewGuid():N}");
        Context = new MongoDbContext(Database);

        await IndexConfiguration.EnsureIndexesAsync(Database, Logger);
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
            await _container.DisposeAsync();
    }

    public IMongoCollection<Shared.Models.PaymentDocument> Payments =>
        Context!.Payments;

    public IMongoCollection<IdempotencyEntry> Idempotency =>
        Context!.IdempotencyLedger;
}
