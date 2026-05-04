// FILE: src/PaymentService.Api.ReaderService/Handlers/IGetPaymentHandler.cs
// VERSION: 1.0.0

using PaymentService.Api.ReaderService.Models;
using PaymentService.Shared;
using PaymentService.Shared.Dtos;

namespace PaymentService.Api.ReaderService.Handlers;

/// <summary>
/// Handler interface for payment query operations.
/// </summary>
public interface IGetPaymentHandler
{
    /// <summary>
    /// Retrieve a single payment by correlation ID.
    /// </summary>
    Task<Result<PaymentStatusDto>> HandleAsync(GetPaymentRequest request, CancellationToken ct);

    /// <summary>
    /// Query payments filtered by status with pagination.
    /// </summary>
    Task<Result<PagedPaymentStatusResponse>> HandleQueryAsync(
        GetPaymentsByStatusRequest request, CancellationToken ct);
}
