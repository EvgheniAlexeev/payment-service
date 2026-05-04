// START_MODULE M-WORKER
// START_BLOCK_DI DependencyInjection
// PURPOSE: Extension method to register all Worker services in the DI container.
//          Registers: saga, step handlers, external services, metrics, configuration, Wolverine.
// SEMANTIC_TAG: [BLOCK_DI] Registering PaymentService.Workers services
namespace PaymentService.Workers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Workers.Configuration;
using PaymentService.Workers.Metrics;
using PaymentService.Workers.Sagas;
using PaymentService.Workers.Services;
using PaymentService.Workers.Steps;
using Polly;
using Polly.Extensions.Http;

/// <summary>
/// Dependency injection registration for PaymentService.Workers module.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all PaymentService.Workers services: saga, handlers, external clients, metrics.
    /// </summary>
    public static IServiceCollection AddPaymentWorkers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ──────────────── Configuration ────────────────
        services.Configure<SagaTimeoutConfiguration>(
            configuration.GetSection(SagaTimeoutConfiguration.SectionName));
        services.Configure<ServiceConfiguration>(
            configuration.GetSection(ServiceConfiguration.SectionName));

        // ──────────────── Metrics ────────────────
        services.AddSingleton<PaymentSagaMetrics>();

        // ──────────────── Saga ────────────────
        services.AddScoped<PaymentSaga>();

        // ──────────────── Step Handlers ────────────────
        services.AddScoped<ValidatePaymentHandler>();
        services.AddScoped<ReserveFundsHandler>();
        services.AddScoped<SettlePaymentHandler>();

        // ──────────────── Services (abstractions) ────────────────
        services.AddScoped<IValidationService, DefaultValidationService>();
        services.AddScoped<ILedgerService, DefaultLedgerService>();
        services.AddScoped<ISettlementService, DefaultSettlementService>();
        services.AddScoped<IDLQPublisher, LoggingDLQPublisher>();

        // ──────────────── HTTP Clients with Polly Retry ────────────────
        var serviceConfig = configuration
            .GetSection(ServiceConfiguration.SectionName)
            .Get<ServiceConfiguration>() ?? new ServiceConfiguration();

        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: serviceConfig.MaxRetries,
                sleepDurationProvider: retryAttempt =>
                {
                    var delay = serviceConfig.RetryBaseDelayMs *
                                Math.Pow(2.0, retryAttempt - 1);
                    var jitter = Random.Shared.Next(0, 100);
                    return TimeSpan.FromMilliseconds(
                        Math.Min(delay + jitter, serviceConfig.RetryMaxDelayMs));
                });

        services.AddHttpClient("ValidationService", client =>
        {
            client.BaseAddress = new Uri(serviceConfig.ValidationServiceUrl);
            client.Timeout = serviceConfig.HttpTimeout;
        }).AddPolicyHandler(retryPolicy);

        services.AddHttpClient("LedgerService", client =>
        {
            client.BaseAddress = new Uri(serviceConfig.LedgerServiceUrl);
            client.Timeout = serviceConfig.HttpTimeout;
        }).AddPolicyHandler(retryPolicy);

        services.AddHttpClient("SettlementService", client =>
        {
            client.BaseAddress = new Uri(serviceConfig.SettlementServiceUrl);
            client.Timeout = serviceConfig.HttpTimeout;
        }).AddPolicyHandler(retryPolicy);

        return services;
    }
}
// END_BLOCK_DI
