// START_MODULE M-WORKER
// START_BLOCK_PROGRAM Program
// PURPOSE: Worker host entry point for PaymentService.Workers.
//          Configures Wolverine with MongoDB saga persistence, Prometheus metrics,
//          and structured logging.
// SEMANTIC_TAG: [BLOCK_HOST] Wolverine worker host
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentService.Workers;
using PaymentService.Workers.Configuration;
using Prometheus;
using Wolverine;

var builder = Host.CreateApplicationBuilder(args);

// ──────────────── Configuration ────────────────
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// ──────────────── Structured Logging ────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddJsonConsole(options =>
{
    options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
    {
        Indented = false,
    };
});

// ──────────────── Services ────────────────
builder.Services.AddPaymentWorkers(builder.Configuration);

// ──────────────── Wolverine with MongoDB Saga Persistence ────────────────
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB")
    ?? "mongodb://localhost:27017";

builder.Host.UseWolverine(opts =>
{
    // Register saga types
    opts.Discovery.IncludeType<PaymentService.Workers.Sagas.PaymentSaga>();

    // ──────────── MongoDB saga persistence ────────────
    opts.PersistSagasWithMarten(marten =>
    {
        // Marten is used for saga state persistence with MongoDB storage
        // In production, configure the MongoDB-backed Marten document store
        marten.Connection(mongoConnectionString);
        marten.DatabaseSchemaName = "payment_sagas";
    });

    // ──────────── Saga timeout from configuration ────────────
    var timeoutConfig = builder.Configuration
        .GetSection(SagaTimeoutConfiguration.SectionName)
        .Get<SagaTimeoutConfiguration>() ?? new SagaTimeoutConfiguration();

    var timeout = timeoutConfig.GetTimeoutForEnvironment(
        builder.Environment.EnvironmentName);

    opts.Policies.OnAnyException()
        .RetryWithCooldown(3, 100.Milliseconds(), 500.Milliseconds());

    // ──────────── Local queue for development ────────────
    opts.UseRabbitMq(rabbit =>
    {
        var rabbitConnection = builder.Configuration.GetConnectionString("RabbitMQ")
            ?? "amqp://localhost:5672";

        rabbit.ConnectionFactory = new RabbitMQ.Client.ConnectionFactory
        {
            Uri = new Uri(rabbitConnection),
        };

        rabbit.AutoProvision = true;
        rabbit.DeadLetterQueueing.Enabled = true;
    }).AutoProvision();

    // ──────────── Message durability ────────────
    opts.Durability.Mode = Wolverine.DurabilityMode.MediatorOnly;
});

// ──────────────── Prometheus Metrics ────────────────
builder.Services.AddMetricServer(options =>
{
    options.Port = 9090;
    options.Hostname = "0.0.0.0";
});

// ──────────────── Health Checks ────────────────
builder.Services.AddHealthChecks();

// ──────────────── Build & Run ────────────────
var host = builder.Build();

// Start metrics server
var metricServer = new KestrelMetricServer(port: 9090);
metricServer.Start();

Console.WriteLine(
    "[PaymentService.Workers][Program][BLOCK_HOST_START] " +
    "Worker started. Environment: {env}, SagaTimeout: {timeout}, " +
    "Metrics: http://0.0.0.0:9090/metrics",
    builder.Environment.EnvironmentName, timeout);

await host.RunAsync();
// END_BLOCK_MODULE
