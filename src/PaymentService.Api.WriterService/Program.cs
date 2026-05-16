// FILE: src/PaymentService.Api.WriterService/Program.cs
// VERSION: 2.0.0
// MODULE: M-WRITER
// PURPOSE: ASP.NET Core bootstrapping for Writer API
// SEMANTIC_TAG: [API_BOOTSTRAP]
// START_MODULE M_WRITER

// FILE: src/PaymentService.Api.WriterService/Program.cs
// VERSION: 1.0.0

using PaymentService.Api.WriterService;
using PaymentService.Api.WriterService.Handlers;
using PaymentService.Persistence;
using PaymentService.Persistence.MongoDB;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MongoDB registration
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDb")
    ?? "mongodb://localhost:27017";
var mongoDatabaseName = builder.Configuration["MongoDb:DatabaseName"] ?? "payment_service";

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return new MongoDbContext(client, mongoDatabaseName);
});

// Register persistence and writer services
builder.Services.AddPaymentPersistence();

// MessagePublisher registration (Wolverine stub for Phase-2)
// In production, replace with Wolverine/Dapr pub-sub integration
builder.Services.AddSingleton<IMessagePublisher>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<InMemoryMessagePublisher>>();
    return new InMemoryMessagePublisher(logger);
});

builder.Services.AddPaymentWriterApi();

var app = builder.Build();

// Ensure indexes at startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await IndexConfiguration.EnsureIndexesAsync(dbContext.Database, logger);
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

/// <summary>
/// In-memory message publisher for Phase-2 testing.
/// Records published messages for verification.
/// </summary>
public class InMemoryMessagePublisher : IMessagePublisher
{
    private readonly ILogger<InMemoryMessagePublisher> _logger;
    private readonly List<object> _publishedMessages = new();

    public IReadOnlyList<object> PublishedMessages => _publishedMessages;

    public InMemoryMessagePublisher(ILogger<InMemoryMessagePublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(T message, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[PaymentService.Api.WriterService][InMemoryMessagePublisher][BLOCK_PUBLISH] " +
            "Publishing message type={Type}", typeof(T).Name);

        _publishedMessages.Add(message!);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get published messages of a specific type.
    /// </summary>
    public List<T> GetPublishedMessagesOfType<T>() =>
        _publishedMessages.OfType<T>().ToList();

    /// <summary>
    /// Clear published messages.
    /// </summary>
    public void Clear() => _publishedMessages.Clear();
}

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }
