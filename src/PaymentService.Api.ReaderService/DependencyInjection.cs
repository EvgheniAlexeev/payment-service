// FILE: src/PaymentService.Api.ReaderService/DependencyInjection.cs
// VERSION: 2.0.0
// MODULE: M-READER
// PURPOSE: Extension methods for service registration and configuration
// SEMANTIC_TAG: [DI_EXTENSION, SERVICE_REGISTRATION]
// START_MODULE M_READER

// FILE: src/PaymentService.Api.ReaderService/DependencyInjection.cs
// VERSION: 1.1.0

using PaymentService.Api.ReaderService.Features;
using PaymentService.Api.ReaderService.Handlers;

namespace PaymentService.Api.ReaderService;

/// <summary>
/// DI registration for PaymentService.Api.ReaderService module.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Register reader API services (legacy handlers + VSA features).
    /// </summary>
    public static IServiceCollection AddPaymentReaderApi(this IServiceCollection services)
    {
        // Legacy handler (backward compat)
        services.AddScoped<IGetPaymentHandler, GetPaymentHandler>();

        // VSA feature registration
        services.AddReaderServiceFeatures();

        return services;
    }
}
