// FILE: src/PaymentService.Api.WriterService/Controllers/PaymentCommandController.cs
// VERSION: 2.0.0
// MODULE: M-WRITER
// PURPOSE: HTTP controller for payment command endpoints
// SEMANTIC_TAG: [HTTP_CONTROLLER]
// START_MODULE M_WRITER

// FILE: src/PaymentService.Api.WriterService/Controllers/PaymentCommandController.cs
// VERSION: 1.0.0

using Microsoft.AspNetCore.Mvc;
using PaymentService.Api.WriterService.Handlers;

namespace PaymentService.Api.WriterService.Controllers;

/// <summary>
/// BLOCK_WRITER_COMMAND controller — Command API for payment creation.
/// Route: api/payment
/// Uses 202 Accepted pattern for async saga processing.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-PAYMENT-WRITER</para>
/// <para><strong>@purpose:</strong> Provides HTTP command endpoint for payment submission with async saga initiation</para>
/// <para><strong>@module-type:</strong> ENTRY_POINT</para>
/// <para><strong>@depends:</strong> M-PAYMENT-SHARED, M-PAYMENT-PERSIST</para>
/// <para><strong>@domain-concept:</strong> PaymentCommandController</para>
/// <para><strong>@invariant:</strong> Response latency p99 ≤ 2s (202 Accepted returned immediately)</para>
/// <para><strong>@invariant:</strong> All requests validated before persistence</para>
/// <para><strong>@invariant:</strong> Idempotency key prevents duplicate processing</para>
/// <para><strong>@stability:</strong> STABLE</para>
/// <para><strong>@verification-ref:</strong> V-M-WRITER-PAY</para>
/// </remarks>
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
    /// <remarks>
    /// <para><strong>@contract-action:</strong> CreatePayment</para>
    /// <para><strong>@param request:</strong> CreatePaymentRequest with payment details</para>
    /// <para><strong>@return:</strong> CreatePaymentResponse with correlationId (202 Accepted)</para>
    /// <para><strong>@throws:</strong> ValidationException — request validation failed; ConflictException — idempotency key already processed</para>
    /// <para><strong>@log-event:</strong> writer.controller.create-payment-start {correlationId}</para>
    /// <para><strong>@log-event:</strong> writer.controller.create-payment-accepted {correlationId}</para>
    /// <para><strong>@log-event:</strong> writer.controller.create-payment-error {correlationId} {error}</para>
    /// <para><strong>@trace-span:</strong> writer.create-payment</para>
    /// <para><strong>@pre-condition:</strong> request != null</para>
    /// <para><strong>@post-condition:</strong> response.StatusCode == 202</para>
    /// <para><strong>@complexity:</strong> O(1) (direct write)</para>
    /// <para><strong>@idempotent:</strong> YES (via idempotency key)</para>
    /// <para><strong>@pure:</strong> NO (I/O: persistence + event publishing)</para>
    /// </remarks>
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
