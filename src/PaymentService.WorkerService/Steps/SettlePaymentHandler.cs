// START_MODULE M-WORKER
// START_BLOCK_HANDLER SettlePaymentHandler
// PURPOSE: Wolverine handler for the SettlePayment step.
//          Calls ILedgerService.SettleFundsAsync, publishes PaymentSettledInternal event.
//          On exception: publishes isSuccessful=false with error details.
// SEMANTIC_TAG: [BLOCK_HANDLER] Wolverine IHandler
// SEMANTIC_TAG: [BLOCK_SETTLE] Settling payment {correlationId}
namespace PaymentService.Workers.Steps;

using Microsoft.Extensions.Logging;
using PaymentService.Workers.Commands;
using PaymentService.Workers.Events;
using PaymentService.Workers.Metrics;
using PaymentService.Workers.Services;

/// <summary>
/// Handles the SettlePayment step — finalizes payment via the ledger/settlement service.
/// </summary>
public class SettlePaymentHandler
{
    private readonly ILedgerService _ledgerService;
    private readonly ILogger<SettlePaymentHandler> _logger;
    private readonly PaymentSagaMetrics _metrics;

    public SettlePaymentHandler(
        ILedgerService ledgerService,
        ILogger<SettlePaymentHandler> logger,
        PaymentSagaMetrics metrics)
    {
        _ledgerService = ledgerService;
        _logger = logger;
        _metrics = metrics;
    }

    // START_BLOCK_HANDLER_SETTLE
    /// <summary>
    /// Handle the SettlePaymentCommand — settle funds via ledger service.
    /// Returns PaymentSettledInternal event for saga consumption.
    /// </summary>
    public async Task<PaymentSettledInternal> Handle(
        SettlePaymentCommand command,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[PaymentService.Workers][SettlePaymentHandler][BLOCK_HANDLER_SETTLE] " +
            "Settling payment for {correlationId}, reservationId={reservationId}, amount={amount}, receiver={receiver}",
            command.CorrelationId, command.ReservationId, command.Amount, command.ReceiverAccount);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var settled = await _ledgerService.SettleFundsAsync(
                command.CorrelationId,
                command.ReservationId,
                command.Amount,
                command.ReceiverAccount,
                ct);

            sw.Stop();
            _metrics.RecordStepDuration("Settle", sw.Elapsed);

            if (settled)
            {
                var settlementId = $"STL-{command.CorrelationId}-{DateTime.UtcNow:yyyyMMddHHmmss}";

                _logger.LogInformation(
                    "[PaymentService.Workers][SettlePaymentHandler][BLOCK_HANDLER_SETTLE_SUCCESS] " +
                    "Payment settled for {correlationId}, settlementId={settlementId}, duration={durationMs}ms",
                    command.CorrelationId, settlementId, sw.ElapsedMilliseconds);
                _metrics.IncrementStepSuccess("Settle");
                _metrics.RecordSettlementAmount(command.Amount);

                return new PaymentSettledInternal
                {
                    CorrelationId = command.CorrelationId,
                    SettlementId = settlementId,
                    IsSuccessful = true,
                    SettledAt = DateTime.UtcNow,
                };
            }

            _logger.LogWarning(
                "[PaymentService.Workers][SettlePaymentHandler][BLOCK_HANDLER_SETTLE_FAIL] " +
                "Settlement returned false for {correlationId}, duration={durationMs}ms",
                command.CorrelationId, sw.ElapsedMilliseconds);
            _metrics.IncrementStepFailure("Settle");

            return new PaymentSettledInternal
            {
                CorrelationId = command.CorrelationId,
                SettlementId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = "Settlement returned false — may be a timing or counterparty issue",
                SettledAt = DateTime.UtcNow,
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _metrics.RecordStepDuration("Settle", sw.Elapsed);
            _metrics.IncrementStepFailure("Settle");

            _logger.LogError(ex,
                "[PaymentService.Workers][SettlePaymentHandler][BLOCK_HANDLER_SETTLE_ERROR] " +
                "Settlement threw exception for {correlationId}, duration={durationMs}ms",
                command.CorrelationId, sw.ElapsedMilliseconds);

            return new PaymentSettledInternal
            {
                CorrelationId = command.CorrelationId,
                SettlementId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = $"Settlement exception: {ex.Message}",
                SettledAt = DateTime.UtcNow,
            };
        }
    }
    // END_BLOCK_HANDLER_SETTLE
}
// END_BLOCK_HANDLER
