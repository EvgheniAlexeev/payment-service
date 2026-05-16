// FILE: src/PaymentService.Api.ReaderService/Features/GetPayment/GetPaymentHandler.cs
// VERSION: 2.0.0
// MODULE: M-READER
// PURPOSE: Business logic handler for query operations
// SEMANTIC_TAG: [HANDLER, QUERY_PROCESSOR]
// START_MODULE M_READER

// FILE: src/PaymentService.Api.ReaderService/Features/GetPayment/GetPaymentHandler.cs
// VERSION: 1.0.0

using Microsoft.Extensions.Logging;
using PaymentService.Persistence.Repositories;
using PaymentService.Shared;
using PaymentService.Shared.Dtos;

namespace PaymentService.Api.ReaderService.Features.GetPayment;

/// <summary>
/// BLOCK_GET_PAYMENT_HANDLER — Payment retrieval handler.
/// VSA feature: GetPayment (ReaderService)
/// </summary>
public class GetPaymentHandler
{
    private readonly IPaymentDocumentRepository _repository;
    private readonly ILogger<GetPaymentHandler> _logger;

    public GetPaymentHandler(
        IPaymentDocumentRepository repository,
        ILogger<GetPaymentHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<PaymentStatusDto>> HandleAsync(string correlationId, CancellationToken ct)
    {
        // START_BLOCK_GET_PAYMENT_HANDLER
        try
        {
            _logger.LogInformation(
                "[PaymentService.Api.ReaderService][Features.GetPayment][GetPaymentHandler] " +
                "Fetching payment {CorrelationId}", correlationId);

            var payment = await _repository.GetByCorrelationIdAsync(correlationId, ct);

            if (payment == null)
            {
                _logger.LogWarning(
                    "[PaymentService.Api.ReaderService][Features.GetPayment][GetPaymentHandler] " +
                    "Payment not found {CorrelationId}", correlationId);
                return Result<PaymentStatusDto>.NotFound(
                    $"Payment not found: {correlationId}");
            }

            var dto = MapToStatusDto(payment);

            _logger.LogInformation(
                "[PaymentService.Api.ReaderService][Features.GetPayment][GetPaymentHandler] " +
                "Payment retrieved successfully {CorrelationId}", correlationId);

            return Result<PaymentStatusDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PaymentService.Api.ReaderService][Features.GetPayment][GetPaymentHandler] " +
                "Error fetching payment {CorrelationId}", correlationId);
            return Result<PaymentStatusDto>.Failure("Internal server error");
        }
        // END_BLOCK_GET_PAYMENT_HANDLER
    }

    private static PaymentStatusDto MapToStatusDto(Shared.Models.PaymentDocument payment)
    {
        return new PaymentStatusDto
        {
            CorrelationId = payment.CorrelationId,
            Status = payment.Status,
            SagaState = payment.SagaState,
            Amount = payment.Request.Amount,
            Currency = payment.Request.Currency,
            CreatedAt = payment.CreatedAt,
            SettledAt = payment.SettledAt,
            SenderAccount = payment.Request.SenderAccount,
            ReceiverAccount = payment.Request.ReceiverAccount
        };
    }
}
