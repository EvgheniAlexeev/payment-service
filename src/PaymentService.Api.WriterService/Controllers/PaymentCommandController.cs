// FILE: src/PaymentService.Api.WriterService/Controllers/PaymentCommandController.cs
// VERSION: 1.0.0

using Microsoft.AspNetCore.Mvc;
using PaymentService.Api.WriterService.Handlers;
using PaymentService.Api.WriterService.Models;

namespace PaymentService.Api.WriterService.Controllers;

/// <summary>
/// BLOCK_WRITER_COMMAND controller — Command API for payment creation.
/// Route: api/payment
/// Uses 202 Accepted pattern for async saga processing.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentCommandController : ControllerBase
{
    private readonly ICreatePaymentHandler _handler;
    private readonly ILogger<PaymentCommandController> _logger;

    public PaymentCommandController(
        ICreatePaymentHandler handler,
        ILogger<PaymentCommandController> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    /// <summary>
    /// Create a new payment. Returns 202 Accepted on success (async saga processing).
    /// Returns 400 on validation failure.
    /// Returns 409 on duplicate correlationId.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreatePaymentResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePayment(
        [FromBody] CreatePaymentRequest request,
        CancellationToken ct)
    {
        // START_BLOCK_WRITER_COMMAND
        _logger.LogInformation(
            "[PaymentService.Api.WriterService][PaymentCommandController][BLOCK_WRITER_COMMAND] " +
            "Creating payment {CorrelationId}", request.CorrelationId);

        var result = await _handler.HandleAsync(request, ct);

        if (!result.IsSuccess)
        {
            var error = result.Error ?? "Unknown error";
            _logger.LogWarning(
                "[PaymentService.Api.WriterService][PaymentCommandController][BLOCK_WRITER_COMMAND] " +
                "Payment creation failed {CorrelationId}: {Error}", request.CorrelationId, error);

            // Check for validation failure vs. internal error
            if (error.Contains("required") || error.Contains("must") || error.Contains("exceed"))
                return BadRequest(new { error });

            return StatusCode(500, new { error });
        }

        _logger.LogInformation(
            "[PaymentService.Api.WriterService][PaymentCommandController][BLOCK_WRITER_COMMAND] " +
            "Payment created successfully {CorrelationId}", request.CorrelationId);

        return Accepted(result.Data);
        // END_BLOCK_WRITER_COMMAND
    }
}
