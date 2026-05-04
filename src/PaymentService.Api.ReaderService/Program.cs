// FILE: src/PaymentService.Api.ReaderService/Program.cs
// VERSION: 1.0.0

using Microsoft.Extensions.Logging;
using PaymentService.Api.ReaderService;
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

// Register persistence and reader services
builder.Services.AddPaymentPersistence();
builder.Services.AddPaymentReaderApi();

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

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }
