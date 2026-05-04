// FILE: src/PaymentService.Api.ReaderService/DependencyInjection.cs
// VERSION: 1.0.0

using PaymentService.Api.ReaderService.Handlers;

namespace PaymentService.Api.ReaderService;

/// <summary>
/// DI registration for PaymentService.Api.ReaderService module.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Register reader API services.
    /// </summary>
    public static IServiceCollection AddPaymentReaderApi(this IServiceCollection services)
    {
        services.AddScoped<IGetPaymentHandler, GetPaymentHandler>();

        return services;
    }
}
