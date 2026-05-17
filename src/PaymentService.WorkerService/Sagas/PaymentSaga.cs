// FILE: PaymentSaga.cs
// VERSION: 2.0.0
// MODULE: M-WORKER
// PURPOSE: Distributed transaction saga orchestration
// SEMANTIC_TAG: [SAGA_ORCHESTRATION, STATE_MACHINE]
// START_MODULE M_WORKER

// START_MODULE M-WORKER
// START_BLOCK_SAGA_ORCHESTRATOR PaymentSaga
// PURPOSE: Wolverine saga orchestrator for distributed payment processing.
//          Orchestrates: Validate → Reserve → Settle using step handlers.
//          Failed events published to DLQ for manual operator review (no automatic compensation).
//          Idempotency ensured via CorrelationId saga identity.
// SEMANTIC_TAG: [BLOCK_SAGA] Wolverine Saga<PaymentSagaState>
// SEMANTIC_TAG: [BLOCK_DLQ] Manual DLQ compensation via IDLQPublisher
// SEMANTIC_TAG: [BLOCK_IDEMPOTENCY] Saga deduplicates via CorrelationId
namespace PaymentService.Workers.Sagas;

/// <summary>
/// Saga orchestrator for distributed transaction processing in the M-WORKER module
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (saga orchestrator, manages distributed transaction lifecycle)</para>
/// <para><strong>@purpose:</strong> Saga orchestrator for distributed transaction processing in the M-WORKER module</para>
/// <para><strong>@invariant:</strong> Saga state transitions are deterministic; idempotency ensures exactly-once processing</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>

using Microsoft.Extensions.Logging;
using PaymentService.Shared.Commands;
using PaymentService.Shared.Events;
using PaymentService.Workers.Commands;
using PaymentService.Workers.Events;
using PaymentService.Workers.Metrics;
using PaymentService.Workers.Services;

public class PaymentSaga : Wolverine.Saga
{
    private readonly ILogger<PaymentSaga> _logger;
    private readonly IDLQPublisher _dlqPublisher;
    private readonly PaymentSagaMetrics _metrics;

    public PaymentSagaState State { get; set; } = new();

    public PaymentSaga(
        ILogger<PaymentSaga> logger,
        IDLQPublisher dlqPublisher,
        PaymentSagaMetrics metrics)
    {
        _logger = logger;
        _dlqPublisher = dlqPublisher;
        _metrics = metrics;
    }

    // ──────────────── START: Handle PaymentCommand ────────────────
    // START_BLOCK_SAGA_PAYMENT_START
    /// <summary>
    /// Saga entry point — receives PaymentCommand and kicks off validation.
    /// Idempotent: if saga already exists for this CorrelationId, Wolverine loads existing state.
    /// </summary>
    public ValidatePaymentCommand Handle(PaymentCommand command)
    {
        _logger.LogInformation(
            "[PaymentService.Workers][PaymentSaga][BLOCK_SAGA_START] " +
            "Starting payment saga for {correlationId}, amount={amount}, currency={currency}",
            command.CorrelationId, command.Request.Amount, command.Request.Currency);

        _metrics.IncrementSagaStarted();

        State.Id = command.CorrelationId;
        State.CorrelationId = command.CorrelationId;
        State.PaymentRequest = command.Request;
        State.Status = "Validating";
        State.CreatedAt = DateTime.UtcNow;

        return new ValidatePaymentCommand
        {
            CorrelationId = command.CorrelationId,
            PaymentRequest = command.Request,
            CreatedAt = DateTime.UtcNow,
        };
    }
    // END_BLOCK_SAGA_PAYMENT_START

    // ──────────────── Handle: PaymentValidated ────────────────
    // START_BLOCK_SAGA_PAYMENT_VALIDATE_RESPONSE
    /// <summary>
    /// Handles the validation result. If invalid → DLQ. If valid → ReserveFunds.
    /// </summary>
    public object? Handle(PaymentValidated @event)
    {
        _logger.LogInformation(
            "[PaymentService.Workers][PaymentSaga][BLOCK_SAGA_VALIDATE_RESPONSE] " +
            "Validation result for {correlationId}: isValid={isValid}",
            @event.CorrelationId, @event.IsValid);

        if (!@event.IsValid)
        {
            return TransitionToFailed(
                "Validate",
                @event.ErrorMessage ?? "Validation failed",
                "VALIDATION_FAILED");
        }

        State.Status = "ReservingFunds";

        _logger.LogInformation(
            "[PaymentService.Workers][PaymentSaga][BLOCK_SAGA_RESERVE] " +
            "Dispatching ReserveFunds for {correlationId}, amount={amount}",
            State.CorrelationId, State.PaymentRequest?.Amount);

        return new ReserveFundsCommand
        {
            CorrelationId = State.CorrelationId,
            Amount = State.PaymentRequest!.Amount,
            SenderAccount = State.PaymentRequest.SenderAccount,
            CreatedAt = DateTime.UtcNow,
        };
    }
    // END_BLOCK_SAGA_PAYMENT_VALIDATE_RESPONSE

    // ──────────────── Handle: FundsReserved ────────────────
    // START_BLOCK_SAGA_FUNDS_RESERVED_RESPONSE
    /// <summary>
    /// Handles the fund reservation result. If failed → DLQ. If reserved → Settle.
    /// </summary>
    public object? Handle(FundsReserved @event)
    {
        _logger.LogInformation(
            "[PaymentService.Workers][PaymentSaga][BLOCK_SAGA_FUNDS_RESERVED] " +
            "Funds reservation result for {correlationId}: isSuccessful={isSuccessful}, reservationId={reservationId}",
            @event.CorrelationId, @event.IsSuccessful, @event.ReservationId);

        if (!@event.IsSuccessful)
        {
            return TransitionToFailed(
                "ReserveFunds",
                @event.ErrorMessage ?? "Fund reservation failed",
                "RESERVATION_FAILED");
        }

        State.ReservationId = @event.ReservationId;
        State.Status = "Settling";

        _logger.LogInformation(
            "[PaymentService.Workers][PaymentSaga][BLOCK_SAGA_SETTLE] " +
            "Dispatching SettlePayment for {correlationId}, reservationId={reservationId}",
            State.CorrelationId, State.ReservationId);

        return new SettlePaymentCommand
        {
            CorrelationId = State.CorrelationId,
            ReservationId = State.ReservationId,
            Amount = State.PaymentRequest!.Amount,
            ReceiverAccount = State.PaymentRequest.ReceiverAccount,
            CreatedAt = DateTime.UtcNow,
        };
    }
    // END_BLOCK_SAGA_FUNDS_RESERVED_RESPONSE

    // ──────────────── Handle: PaymentSettledInternal ────────────────
    // START_BLOCK_SAGA_SETTLE_RESPONSE
    /// <summary>
    /// Handles settlement result. If failed → DLQ. If settled → saga complete.
    /// </summary>
    public object? Handle(PaymentSettledInternal @event)
    {
        _logger.LogInformation(
            "[PaymentService.Workers][PaymentSaga][BLOCK_SAGA_SETTLE_RESPONSE] " +
            "Settlement result for {correlationId}: isSuccessful={isSuccessful}",
            @event.CorrelationId, @event.IsSuccessful);

        if (!@event.IsSuccessful)
        {
            return TransitionToFailed(
                "Settle",
                @event.ErrorMessage ?? "Settlement failed",
                "SETTLEMENT_FAILED");
        }

        State.Status = "Settled";
        State.CompletedAt = DateTime.UtcNow;

        _metrics.IncrementSagaCompleted();
        _metrics.RecordSagaDuration(State.CompletedAt.Value - State.CreatedAt);

        _logger.LogInformation(
            "[PaymentService.Workers][PaymentSaga][BLOCK_SAGA_COMPLETE] " +
            "Payment saga completed successfully for {correlationId}, settlementId={settlementId}, " +
            "duration={durationMs}ms",
            @event.CorrelationId, @event.SettlementId,
            (State.CompletedAt.Value - State.CreatedAt).TotalMilliseconds);

        // Publish external notification event
        var settled = new PaymentSettled
        {
            CorrelationId = State.CorrelationId,
            SettlementId = @event.SettlementId,
            SettledAt = @event.SettledAt,
            Status = "Settled",
        };

        MarkCompleted();
        return settled;
    }
    // END_BLOCK_SAGA_SETTLE_RESPONSE

    // ──────────────── Private Helpers ────────────────
    // START_BLOCK_SAGA_FAIL_TRANSITION
    private PaymentFailed TransitionToFailed(string step, string errorMessage, string errorCode)
    {
        State.Status = "Failed";
        State.ErrorReason = errorMessage;
        State.ErrorCode = errorCode;
        State.CompletedAt = DateTime.UtcNow;

        _metrics.IncrementSagaFailed(step);
        _metrics.RecordSagaDuration(State.CompletedAt.Value - State.CreatedAt);

        _logger.LogWarning(
            "[PaymentService.Workers][PaymentSaga][BLOCK_SAGA_FAIL] " +
            "Payment saga failed for {correlationId} at step={step}, code={errorCode}, reason={errorReason}, " +
            "duration={durationMs}ms",
            State.CorrelationId, step, errorCode, errorMessage,
            (State.CompletedAt.Value - State.CreatedAt).TotalMilliseconds);

        var failedEvent = new PaymentFailed
        {
            CorrelationId = State.CorrelationId,
            OriginalRequest = State.PaymentRequest!,
            FailedStep = step,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode,
            RetryCount = State.RetryCount,
            FailedAt = DateTime.UtcNow,
        };

        // Publish to DLQ asynchronously
        _ = _dlqPublisher.PublishFailedPaymentAsync(failedEvent);

        MarkCompleted();
        return failedEvent;
    }
    // END_BLOCK_SAGA_FAIL_TRANSITION
}
// END_BLOCK_SAGA_ORCHESTRATOR
