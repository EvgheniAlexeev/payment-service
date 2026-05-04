// FILE: tests/.../WriterService.IntegrationTests/WriterApiFixture.cs
// VERSION: 1.0.0

using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using PaymentService.Api.WriterService.Handlers;
using PaymentService.Persistence.MongoDB;
using PaymentService.Shared.Commands;
using PaymentService.Shared.Models;
using Testcontainers.MongoDb;

namespace PaymentService.Api.WriterService.IntegrationTests;

/// <summary>
/// Integration test fixture for writer API using Testcontainers MongoDB + WebApplicationFactory.
/// </summary>
public class WriterApiFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer;
    private IMongoClient? _mongoClient;
    private IMongoDatabase? _database;
    private MongoDbContext? _dbContext;

    public HttpClient Client { get; private set; } = null!;
    public InMemoryMessagePublisher MessagePublisher { get; private set; } = null!;
    public IMongoCollection<PaymentDocument> Payments =>
        _database!.GetCollection<PaymentDocument>("payments");

    private WebApplicationFactory<Program> _factory = null!;

    public WriterApiFixture()
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
        var dbName = $"writer_test_{Guid.NewGuid():N}";

        _mongoClient = new MongoClient(connectionString);
        _database = _mongoClient.GetDatabase(dbName);
        _dbContext = new MongoDbContext(_database);

        await IndexConfiguration.EnsureIndexesAsync(
            _database,
            MockLogger());

        // Create the message publisher ahead of time so we can capture it
        var publisherLogger = MockLogger("Publisher");
        MessagePublisher = new InMemoryMessagePublisher(publisherLogger);

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IMongoClient>();
                    services.AddSingleton(_mongoClient);

                    services.RemoveAll<MongoDbContext>();
                    services.AddSingleton(_dbContext);

                    // Replace message publisher with our tracked instance
                    services.RemoveAll<IMessagePublisher>();
                    services.AddSingleton<IMessagePublisher>(MessagePublisher);
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
    /// Get payment by correlationId from the test database.
    /// </summary>
    public async Task<PaymentDocument?> GetPaymentAsync(string correlationId)
    {
        var filter = Builders<PaymentDocument>.Filter.Eq(p => p.CorrelationId, correlationId);
        return await Payments.Find(filter).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Get published PaymentCommands from the tracked publisher.
    /// </summary>
    public List<PaymentCommand> GetPublishedCommands() =>
        MessagePublisher.GetPublishedMessagesOfType<PaymentCommand>();

    private static ILogger MockLogger(string name = "Test")
    {
        using var factory = LoggerFactory.Create(b => b.AddConsole());
        return factory.CreateLogger(name);
    }
}
