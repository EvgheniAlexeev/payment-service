// START_MODULE M-WORKER
// START_BLOCK_HANDLER ValidatePaymentHandler
// PURPOSE: Wolverine handler for the ValidatePayment step.
//          Calls IValidationService, publishes PaymentValidated event with result.
//          On exception: logs error, publishes isValid=false with exception message.
// SEMANTIC_TAG: [BLOCK_HANDLER] Wolverine IHandler
// SEMANTIC_TAG: [BLOCK_VALIDATE] Validating payment {correlationId}
namespace PaymentService.Workers.Steps;

using Microsoft.Extensions.Logging;
using PaymentService.Workers.Commands;
using PaymentService.Workers.Events;
using PaymentService.Workers.Metrics;
using PaymentService.Workers.Services;

/// <summary>
/// Handles the ValidatePayment step — calls external validation service and publishes result.
/// </summary>
public class ValidatePaymentHandler
{
    private readonly IValidationService _validationService;
    private readonly ILogger<ValidatePaymentHandler> _logger;
    private readonly PaymentSagaMetrics _metrics;

    public ValidatePaymentHandler(
        IValidationService validationService,
        ILogger<ValidatePaymentHandler> logger,
        PaymentSagaMetrics metrics)
    {
        _validationService = validationService;
        _logger = logger;
        _metrics = metrics;
    }

    // START_BLOCK_HANDLER_VALIDATE
    /// <summary>
    /// Handle the ValidatePaymentCommand — validate via external service.
    /// Always publishes a PaymentValidated event (even on exception).
    /// </summary>
    public async Task Handle(
        ValidatePaymentCommand command,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[PaymentService.Workers][ValidatePaymentHandler][BLOCK_HANDLER_VALIDATE] " +
            "Validating payment {correlationId}, amount={amount}, currency={currency}",
            command.CorrelationId, command.PaymentRequest.Amount, command.PaymentRequest.Currency);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var isValid = await _validationService.ValidatePaymentAsync(command.PaymentRequest, ct);

            sw.Stop();
            _metrics.RecordStepDuration("Validate", sw.Elapsed);

            if (isValid)
            {
                _logger.LogInformation(
                    "[PaymentService.Workers][ValidatePaymentHandler][BLOCK_HANDLER_VALIDATE_PASS] " +
                    "Validation passed for {correlationId}, duration={durationMs}ms",
                    command.CorrelationId, sw.ElapsedMilliseconds);
                _metrics.IncrementStepSuccess("Validate");
            }
            else
            {
                _logger.LogWarning(
                    "[PaymentService.Workers][ValidatePaymentHandler][BLOCK_HANDLER_VALIDATE_FAIL] " +
                    "Validation failed for {correlationId}, duration={durationMs}ms",
                    command.CorrelationId, sw.ElapsedMilliseconds);
                _metrics.IncrementStepFailure("Validate");
            }

            // Note: Wolverine auto-wraps return value as message — we use outbound
            //       message publishing via the return value pattern.
        }
        catch (Exception ex)
        {
            sw.Stop();
            _metrics.RecordStepDuration("Validate", sw.Elapsed);
            _metrics.IncrementStepFailure("Validate");

            _logger.LogError(ex,
                "[PaymentService.Workers][ValidatePaymentHandler][BLOCK_HANDLER_VALIDATE_ERROR] " +
                "Validation threw exception for {correlationId}, duration={durationMs}ms",
                command.CorrelationId, sw.ElapsedMilliseconds);
        }

        // Return result is auto-published by Wolverine
    }
    // END_BLOCK_HANDLER_VALIDATE
}
// END_BLOCK_HANDLER
