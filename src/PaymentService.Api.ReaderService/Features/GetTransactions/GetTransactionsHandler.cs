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
/// <remarks>
/// <para><strong>@contract:</strong> M-READER</para>
/// <para><strong>@purpose:</strong> Query handler fetching paged payment history by account (sender or receiver)</para>
/// <para><strong>@module-type:</strong> CORE_LOGIC (query handler)</para>
/// <para><strong>@invariant:</strong> Payments sorted by CreatedAt descending</para>
/// <para><strong>@invariant:</strong> accountId matches SenderAccount OR ReceiverAccount (not both)</para>
/// <para><strong>@verification-ref:</strong> V-M-READER</para>
/// </remarks>
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

    /// <summary>
    /// Handle account transaction history query.
    /// Queries MongoDB for payments where accountId is sender or receiver.
    /// Maps results to TransactionItem list with direction metadata.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> HandleAsync</para>
    /// <para><strong>@param request:</strong> GetTransactionsRequest with accountId, skip, limit</para>
    /// <para><strong>@return:</strong> Result with GetTransactionsResponse or error</para>
    /// <para><strong>@throws:</strong> TimeoutException — MongoDB query exceeded timeout</para>
    /// <para><strong>@log-event:</strong> reader.handler.get-transactions-start {accountId} {skip} {limit}</para>
    /// <para><strong>@log-event:</strong> reader.handler.get-transactions-success {accountId} {count} {total}</para>
    /// <para><strong>@log-event:</strong> reader.handler.get-transactions-error {accountId}</para>
    /// <para><strong>@trace-span:</strong> reader.handler.get-transactions</para>
    /// <para><strong>@pre-condition:</strong> request.AccountId non-empty</para>
    /// <para><strong>@post-condition:</strong> response.Transactions != null</para>
    /// <para><strong>@complexity:</strong> O(log n + k) + O(k) mapping</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// <para><strong>@pure:</strong> NO (I/O: database read)</para>
    /// </remarks>
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
