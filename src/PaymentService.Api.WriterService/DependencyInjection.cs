// FILE: src/PaymentService.Api.WriterService/DependencyInjection.cs
// VERSION: 2.0.0
// MODULE: M-WRITER
// PURPOSE: Service registration for Writer API
// SEMANTIC_TAG: [DI_EXTENSION]
// START_MODULE M_WRITER

// FILE: src/PaymentService.Api.WriterService/DependencyInjection.cs
// VERSION: 1.0.0

using PaymentService.Api.WriterService.Handlers;
using PaymentService.Api.WriterService.Validators;

namespace PaymentService.Api.WriterService;

/// <summary>
/// DI registration for PaymentService.Api.WriterService module.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Register writer API services.
    /// </summary>
    public static IServiceCollection AddPaymentWriterApi(this IServiceCollection services)
    {
        services.AddScoped<CreatePaymentValidator>();
        services.AddScoped<ICreatePaymentHandler, CreatePaymentHandler>();

        return services;
    }
}
