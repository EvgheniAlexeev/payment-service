// FILE: tests/.../ReaderService.IntegrationTests/ApiIntegrationFixture.cs
// VERSION: 1.0.0

using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using PaymentService.Persistence.MongoDB;
using PaymentService.Shared.Dtos;
using PaymentService.Shared.Models;
using Testcontainers.MongoDb;

namespace PaymentService.Api.ReaderService.IntegrationTests;

/// <summary>
/// Integration test fixture using Testcontainers MongoDB + WebApplicationFactory.
/// Provides seeded HttpClient and direct MongoDB access for assertions.
/// </summary>
public class ApiIntegrationFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer;
    private IMongoClient? _mongoClient;
    private IMongoDatabase? _database;
    private MongoDbContext? _dbContext;

    public HttpClient Client { get; private set; } = null!;
    public IMongoCollection<PaymentDocument> Payments => _database!.GetCollection<PaymentDocument>("payments");

    private WebApplicationFactory<Program> _factory = null!;

    public ApiIntegrationFixture()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();

        var connectionString = _mongoContainer.GetConnectionString();
        var dbName = $"reader_test_{Guid.NewGuid():N}";

        _mongoClient = new MongoClient(connectionString);
        _database = _mongoClient.GetDatabase(dbName);
        _dbContext = new MongoDbContext(_database);

        // Ensure indexes
        await IndexConfiguration.EnsureIndexesAsync(
            _database,
            MockLogger());

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace real MongoDB with test container
                    services.RemoveAll<IMongoClient>();
                    services.AddSingleton(_mongoClient);

                    services.RemoveAll<MongoDbContext>();
                    services.AddSingleton(_dbContext);
                });

                builder.UseEnvironment("Development");
            });

        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        _factory?.Dispose();
        if (_mongoContainer != null)
            await _mongoContainer.DisposeAsync();
    }

    /// <summary>
    /// Seed a payment document directly into MongoDB.
    /// </summary>
    public async Task SeedPaymentAsync(PaymentDocument payment)
    {
        payment = payment with
        {
            Id = payment.Id is { Length: > 0 } ? payment.Id : Guid.NewGuid().ToString(),
            CreatedAt = payment.CreatedAt == default ? DateTime.UtcNow : payment.CreatedAt
        };
        await Payments.InsertOneAsync(payment);
    }

    /// <summary>
    /// Seed multiple payments at once.
    /// </summary>
    public async Task SeedPaymentsAsync(params PaymentDocument[] payments)
    {
        if (payments.Length > 0)
        {
            await Payments.InsertManyAsync(payments);
        }
    }

    /// <summary>
    /// Direct MongoDB query helper.
    /// </summary>
    public async Task<PaymentDocument?> GetPaymentAsync(string correlationId)
    {
        var filter = Builders<PaymentDocument>.Filter.Eq(p => p.CorrelationId, correlationId);
        return await Payments.Find(filter).FirstOrDefaultAsync();
    }

    private static ILogger MockLogger()
    {
        using var factory = LoggerFactory.Create(b => b.AddConsole());
        return factory.CreateLogger("Test");
    }
}
