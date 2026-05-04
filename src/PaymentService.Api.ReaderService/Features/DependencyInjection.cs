// FILE: src/PaymentService.Api.ReaderService/Features/DependencyInjection.cs
// VERSION: 1.0.0

using Microsoft.Extensions.DependencyInjection;
using PaymentService.Api.ReaderService.Features.GetPayment;
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
        services.AddValidatorsFromAssemblyContaining<GetPaymentValidator>();

        // QueryPayments feature
        services.AddScoped<QueryPaymentsHandler>();
        services.AddScoped<QueryPaymentsEndpoint>();
        services.AddValidatorsFromAssemblyContaining<QueryPaymentsValidator>();

        return services;
    }
}
