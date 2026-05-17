// FILE: PaymentSagaMetrics.cs
// VERSION: 2.0.0
// MODULE: M-WORKER
// PURPOSE: External service dependency
// SEMANTIC_TAG: [SERVICE_ABSTRACTION, DEPENDENCY]
// START_MODULE M_WORKER

// START_MODULE M-WORKER
// START_BLOCK_METRICS PaymentSagaMetrics
// PURPOSE: Prometheus metrics for PaymentSaga orchestration.
//          Exposes counters, histograms, and gauges for saga state transitions,
//          step durations, amounts, and failure rates.
// SEMANTIC_TAG: [BLOCK_METRICS] Prometheus metrics collection
namespace PaymentService.Workers.Metrics;

/// <summary>
/// Saga orchestrator for distributed transaction processing in the M-WORKER module
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (saga orchestrator, manages distributed transaction lifecycle)</para>
/// <para><strong>@purpose:</strong> Saga orchestrator for distributed transaction processing in the M-WORKER module</para>
/// <para><strong>@invariant:</strong> Saga state transitions are deterministic; idempotency ensures exactly-once processing</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>

using Prometheus;

public class PaymentSagaMetrics
{
    // START_BLOCK_METRICS_INIT
    private readonly Counter _sagaStartedTotal;
    private readonly Counter _sagaCompletedTotal;
    private readonly Counter _sagaFailedTotal;
    private readonly Histogram _sagaDurationSeconds;
    private readonly Counter _stepSuccessTotal;
    private readonly Counter _stepFailureTotal;
    private readonly Histogram _stepDurationSeconds;
    private readonly Histogram _reservationAmount;
    private readonly Histogram _settlementAmount;
    private readonly Gauge _sagasInProgress;

    public PaymentSagaMetrics()
    {
        _sagaStartedTotal = Metrics.CreateCounter(
            "payment_saga_started_total",
            "Total number of payment sagas started.");

        _sagaCompletedTotal = Metrics.CreateCounter(
            "payment_saga_completed_total",
            "Total number of payment sagas completed successfully.");

        _sagaFailedTotal = Metrics.CreateCounter(
            "payment_saga_failed_total",
            "Total number of payment sagas that failed, labeled by failure step.",
            new CounterConfiguration
            {
                LabelNames = new[] { "failed_step" },
            });

        _sagaDurationSeconds = Metrics.CreateHistogram(
            "payment_saga_duration_seconds",
            "Histogram of saga execution duration in seconds.",
            new HistogramConfiguration
            {
                Buckets = new[] { 0.1, 0.5, 1, 2, 5, 10, 30, 60, 120, 300, 600 },
            });

        _stepSuccessTotal = Metrics.CreateCounter(
            "payment_step_success_total",
            "Total number of successful step completions, labeled by step name.",
            new CounterConfiguration
            {
                LabelNames = new[] { "step" },
            });

        _stepFailureTotal = Metrics.CreateCounter(
            "payment_step_failure_total",
            "Total number of failed step executions, labeled by step name.",
            new CounterConfiguration
            {
                LabelNames = new[] { "step" },
            });

        _stepDurationSeconds = Metrics.CreateHistogram(
            "payment_step_duration_seconds",
            "Histogram of step execution duration in seconds, labeled by step name.",
            new HistogramConfiguration
            {
                LabelNames = new[] { "step" },
                Buckets = new[] { 0.01, 0.05, 0.1, 0.5, 1, 2, 5, 10, 30 },
            });

        _reservationAmount = Metrics.CreateHistogram(
            "payment_reservation_amount",
            "Histogram of reserved amounts.",
            new HistogramConfiguration
            {
                Buckets = new[] { 1, 10, 100, 1000, 10000, 100000, 1000000 },
            });

        _settlementAmount = Metrics.CreateHistogram(
            "payment_settlement_amount",
            "Histogram of settled amounts.",
            new HistogramConfiguration
            {
                Buckets = new[] { 1, 10, 100, 1000, 10000, 100000, 1000000 },
            });

        _sagasInProgress = Metrics.CreateGauge(
            "payment_sagas_in_progress",
            "Number of payment sagas currently in progress.");
    }
    // END_BLOCK_METRICS_INIT

    // START_BLOCK_METRICS_METHODS
    /// <summary>Increment saga started counter and in-progress gauge.</summary>
    public void IncrementSagaStarted()
    {
        _sagaStartedTotal.Inc();
        _sagasInProgress.Inc();
    }

    /// <summary>Increment saga completed counter and decrement in-progress gauge.</summary>
    public void IncrementSagaCompleted()
    {
        _sagaCompletedTotal.Inc();
        _sagasInProgress.Dec();
    }

    /// <summary>
    /// Increment saga failed counter (labeled by step) and decrement in-progress gauge.
    /// </summary>
    public void IncrementSagaFailed(string failedStep)
    {
        _sagaFailedTotal.WithLabels(failedStep).Inc();
        _sagasInProgress.Dec();
    }

    /// <summary>Record saga total duration.</summary>
    public void RecordSagaDuration(TimeSpan duration)
    {
        _sagaDurationSeconds.Observe(duration.TotalSeconds);
    }

    /// <summary>Record step execution duration.</summary>
    public void RecordStepDuration(string step, TimeSpan duration)
    {
        _stepDurationSeconds.WithLabels(step).Observe(duration.TotalSeconds);
    }

    /// <summary>Increment per-step success counter.</summary>
    public void IncrementStepSuccess(string step)
    {
        _stepSuccessTotal.WithLabels(step).Inc();
    }

    /// <summary>Increment per-step failure counter.</summary>
    public void IncrementStepFailure(string step)
    {
        _stepFailureTotal.WithLabels(step).Inc();
    }

    /// <summary>Record reservation amount.</summary>
    public void RecordReservationAmount(decimal amount)
    {
        _reservationAmount.Observe((double)amount);
    }

    /// <summary>Record settlement amount.</summary>
    public void RecordSettlementAmount(decimal amount)
    {
        _settlementAmount.Observe((double)amount);
    }
    // END_BLOCK_METRICS_METHODS
}
// END_BLOCK_METRICS
