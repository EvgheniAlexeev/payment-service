// FILE: src/PaymentService.Api.ReaderService/Features/GetTransactions/GetTransactionsEndpoint.cs
// VERSION: 2.0.0
// MODULE: M-READER
// PURPOSE: Minimal API endpoint for account transaction history
// SEMANTIC_TAG: [ENDPOINT, ROUTE_DEFINITION]
// START_MODULE M_READER

using Microsoft.AspNetCore.Mvc;
using PaymentService.Api.ReaderService.Features.GetPayment;

namespace PaymentService.Api.ReaderService.Features.GetTransactions;

/// <summary>
/// BLOCK_GET_TRANSACTIONS_ENDPOINT — Get transaction history for an account.
/// VSA feature: GetTransactions (ReaderService)
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-READER</para>
/// <para><strong>@purpose:</strong> HTTP endpoint returning paged transaction history for a given account</para>
/// <para><strong>@module-type:</strong> ENTRY_POINT (API endpoint)</para>
/// <para><strong>@invariant:</strong> accountId must be non-empty, max 64 chars</para>
/// <para><strong>@invariant:</strong> skip ≥ 0, limit between 1 and 100</para>
/// <para><strong>@verification-ref:</strong> V-M-READER</para>
/// </remarks>
[ApiController]
[Route("api/accounts")]
public class GetTransactionsEndpoint : ControllerBase
{
    private readonly GetTransactionsHandler _handler;
    private readonly GetTransactionsValidator _validator;
    private readonly ILogger<GetTransactionsEndpoint> _logger;

    public GetTransactionsEndpoint(
        GetTransactionsHandler handler,
        GetTransactionsValidator validator,
        ILogger<GetTransactionsEndpoint> logger)
    {
        _handler = handler;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Get transaction history for an account.
    /// Returns payments where the account appears as sender or receiver.
    /// </summary>
    /// <remarks>
    /// <para><strong>@contract-action:</strong> GetTransactions</para>
    /// <para><strong>@param accountId:</strong> Account to query history for</para>
    /// <para><strong>@param skip:</strong> Pagination offset (0-based)</para>
    /// <para><strong>@param limit:</strong> Max results (1-100, default 20)</para>
    /// <para><strong>@return:</strong> 200 OK + GetTransactionsResponse or 400 on validation error</para>
    /// <para><strong>@throws:</strong> ValidationException — invalid accountId/skip/limit</para>
    /// <para><strong>@log-event:</strong> reader.get-transactions-start {accountId} {skip} {limit}</para>
    /// <para><strong>@log-event:</strong> reader.get-transactions-success {accountId} {count}</para>
    /// <para><strong>@log-event:</strong> reader.get-transactions-validation-error {accountId} {errors}</para>
    /// <para><strong>@log-event:</strong> reader.get-transactions-error {accountId} {error}</para>
    /// <para><strong>@trace-span:</strong> reader.get-transactions</para>
    /// <para><strong>@pre-condition:</strong> accountId non-empty && skip ≥ 0 && 1 ≤ limit ≤ 100</para>
    /// <para><strong>@post-condition:</strong> result != null (empty array if no transactions)</para>
    /// <para><strong>@complexity:</strong> O(log n + k) where k = result set size</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// <para><strong>@pure:</strong> NO (I/O: database read)</para>
    /// </remarks>
    [HttpGet("{accountId}/transactions")]
    [ProducesResponseType(typeof(GetTransactionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTransactions(
        string accountId,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        // START_BLOCK_GET_TRANSACTIONS_ENDPOINT
        _logger.LogInformation(
            "[PaymentService.Api.ReaderService][Features.GetTransactions][GetTransactionsEndpoint] " +
            "Querying transactions for account {AccountId} (skip={Skip}, limit={Limit})",
            accountId, skip, limit);

        var request = new GetTransactionsRequest
        {
            AccountId = accountId,
            Skip = skip,
            Limit = limit
        };

        var validationResult = await _validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "[PaymentService.Api.ReaderService][Features.GetTransactions][GetTransactionsEndpoint] " +
                "Validation failed for account {AccountId}: {Errors}",
                accountId, string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));

            return BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)),
                Status = StatusCodes.Status400BadRequest
            });
        }

        var result = await _handler.HandleAsync(request, ct);

        if (!result.IsSuccess)
        {
            _logger.LogError(
                "[PaymentService.Api.ReaderService][Features.GetTransactions][GetTransactionsEndpoint] " +
                "Error querying transactions for account {AccountId}: {Error}",
                accountId, result.Error);
            return StatusCode(500, new { error = result.Error });
        }

        _logger.LogInformation(
            "[PaymentService.Api.ReaderService][Features.GetTransactions][GetTransactionsEndpoint] " +
            "Returned {Count} transactions for account {AccountId}",
            result.Data!.Transactions.Count, accountId);

        return Ok(result.Data);
        // END_BLOCK_GET_TRANSACTIONS_ENDPOINT
    }
}
