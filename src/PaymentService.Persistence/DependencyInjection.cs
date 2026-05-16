// FILE: src/PaymentService.Persistence/DependencyInjection.cs
// VERSION: 2.0.0
// MODULE: M-MONGO
// PURPOSE: MongoDB service registration
// SEMANTIC_TAG: [DI_EXTENSION, MONGODB]
// START_MODULE M_MONGO

// FILE: src/PaymentService.Persistence/DependencyInjection.cs
// VERSION: 1.0.0

using Microsoft.Extensions.DependencyInjection;
using PaymentService.Persistence.Repositories;

namespace PaymentService.Persistence;

/// <summary>
/// DI registration for PaymentService.Persistence module.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Register persistence layer services.
    /// </summary>
    public static IServiceCollection AddPaymentPersistence(this IServiceCollection services)
    {
        services.AddScoped<IPaymentDocumentRepository, PaymentDocumentRepository>();
        services.AddScoped<IIdempotencyLedger, IdempotencyLedger>();

        return services;
    }
}
