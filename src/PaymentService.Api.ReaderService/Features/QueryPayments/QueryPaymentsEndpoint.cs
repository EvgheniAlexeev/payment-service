// FILE: src/PaymentService.Api.ReaderService/Features/QueryPayments/QueryPaymentsEndpoint.cs
// VERSION: 2.0.0
// MODULE: M-READER
// PURPOSE: Minimal API endpoint definition
// SEMANTIC_TAG: [ENDPOINT, ROUTE_DEFINITION]
// START_MODULE M_READER

// FILE: src/PaymentService.Api.ReaderService/Features/QueryPayments/QueryPaymentsEndpoint.cs
// VERSION: 1.0.0

using Microsoft.AspNetCore.Mvc;
using PaymentService.Shared.Dtos;

namespace PaymentService.Api.ReaderService.Features.QueryPayments;

/// <summary>
/// BLOCK_QUERY_PAYMENTS_ENDPOINT — Query payments by status with pagination.
/// VSA feature: QueryPayments (ReaderService)
/// </summary>
[ApiController]
[Route("api/payment")]
public class QueryPaymentsEndpoint : ControllerBase
{
    private readonly QueryPaymentsHandler _handler;
    private readonly ILogger<QueryPaymentsEndpoint> _logger;

    public QueryPaymentsEndpoint(QueryPaymentsHandler handler, ILogger<QueryPaymentsEndpoint> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    /// <summary>
    /// Query payments by status with pagination.
    /// </summary>
    [HttpGet("by-status/{status}")]
    [ProducesResponseType(typeof(PagedPaymentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Query(
        string status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        // START_BLOCK_QUERY_PAYMENTS_ENDPOINT
        _logger.LogInformation(
            "[PaymentService.Api.ReaderService][Features.QueryPayments][QueryPaymentsEndpoint] " +
            "Querying payments by status {Status} page={Page}", status, page);

        var request = new QueryPaymentsRequest
        {
            Status = status,
            Page = page,
            PageSize = pageSize
        };

        var result = await _handler.HandleAsync(request, ct);

        if (!result.IsSuccess)
        {
            _logger.LogError(
                "[PaymentService.Api.ReaderService][Features.QueryPayments][QueryPaymentsEndpoint] " +
                "Error querying payments by status {Status}: {Error}", status, result.Error);
            return StatusCode(500, new { error = result.Error });
        }

        return Ok(result.Data);
        // END_BLOCK_QUERY_PAYMENTS_ENDPOINT
    }
}
