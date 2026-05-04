// FILE: src/PaymentService.Api.ReaderService/Features/GetPayment/GetPaymentEndpoint.cs
// VERSION: 1.0.0

using Microsoft.AspNetCore.Mvc;
using PaymentService.Shared.Dtos;

namespace PaymentService.Api.ReaderService.Features.GetPayment;

/// <summary>
/// BLOCK_GET_PAYMENT_ENDPOINT — Get single payment by correlation ID.
/// VSA feature: GetPayment (ReaderService)
/// </summary>
[ApiController]
[Route("api/payment")]
public class GetPaymentEndpoint : ControllerBase
{
    private readonly GetPaymentHandler _handler;
    private readonly ILogger<GetPaymentEndpoint> _logger;

    public GetPaymentEndpoint(GetPaymentHandler handler, ILogger<GetPaymentEndpoint> logger)
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
    public async Task<IActionResult> Get(string correlationId, CancellationToken ct)
    {
        // START_BLOCK_GET_PAYMENT_ENDPOINT
        _logger.LogInformation(
            "[PaymentService.Api.ReaderService][Features.GetPayment][GetPaymentEndpoint] " +
            "Querying payment {CorrelationId}", correlationId);

        var result = await _handler.HandleAsync(correlationId, ct);

        if (result.IsNotFound)
        {
            _logger.LogWarning(
                "[PaymentService.Api.ReaderService][Features.GetPayment][GetPaymentEndpoint] " +
                "Payment not found {CorrelationId}", correlationId);
            return NotFound(new { error = result.Error });
        }

        if (!result.IsSuccess)
        {
            _logger.LogError(
                "[PaymentService.Api.ReaderService][Features.GetPayment][GetPaymentEndpoint] " +
                "Error querying payment {CorrelationId}: {Error}", correlationId, result.Error);
            return StatusCode(500, new { error = result.Error });
        }

        return Ok(result.Data);
        // END_BLOCK_GET_PAYMENT_ENDPOINT
    }
}
