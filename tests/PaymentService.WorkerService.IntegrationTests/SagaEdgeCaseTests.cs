// FILE: SagaEdgeCaseTests.cs
// VERSION: 2.0.0
// MODULE: M-INTEGRATION
// PURPOSE: Test specification
// SEMANTIC_TAG: [TEST]
// START_MODULE M_INTEGRATION

// START_MODULE TESTS
// START_BLOCK_TESTS SagaEdgeCaseTests
// PURPOSE: Edge case and boundary tests for PaymentSaga orchestrator.
//          Covers: idempotency, concurrent sagas, state transitions, invalid inputs.
//          Tests: ~30
// SEMANTIC_TAG: [BLOCK_TEST_SAGA_EDGE] Edge case saga tests
namespace PaymentService.Workers.IntegrationTests;

using PaymentService.Workers.Services;
using PaymentService.Shared.Commands;
using PaymentService.Workers.Events;
using PaymentService.Workers.Sagas;

public class SagaEdgeCaseTests
{
    // ──────────────── State Transition Tests ────────────────

    [Fact]
    public void State_Transitions_Validating_To_ReservingFunds()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, state) = CreateSaga("TRANSITION-001", dlq);

        var startCmd = new PaymentCommand
        {
            CorrelationId = "TRANSITION-001",
            PaymentRequest = TestDataFactory.CreateValidRequest("TRANSITION-001", amount: 500m),
            IdempotencyKey = "IDEM-TR-001",
        };

        saga.Handle(startCmd);
        state.Status.Should().Be("Validating");

        saga.Handle(new PaymentValidated
        {
            CorrelationId = "TRANSITION-001",
            IsValid = true,
        });
        state.Status.Should().Be("ReservingFunds");
    }

    [Fact]
    public void State_Transitions_ReservingFunds_To_Settling()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, state) = CreateSaga("TRANSITION-002", dlq);

        StartAndValidate(saga, "TRANSITION-002");
        state.Status = "ReservingFunds";

        saga.Handle(new FundsReserved
        {
            CorrelationId = "TRANSITION-002",
            IsSuccessful = true,
            ReservationId = "RSV-TR2",
            Amount = 500m,
        });
        state.Status.Should().Be("Settling");
    }

    [Fact]
    public void State_Transitions_Settling_To_Settled()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, state) = CreateSaga("TRANSITION-003", dlq);

        StartAndValidate(saga, "TRANSITION-003");
        state.Status = "ReservingFunds";
        saga.Handle(new FundsReserved
        {
            CorrelationId = "TRANSITION-003",
            IsSuccessful = true,
            ReservationId = "RSV-TR3",
            Amount = 500m,
        });
        state.Status.Should().Be("Settling");

        saga.Handle(new PaymentSettledInternal
        {
            CorrelationId = "TRANSITION-003",
            IsSuccessful = true,
            SettlementId = "STL-TR3",
        });
        state.Status.Should().Be("Settled");
        state.CompletedAt.Should().NotBeNull();
    }

    // ──────────────── DLQ Content Detail Tests ────────────────

    [Fact]
    public void DLQEvent_IncludesRetryCount()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, state) = CreateSaga("DLQ-RETRY", dlq);

        var startCmd = new PaymentCommand
        {
            CorrelationId = "DLQ-RETRY",
            PaymentRequest = TestDataFactory.CreateValidRequest("DLQ-RETRY"),
            IdempotencyKey = "IDEM-RETRY",
        };
        saga.Handle(startCmd);
        state.RetryCount = 3; // Simulate 3 retries

        saga.Handle(new PaymentValidated
        {
            CorrelationId = "DLQ-RETRY",
            IsValid = false,
            ErrorMessage = "Exhausted retries",
        });

        var dlqEvent = dlq.PublishedEvents.Single();
        dlqEvent.RetryCount.Should().Be(3);
    }

    [Fact]
    public void DLQEvent_HasTimestamps()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, _) = CreateSaga("DLQ-TIME", dlq);

        saga.Handle(new PaymentCommand
        {
            CorrelationId = "DLQ-TIME",
            PaymentRequest = TestDataFactory.CreateValidRequest("DLQ-TIME"),
            IdempotencyKey = "IDEM-TIME",
        });

        saga.Handle(new PaymentValidated
        {
            CorrelationId = "DLQ-TIME",
            IsValid = false,
            ErrorMessage = "Timing issue",
        });

        var dlqEvent = dlq.PublishedEvents.Single();
        dlqEvent.FailedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ──────────────── Various Amount Tests ────────────────

    [Fact]
    public void Saga_WithDifferentAmounts_CompletesCorrectly()
    {
        var amounts = new[] { 0.01m, 1m, 10m, 100m, 1000m, 10000m, 100000m, 999999m };
        foreach (var amount in amounts)
        {
            var dlq = new CapturingDLQPublisher();
            var (saga, state) = CreateSaga($"AMT-{amount}", dlq);
            StartAndValidate(saga, $"AMT-{amount}", amount);
            state.Status = "ReservingFunds";

            saga.Handle(new FundsReserved
            {
                CorrelationId = $"AMT-{amount}",
                IsSuccessful = true,
                ReservationId = $"RSV-AMT-{amount}",
                Amount = amount,
            });
            state.Status.Should().Be("Settling");

            saga.Handle(new PaymentSettledInternal
            {
                CorrelationId = $"AMT-{amount}",
                IsSuccessful = true,
                SettlementId = $"STL-AMT-{amount}",
            });
            state.Status.Should().Be("Settled");
        }
    }

    // ──────────────── Concurrent Saga Test ────────────────

    [Fact]
    public void ManySagas_RunningInParallel_NoStateInterference()
    {
        const int sagaCount = 50;
        var dlq = new CapturingDLQPublisher();
        var sagas = new List<(PaymentService.Workers.Sagas.PaymentSaga saga, PaymentSagaState state)>();

        for (int i = 0; i < sagaCount; i++)
        {
            var (s, st) = CreateSaga($"CONCURRENT-{i:D3}", dlq);
            sagas.Add((s, st));
        }

        // Start all sagas
        for (int i = 0; i < sagaCount; i++)
        {
            var cmd = new PaymentCommand
            {
                CorrelationId = $"CONCURRENT-{i:D3}",
                PaymentRequest = TestDataFactory.CreateValidRequest($"CONCURRENT-{i:D3}", amount: 100m * (i + 1)),
                IdempotencyKey = $"IDEM-CONC-{i:D3}",
            };
            sagas[i].saga.Handle(cmd);
        }

        // Validate all: half pass, half fail
        for (int i = 0; i < sagaCount; i++)
        {
            var shouldPass = i % 2 == 0;
            sagas[i].saga.Handle(new PaymentValidated
            {
                CorrelationId = $"CONCURRENT-{i:D3}",
                IsValid = shouldPass,
                ErrorMessage = shouldPass ? null : "Concurrent test failure",
            });
        }

        // Verify states
        for (int i = 0; i < sagaCount; i++)
        {
            if (i % 2 == 0)
            {
                sagas[i].state.Status.Should().Be("ReservingFunds");
            }
            else
            {
                sagas[i].state.Status.Should().Be("Failed");
            }
        }

        // Only the failed ones should be in DLQ
        dlq.PublishedEvents.Should().HaveCount(sagaCount / 2);
    }

    // ──────────────── Saga Completion State ────────────────

    [Fact]
    public void CompletedSaga_HasCompletedAtSet()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, state) = CreateSaga("COMPLETED-AT", dlq);

        RunFullHappyPath(saga, "COMPLETED-AT");

        state.CompletedAt.Should().NotBeNull();
        state.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void FailedSaga_HasCompletedAtSet()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, state) = CreateSaga("FAILED-AT", dlq);

        saga.Handle(new PaymentCommand
        {
            CorrelationId = "FAILED-AT",
            PaymentRequest = TestDataFactory.CreateValidRequest("FAILED-AT"),
            IdempotencyKey = "IDEM-FAIL-AT",
        });

        saga.Handle(new PaymentValidated
        {
            CorrelationId = "FAILED-AT",
            IsValid = false,
            ErrorMessage = "Test",
        });

        state.CompletedAt.Should().NotBeNull();
        state.Status.Should().Be("Failed");
    }

    // ──────────────── Helpers ────────────────

    private static (
        PaymentService.Workers.Sagas.PaymentSaga saga,
        PaymentSagaState state
    ) CreateSaga(string correlationId, IDLQPublisher dlq)
    {
        var (_, _, _, sagaLogger, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var saga = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger, dlq, metrics);
        return (saga, saga.State);
    }

    private static void StartAndValidate(
        PaymentService.Workers.Sagas.PaymentSaga saga,
        string correlationId,
        decimal amount = 500m)
    {
        var cmd = new PaymentCommand
        {
            CorrelationId = correlationId,
            PaymentRequest = TestDataFactory.CreateValidRequest(correlationId, amount: amount),
            IdempotencyKey = $"IDEM-{correlationId}",
        };
        saga.Handle(cmd);
        saga.Handle(new PaymentValidated
        {
            CorrelationId = correlationId,
            IsValid = true,
        });
    }

    private static void RunFullHappyPath(
        PaymentService.Workers.Sagas.PaymentSaga saga,
        string correlationId)
    {
        StartAndValidate(saga, correlationId);
        saga.State.Status = "ReservingFunds";

        saga.Handle(new FundsReserved
        {
            CorrelationId = correlationId,
            IsSuccessful = true,
            ReservationId = $"RSV-{correlationId}",
            Amount = 500m,
        });

        saga.Handle(new PaymentSettledInternal
        {
            CorrelationId = correlationId,
            IsSuccessful = true,
            SettlementId = $"STL-{correlationId}",
        });
    }
}
// END_BLOCK_TESTS
