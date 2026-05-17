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
/// <para><strong>@invariant:</strong> Date range resolved by defaulting rules before query</para>
/// <para><strong>@invariant:</strong> accountId matches SenderAccount OR ReceiverAccount</para>
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
    /// Handle account transaction history query with date range resolution.
    ///
    /// Date range defaults (applied before query)
    ///   - Both null → last 7 days
    ///   - DateFrom null, DateTo set → from first day of DateTo's year
    ///   - DateFrom set, DateTo null → to current UTC date
    ///   - Both set → validate span ≤ days-in-year
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> HandleAsync</para>
    /// <para><strong>@param request:</strong> GetTransactionsRequest with accountId, dates, pagination</para>
    /// <para><strong>@return:</strong> Result with GetTransactionsResponse or error</para>
    /// <para><strong>@throws:</strong> TimeoutException — MongoDB query exceeded timeout</para>
    /// <para><strong>@log-event:</strong> reader.handler.get-transactions-start {accountId} {dateFrom} {dateTo}</para>
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
            // Resolve date range defaults
            var (dateFrom, dateTo) = ResolveDateRange(request.DateFrom, request.DateTo);

            _logger.LogInformation(
                "[PaymentService.Api.ReaderService][Features.GetTransactions][GetTransactionsHandler] " +
                "Fetching transactions for account {AccountId} " +
                "(dateFrom={DateFrom}, dateTo={DateTo}, skip={Skip}, limit={Limit})",
                request.AccountId, dateFrom.ToString("yyyy-MM-dd"),
                dateTo.ToString("yyyy-MM-dd"), request.Skip, request.Limit);

            var (payments, totalCount) = await _repository.GetByAccountAsync(
                request.AccountId, dateFrom, dateTo,
                request.Skip, request.Limit, ct);

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

    /// <summary>
    /// Resolve date range defaults:
    /// - Both null → last 7 days
    /// - DateFrom null, DateTo set → from first day of DateTo's year
    /// - DateFrom set, DateTo null → to current UTC date
    /// </summary>
    internal static (DateTime dateFrom, DateTime dateTo) ResolveDateRange(
        DateTime? dateFrom, DateTime? dateTo)
    {
        var now = DateTime.UtcNow.Date;

        // Both null: last 7 days
        if (dateFrom == null && dateTo == null)
        {
            return (now.AddDays(-6), now);
        }

        // DateTo set, DateFrom null: from first day of DateTo's year
        if (dateFrom == null && dateTo != null)
        {
            var d = dateTo.Value.Date;
            return (new DateTime(d.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc), d);
        }

        // DateFrom set, DateTo null: to current UTC date
        if (dateFrom != null && dateTo == null)
        {
            // Cap dateFrom at current date
            var df = dateFrom.Value.Date > now ? now : dateFrom.Value.Date;
            return (df, now);
        }

        // Both set: use as-is
        return (dateFrom!.Value.Date, dateTo!.Value.Date);
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
