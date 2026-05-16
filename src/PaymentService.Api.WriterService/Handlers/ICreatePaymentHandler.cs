// FILE: src/PaymentService.Api.WriterService/Handlers/ICreatePaymentHandler.cs
// VERSION: 2.0.0
// MODULE: M-WRITER
// PURPOSE: Business logic handler for command operations
// SEMANTIC_TAG: [HANDLER, COMMAND_PROCESSOR]
// START_MODULE M_WRITER

// FILE: src/PaymentService.Api.WriterService/Handlers/ICreatePaymentHandler.cs
// VERSION: 1.0.0

using PaymentService.Api.WriterService.Models;
using PaymentService.Shared;

namespace PaymentService.Api.WriterService.Handlers;

/// <summary>
/// Handler interface for payment creation operations.
/// </summary>
public interface ICreatePaymentHandler
{
    /// <summary>
    /// Handle a payment creation request: validates, persists, and publishes saga command.
    /// </summary>
    Task<Result<CreatePaymentResponse>> HandleAsync(CreatePaymentRequest request, CancellationToken ct);
}
