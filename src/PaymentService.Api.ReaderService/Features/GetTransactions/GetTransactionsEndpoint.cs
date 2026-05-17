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
