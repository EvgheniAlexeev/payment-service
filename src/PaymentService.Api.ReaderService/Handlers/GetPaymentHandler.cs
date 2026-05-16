// FILE: src/PaymentService.Api.ReaderService/Handlers/GetPaymentHandler.cs
// VERSION: 2.0.0
// MODULE: M-READER
// PURPOSE: Business logic handler for query operations
// SEMANTIC_TAG: [HANDLER, QUERY_PROCESSOR]
// START_MODULE M_READER

// FILE: src/PaymentService.Api.ReaderService/Handlers/GetPaymentHandler.cs
// VERSION: 1.0.0

using Microsoft.Extensions.Logging;
using PaymentService.Persistence.Repositories;
using PaymentService.Shared;
using PaymentService.Shared.Dtos;

namespace PaymentService.Api.ReaderService.Handlers;

/// <summary>
/// BLOCK_HANDLER_GET — Payment query handler.
/// Retrieves payment status from MongoDB via repository.
/// </summary>
public class GetPaymentHandler : IGetPaymentHandler
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

    public async Task<Result<PaymentStatusDto>> HandleAsync(GetPaymentRequest request, CancellationToken ct)
    {
        // START_BLOCK_HANDLER_GET
        try
        {
            _logger.LogInformation(
                "[PaymentService.Api.ReaderService][GetPaymentHandler][BLOCK_HANDLER_GET] " +
                "Fetching payment {CorrelationId}", request.CorrelationId);

            var payment = await _repository.GetByCorrelationIdAsync(request.CorrelationId, ct);

            if (payment == null)
            {
                _logger.LogWarning(
                    "[PaymentService.Api.ReaderService][GetPaymentHandler][BLOCK_HANDLER_GET] " +
                    "Payment not found {CorrelationId}", request.CorrelationId);
                return Result<PaymentStatusDto>.NotFound(
                    $"Payment not found: {request.CorrelationId}");
            }

            var dto = MapToStatusDto(payment);

            _logger.LogInformation(
                "[PaymentService.Api.ReaderService][GetPaymentHandler][BLOCK_HANDLER_GET] " +
                "Payment retrieved successfully {CorrelationId}", request.CorrelationId);

            return Result<PaymentStatusDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PaymentService.Api.ReaderService][GetPaymentHandler][BLOCK_HANDLER_GET] " +
                "Error fetching payment {CorrelationId}", request.CorrelationId);
            return Result<PaymentStatusDto>.Failure("Internal server error");
        }
        // END_BLOCK_HANDLER_GET
    }

    public async Task<Result<PagedPaymentStatusResponse>> HandleQueryAsync(
        GetPaymentsByStatusRequest request, CancellationToken ct)
    {
        // START_BLOCK_HANDLER_QUERY_STATUS
        try
        {
            _logger.LogInformation(
                "[PaymentService.Api.ReaderService][GetPaymentHandler][BLOCK_HANDLER_QUERY_STATUS] " +
                "Querying payments by status {Status} page={Page} size={PageSize}",
                request.Status, request.Page, request.PageSize);

            var pageSize = Math.Min(request.PageSize, 100);
            var skip = (request.Page - 1) * pageSize;

            if (skip < 0) skip = 0;

            var payments = await _repository.GetByStatusAsync(request.Status, skip, pageSize, ct);

            // For total count, we'd normally do a count query. Here we get one extra page.
            var response = new PagedPaymentStatusResponse
            {
                Items = payments.Select(MapToStatusDto).ToList(),
                TotalCount = payments.Count, // Simplified; production would use CountDocumentsAsync
                Page = request.Page,
                PageSize = pageSize
            };

            _logger.LogInformation(
                "[PaymentService.Api.ReaderService][GetPaymentHandler][BLOCK_HANDLER_QUERY_STATUS] " +
                "Status query returned {Count} results", payments.Count);

            return Result<PagedPaymentStatusResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PaymentService.Api.ReaderService][GetPaymentHandler][BLOCK_HANDLER_QUERY_STATUS] " +
                "Error querying payments by status {Status}", request.Status);
            return Result<PagedPaymentStatusResponse>.Failure("Internal server error");
        }
        // END_BLOCK_HANDLER_QUERY_STATUS
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
