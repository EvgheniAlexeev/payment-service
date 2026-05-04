// FILE: src/PaymentService.Api.ReaderService/Controllers/PaymentQueryController.cs
// VERSION: 1.0.0

using Microsoft.AspNetCore.Mvc;
using PaymentService.Api.ReaderService.Handlers;
using PaymentService.Api.ReaderService.Models;
using PaymentService.Shared.Dtos;

namespace PaymentService.Api.ReaderService.Controllers;

/// <summary>
/// BLOCK_READER_QUERY controller — Query-only API for payment status.
/// Route: api/payment
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentQueryController : ControllerBase
{
    private readonly IGetPaymentHandler _handler;
    private readonly ILogger<PaymentQueryController> _logger;

    public PaymentQueryController(IGetPaymentHandler handler, ILogger<PaymentQueryController> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    /// <summary>
    /// Get a single payment by correlation ID.
    /// </summary>
    [HttpGet("{correlationId}")]
    [ProducesResponseType(typeof(PaymentStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPayment(string correlationId, CancellationToken ct)
    {
        // START_BLOCK_READER_QUERY
        _logger.LogInformation(
            "[PaymentService.Api.ReaderService][PaymentQueryController][BLOCK_READER_QUERY] " +
            "Querying payment {CorrelationId}", correlationId);

        var result = await _handler.HandleAsync(
            new GetPaymentRequest { CorrelationId = correlationId }, ct);

        if (result.IsNotFound)
        {
            _logger.LogWarning(
                "[PaymentService.Api.ReaderService][PaymentQueryController][BLOCK_READER_QUERY] " +
                "Payment not found {CorrelationId}", correlationId);
            return NotFound(new { error = result.Error });
        }

        if (!result.IsSuccess)
        {
            _logger.LogError(
                "[PaymentService.Api.ReaderService][PaymentQueryController][BLOCK_READER_QUERY] " +
                "Error querying payment {CorrelationId}: {Error}", correlationId, result.Error);
            return StatusCode(500, new { error = result.Error });
        }

        return Ok(result.Data);
        // END_BLOCK_READER_QUERY
    }

    /// <summary>
    /// Query payments by status with pagination.
    /// </summary>
    [HttpGet("by-status/{status}")]
    [ProducesResponseType(typeof(PagedPaymentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentsByStatus(
        string status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        // START_BLOCK_READER_QUERY_STATUS
        _logger.LogInformation(
            "[PaymentService.Api.ReaderService][PaymentQueryController][BLOCK_READER_QUERY_STATUS] " +
            "Querying payments by status {Status} page={Page}", status, page);

        var request = new GetPaymentsByStatusRequest
        {
            Status = status,
            Page = page,
            PageSize = pageSize
        };

        var result = await _handler.HandleQueryAsync(request, ct);

        if (!result.IsSuccess)
        {
            _logger.LogError(
                "[PaymentService.Api.ReaderService][PaymentQueryController][BLOCK_READER_QUERY_STATUS] " +
                "Error querying payments by status {Status}: {Error}", status, result.Error);
            return StatusCode(500, new { error = result.Error });
        }

        return Ok(result.Data);
        // END_BLOCK_READER_QUERY_STATUS
    }
}
