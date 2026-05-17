// FILE: src/PaymentService.Api.ReaderService/Features/GetTransactions/GetTransactionsHandler.cs
// VERSION: 2.0.0
// MODULE: M-READER
// PURPOSE: Business logic handler for account transaction history query
// SEMANTIC_TAG: [HANDLER, QUERY_PROCESSOR]
// START_MODULE M_READER

using Microsoft.Extensions.Logging;
using PaymentService.Persistence.Repositories;
using PaymentService.Shared;
using PaymentService.Shared.Models;

namespace PaymentService.Api.ReaderService.Features.GetTransactions;

/// <summary>
/// BLOCK_GET_TRANSACTIONS_HANDLER — Account transaction history handler.
/// VSA feature: GetTransactions (ReaderService)
/// </summary>
public class GetTransactionsHandler
{
    private readonly IPaymentDocumentRepository _repository;
    private readonly ILogger<GetTransactionsHandler> _logger;

    public GetTransactionsHandler(
        IPaymentDocumentRepository repository,
        ILogger<GetTransactionsHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<GetTransactionsResponse>> HandleAsync(
        GetTransactionsRequest request, CancellationToken ct)
    {
        // START_BLOCK_GET_TRANSACTIONS_HANDLER
        try
        {
            _logger.LogInformation(
                "[PaymentService.Api.ReaderService][Features.GetTransactions][GetTransactionsHandler] " +
                "Fetching transactions for account {AccountId} (skip={Skip}, limit={Limit})",
                request.AccountId, request.Skip, request.Limit);

            var (payments, totalCount) = await _repository.GetByAccountAsync(
                request.AccountId, request.Skip, request.Limit, ct);

            var transactions = payments.Select(p => MapToTransactionItem(p, request.AccountId)).ToList();

            var response = new GetTransactionsResponse
            {
                Transactions = transactions,
                TotalCount = totalCount,
                AccountId = request.AccountId
            };

            _logger.LogInformation(
                "[PaymentService.Api.ReaderService][Features.GetTransactions][GetTransactionsHandler] " +
                "Returned {Count} transactions for account {AccountId} (total: {Total})",
                transactions.Count, request.AccountId, totalCount);

            return Result<GetTransactionsResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PaymentService.Api.ReaderService][Features.GetTransactions][GetTransactionsHandler] " +
                "Error fetching transactions for account {AccountId}",
                request.AccountId);
            return Result<GetTransactionsResponse>.Failure("Internal server error");
        }
        // END_BLOCK_GET_TRANSACTIONS_HANDLER
    }

    private static TransactionItem MapToTransactionItem(PaymentDocument payment, string accountId)
    {
        var isSender = payment.Request.SenderAccount == accountId;
        var isReceiver = payment.Request.ReceiverAccount == accountId;

        return new TransactionItem
        {
            CorrelationId = payment.CorrelationId,
            CounterpartyAccount = isSender ? payment.Request.ReceiverAccount : payment.Request.SenderAccount,
            Direction = isSender ? "outgoing" : "incoming",
            Amount = payment.Request.Amount,
            Currency = payment.Request.Currency,
            Status = payment.Status,
            CreatedAt = payment.CreatedAt,
            SettledAt = payment.SettledAt
        };
    }
}
