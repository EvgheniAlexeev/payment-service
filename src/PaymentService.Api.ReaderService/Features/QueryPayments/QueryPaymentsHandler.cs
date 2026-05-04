// FILE: src/PaymentService.Api.ReaderService/Features/QueryPayments/QueryPaymentsHandler.cs
// VERSION: 1.0.0

using Microsoft.Extensions.Logging;
using PaymentService.Persistence.Repositories;
using PaymentService.Shared;
using PaymentService.Shared.Dtos;

namespace PaymentService.Api.ReaderService.Features.QueryPayments;

/// <summary>
/// BLOCK_QUERY_PAYMENTS_HANDLER — Handler for querying payments by status.
/// VSA feature: QueryPayments (ReaderService)
/// </summary>
public class QueryPaymentsHandler
{
    private readonly IPaymentDocumentRepository _repository;
    private readonly ILogger<QueryPaymentsHandler> _logger;

    public QueryPaymentsHandler(
        IPaymentDocumentRepository repository,
        ILogger<QueryPaymentsHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<PagedPaymentStatusResponse>> HandleAsync(
        QueryPaymentsRequest request, CancellationToken ct)
    {
        // START_BLOCK_QUERY_PAYMENTS_HANDLER
        try
        {
            _logger.LogInformation(
                "[PaymentService.Api.ReaderService][Features.QueryPayments][QueryPaymentsHandler] " +
                "Querying payments by status {Status} page={Page} size={PageSize}",
                request.Status, request.Page, request.PageSize);

            var pageSize = Math.Min(request.PageSize, 100);
            var skip = (request.Page - 1) * pageSize;

            if (skip < 0) skip = 0;

            var payments = await _repository.GetByStatusAsync(request.Status, skip, pageSize, ct);

            var response = new PagedPaymentStatusResponse
            {
                Items = payments.Select(MapToStatusDto).ToList(),
                TotalCount = payments.Count,
                Page = request.Page,
                PageSize = pageSize
            };

            _logger.LogInformation(
                "[PaymentService.Api.ReaderService][Features.QueryPayments][QueryPaymentsHandler] " +
                "Status query returned {Count} results", payments.Count);

            return Result<PagedPaymentStatusResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PaymentService.Api.ReaderService][Features.QueryPayments][QueryPaymentsHandler] " +
                "Error querying payments by status {Status}", request.Status);
            return Result<PagedPaymentStatusResponse>.Failure("Internal server error");
        }
        // END_BLOCK_QUERY_PAYMENTS_HANDLER
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
