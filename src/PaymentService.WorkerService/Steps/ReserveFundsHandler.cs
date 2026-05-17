// FILE: ReserveFundsHandler.cs
// VERSION: 2.0.0
// MODULE: M-WORKER
// PURPOSE: Saga step handler logic
// SEMANTIC_TAG: [SAGA_HANDLER, STEP]
// START_MODULE M_WORKER

// START_MODULE M-WORKER
// START_BLOCK_HANDLER ReserveFundsHandler
// PURPOSE: Wolverine handler for the ReserveFunds step.
//          Calls ILedgerService.ReserveFundsAsync, publishes FundsReserved event.
//          On exception: publishes isSuccessful=false with error details.
// SEMANTIC_TAG: [BLOCK_HANDLER] Wolverine IHandler
// SEMANTIC_TAG: [BLOCK_RESERVE] Reserving funds {correlationId}
namespace PaymentService.Workers.Steps;

/// <summary>
/// Step handler processing Wolverine commands for the M-WORKER module
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (step handler, processes Wolverine commands)</para>
/// <para><strong>@purpose:</strong> Step handler processing Wolverine commands for the M-WORKER module</para>
/// <para><strong>@invariant:</strong> All operations logged with [BLOCK_*] markers for end-to-end traceability</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>

using Microsoft.Extensions.Logging;
using PaymentService.Workers.Commands;
using PaymentService.Workers.Events;
using PaymentService.Workers.Metrics;
using PaymentService.Workers.Services;

public class ReserveFundsHandler
{
    private readonly ILedgerService _ledgerService;
    private readonly ILogger<ReserveFundsHandler> _logger;
    private readonly PaymentSagaMetrics _metrics;

    public ReserveFundsHandler(
        ILedgerService ledgerService,
        ILogger<ReserveFundsHandler> logger,
        PaymentSagaMetrics metrics)
    {
        _ledgerService = ledgerService;
        _logger = logger;
        _metrics = metrics;
    }

    // START_BLOCK_HANDLER_RESERVE
    /// <summary>
    /// Handle the ReserveFundsCommand — reserve funds via ledger service.
    /// Returns FundsReserved event for saga consumption.
    /// </summary>
    public async Task<FundsReserved> Handle(
        ReserveFundsCommand command,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[PaymentService.Workers][ReserveFundsHandler][BLOCK_HANDLER_RESERVE] " +
            "Reserving funds for {correlationId}, amount={amount}, sender={sender}",
            command.CorrelationId, command.Amount, command.SenderAccount);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var reservationId = await _ledgerService.ReserveFundsAsync(
                command.CorrelationId,
                command.Amount,
                command.SenderAccount,
                ct);

            sw.Stop();
            _metrics.RecordStepDuration("ReserveFunds", sw.Elapsed);

            if (reservationId != null)
            {
                _logger.LogInformation(
                    "[PaymentService.Workers][ReserveFundsHandler][BLOCK_HANDLER_RESERVE_SUCCESS] " +
                    "Funds reserved for {correlationId}, reservationId={reservationId}, duration={durationMs}ms",
                    command.CorrelationId, reservationId, sw.ElapsedMilliseconds);
                _metrics.IncrementStepSuccess("ReserveFunds");
                _metrics.RecordReservationAmount(command.Amount);

                return new FundsReserved
                {
                    CorrelationId = command.CorrelationId,
                    ReservationId = reservationId,
                    Amount = command.Amount,
                    IsSuccessful = true,
                    ReservedAt = DateTime.UtcNow,
                };
            }

            _logger.LogWarning(
                "[PaymentService.Workers][ReserveFundsHandler][BLOCK_HANDLER_RESERVE_FAIL] " +
                "Fund reservation returned null for {correlationId}, duration={durationMs}ms",
                command.CorrelationId, sw.ElapsedMilliseconds);
            _metrics.IncrementStepFailure("ReserveFunds");

            return new FundsReserved
            {
                CorrelationId = command.CorrelationId,
                ReservationId = string.Empty,
                Amount = command.Amount,
                IsSuccessful = false,
                ErrorMessage = "Fund reservation returned null — insufficient funds or account issue",
                ReservedAt = DateTime.UtcNow,
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _metrics.RecordStepDuration("ReserveFunds", sw.Elapsed);
            _metrics.IncrementStepFailure("ReserveFunds");

            _logger.LogError(ex,
                "[PaymentService.Workers][ReserveFundsHandler][BLOCK_HANDLER_RESERVE_ERROR] " +
                "Fund reservation threw exception for {correlationId}, duration={durationMs}ms",
                command.CorrelationId, sw.ElapsedMilliseconds);

            return new FundsReserved
            {
                CorrelationId = command.CorrelationId,
                ReservationId = string.Empty,
                Amount = command.Amount,
                IsSuccessful = false,
                ErrorMessage = $"Reservation exception: {ex.Message}",
                ReservedAt = DateTime.UtcNow,
            };
        }
    }
    // END_BLOCK_HANDLER_RESERVE
}
// END_BLOCK_HANDLER
