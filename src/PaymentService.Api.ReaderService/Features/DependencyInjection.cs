// FILE: src/PaymentService.Api.ReaderService/Features/DependencyInjection.cs
// VERSION: 2.0.0
// MODULE: M-READER
// PURPOSE: Extension methods for service registration and configuration
// SEMANTIC_TAG: [DI_EXTENSION, SERVICE_REGISTRATION]
// START_MODULE M_READER

// FILE: src/PaymentService.Api.ReaderService/Features/DependencyInjection.cs
// VERSION: 1.0.0

using Microsoft.Extensions.DependencyInjection;
using PaymentService.Api.ReaderService.Features.GetPayment;
using PaymentService.Api.ReaderService.Features.GetTransactions;
using PaymentService.Api.ReaderService.Features.QueryPayments;

namespace PaymentService.Api.ReaderService.Features;

/// <summary>
/// Registers VSA feature handlers and validators for the ReaderService.
/// </summary>
public static class VsaFeatureRegistration
{
    public static IServiceCollection AddReaderServiceFeatures(this IServiceCollection services)
    {
        // GetPayment feature
        services.AddScoped<GetPaymentHandler>();
        services.AddScoped<GetPaymentEndpoint>();
        // services.AddValidatorsFromAssemblyContaining<GetPaymentValidator>();

        // QueryPayments feature
        services.AddScoped<QueryPaymentsHandler>();
        services.AddScoped<QueryPaymentsEndpoint>();
        // services.AddValidatorsFromAssemblyContaining<QueryPaymentsValidator>();

        // GetTransactions feature
        services.AddScoped<GetTransactionsHandler>();
        services.AddScoped<GetTransactionsEndpoint>();
        services.AddScoped<GetTransactionsValidator>();

        return services;
    }
}
