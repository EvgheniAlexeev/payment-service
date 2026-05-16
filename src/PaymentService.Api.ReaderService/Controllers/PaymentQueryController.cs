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
/// <remarks>
/// <para><strong>@contract:</strong> M-PAYMENT-READER</para>
/// <para><strong>@purpose:</strong> Provides HTTP query endpoints for payment retrieval with fast synchronized reads from MongoDB</para>
/// <para><strong>@module-type:</strong> ENTRY_POINT</para>
/// <para><strong>@depends:</strong> M-PAYMENT-SHARED, M-PAYMENT-PERSIST</para>
/// <para><strong>@domain-concept:</strong> PaymentQueryController</para>
/// <para><strong>@invariant:</strong> Response latency p99 ≤ 100ms</para>
/// <para><strong>@invariant:</strong> All queries validated before database access</para>
/// <para><strong>@stability:</strong> STABLE</para>
/// <para><strong>@verification-ref:</strong> V-M-READER-PAY</para>
/// </remarks>
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
    /// <remarks>
    /// <para><strong>@contract-action:</strong> GetPayment</para>
    /// <para><strong>@param correlationId:</strong> Unique payment identifier</para>
    /// <para><strong>@return:</strong> PaymentStatusDto with payment details (200 OK)</para>
    /// <para><strong>@throws:</strong> NotFoundException — when payment not found; ValidationException — correlationId invalid</para>
    /// <para><strong>@log-event:</strong> reader.controller.get-payment-start {correlationId}</para>
    /// <para><strong>@log-event:</strong> reader.controller.get-payment-success {correlationId}</para>
    /// <para><strong>@log-event:</strong> reader.controller.get-payment-error {correlationId} {error}</para>
    /// <para><strong>@trace-span:</strong> reader.get-payment</para>
    /// <para><strong>@pre-condition:</strong> correlationId != null && correlationId.Length > 0</para>
    /// <para><strong>@post-condition:</strong> result != null</para>
    /// <para><strong>@complexity:</strong> O(1) (indexed query)</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// <para><strong>@pure:</strong> NO (I/O: database read)</para>
    /// </remarks>
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
    /// <remarks>
    /// <para><strong>@contract-action:</strong> GetPaymentsByStatus</para>
    /// <para><strong>@param status:</strong> Payment status filter (e.g., PENDING, COMPLETED, FAILED)</para>
    /// <para><strong>@param page:</strong> Page number (1-indexed)</para>
    /// <para><strong>@param pageSize:</strong> Results per page (1-100)</para>
    /// <para><strong>@return:</strong> PagedPaymentStatusResponse with filtered results (200 OK)</para>
    /// <para><strong>@throws:</strong> ValidationException — status or pagination invalid</para>
    /// <para><strong>@log-event:</strong> reader.controller.get-payments-by-status-start {status} {page}</para>
    /// <para><strong>@log-event:</strong> reader.controller.get-payments-by-status-result {status} {count}</para>
    /// <para><strong>@trace-span:</strong> reader.get-payments-by-status</para>
    /// <para><strong>@pre-condition:</strong> status != null && page > 0 && pageSize > 0</para>
    /// <para><strong>@complexity:</strong> O(log n + k) where k = result set size</para>
    /// <para><strong>@idempotent:</strong> YES</para>
    /// </remarks>
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
