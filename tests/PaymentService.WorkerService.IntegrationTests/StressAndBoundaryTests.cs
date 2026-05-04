// START_MODULE TESTS
// START_BLOCK_TESTS StressAndBoundaryTests
// PURPOSE: Stress tests, boundary value tests, and high-volume saga tests.
//          Validates system behavior under load and edge conditions.
//          Tests: ~45
// SEMANTIC_TAG: [BLOCK_TEST_STRESS] Stress and boundary tests
namespace PaymentService.Workers.IntegrationTests;

using PaymentService.Shared.Commands;
using PaymentService.Shared.Dtos;
using PaymentService.Workers.Commands;
using PaymentService.Workers.Events;
using PaymentService.Workers.Sagas;

public class StressAndBoundaryTests
{
    // ──── Bulk Saga Tests ────

    [Fact]
    public void BulkSagas_100Sagas_AllComplete()
    {
        const int count = 100;
        var dlq = new CapturingDLQPublisher();
        var (_, _, _, sagaLogger, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();

        for (int i = 0; i < count; i++)
        {
            var saga = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger, dlq, metrics);
            var cid = $"BULK-{i:D4}";
            var request = TestDataFactory.CreateValidRequest(cid, amount: 10m * (i + 1));

            saga.Handle(new PaymentCommand { CorrelationId = cid, Request = request, IdempotencyKey = $"ID-{cid}" });
            saga.Handle(new PaymentValidated { CorrelationId = cid, IsValid = true });
            saga.State.Status = "ReservingFunds";
            saga.Handle(new FundsReserved { CorrelationId = cid, IsSuccessful = true, ReservationId = $"RSV-{cid}", Amount = request.Amount });
            saga.Handle(new PaymentSettledInternal { CorrelationId = cid, IsSuccessful = true, SettlementId = $"STL-{cid}" });

            saga.State.Status.Should().Be("Settled");
        }
    }

    [Fact]
    public void BulkSagas_50FailAtValidation_50Complete()
    {
        const int count = 50;
        var dlq = new CapturingDLQPublisher();
        var (_, _, _, sagaLogger, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var completedIds = new List<string>();
        var failedIds = new List<string>();

        for (int i = 0; i < count; i++)
        {
            var saga = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger, dlq, metrics);
            var cid = $"FAILHALF-{i:D4}";
            var shouldPass = i % 2 == 0;
            var request = TestDataFactory.CreateValidRequest(cid, amount: 10m * (i + 1));

            saga.Handle(new PaymentCommand { CorrelationId = cid, Request = request, IdempotencyKey = $"ID-{cid}" });
            saga.Handle(new PaymentValidated { CorrelationId = cid, IsValid = shouldPass, ErrorMessage = shouldPass ? null : "Bulk test failure" });

            if (shouldPass)
            {
                saga.State.Status = "ReservingFunds";
                saga.Handle(new FundsReserved { CorrelationId = cid, IsSuccessful = true, ReservationId = $"RSV-{cid}", Amount = request.Amount });
                saga.Handle(new PaymentSettledInternal { CorrelationId = cid, IsSuccessful = true, SettlementId = $"STL-{cid}" });
                saga.State.Status.Should().Be("Settled");
                completedIds.Add(cid);
            }
            else
            {
                saga.State.Status.Should().Be("Failed");
                failedIds.Add(cid);
            }
        }

        completedIds.Should().HaveCount(25);
        failedIds.Should().HaveCount(25);
        dlq.PublishedEvents.Should().HaveCount(25);
    }

    [Fact]
    public void BulkSagas_FailAtEachStep_25Each()
    {
        const int perStep = 25;
        var dlq = new CapturingDLQPublisher();
        var (_, _, _, sagaLogger, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();

        var validateFails = new List<string>();
        var reserveFails = new List<string>();
        var settleFails = new List<string>();
        var successes = new List<string>();

        // Validate failures (25)
        for (int i = 0; i < perStep; i++)
        {
            var saga = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger, dlq, metrics);
            var cid = $"VF-{i:D4}";
            saga.Handle(new PaymentCommand { CorrelationId = cid, Request = TestDataFactory.CreateValidRequest(cid, amount: 10m), IdempotencyKey = $"ID-{cid}" });
            saga.Handle(new PaymentValidated { CorrelationId = cid, IsValid = false, ErrorMessage = "Validate bulk fail" });
            saga.State.Status.Should().Be("Failed");
            validateFails.Add(cid);
        }

        // Reserve failures (25)
        for (int i = 0; i < perStep; i++)
        {
            var saga = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger, dlq, metrics);
            var cid = $"RF-{i:D4}";
            saga.Handle(new PaymentCommand { CorrelationId = cid, Request = TestDataFactory.CreateValidRequest(cid, amount: 10m), IdempotencyKey = $"ID-{cid}" });
            saga.Handle(new PaymentValidated { CorrelationId = cid, IsValid = true });
            saga.State.Status = "ReservingFunds";
            saga.Handle(new FundsReserved { CorrelationId = cid, IsSuccessful = false, ErrorMessage = "Reserve bulk fail" });
            saga.State.Status.Should().Be("Failed");
            reserveFails.Add(cid);
        }

        // Settle failures (25)
        for (int i = 0; i < perStep; i++)
        {
            var saga = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger, dlq, metrics);
            var cid = $"SF-{i:D4}";
            saga.Handle(new PaymentCommand { CorrelationId = cid, Request = TestDataFactory.CreateValidRequest(cid, amount: 10m), IdempotencyKey = $"ID-{cid}" });
            saga.Handle(new PaymentValidated { CorrelationId = cid, IsValid = true });
            saga.State.Status = "ReservingFunds";
            saga.Handle(new FundsReserved { CorrelationId = cid, IsSuccessful = true, ReservationId = $"RSV-{cid}", Amount = 10m });
            saga.Handle(new PaymentSettledInternal { CorrelationId = cid, IsSuccessful = false, ErrorMessage = "Settle bulk fail" });
            saga.State.Status.Should().Be("Failed");
            settleFails.Add(cid);
        }

        // Successes (25)
        for (int i = 0; i < perStep; i++)
        {
            var saga = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger, dlq, metrics);
            var cid = $"OK-{i:D4}";
            saga.Handle(new PaymentCommand { CorrelationId = cid, Request = TestDataFactory.CreateValidRequest(cid, amount: 10m), IdempotencyKey = $"ID-{cid}" });
            saga.Handle(new PaymentValidated { CorrelationId = cid, IsValid = true });
            saga.State.Status = "ReservingFunds";
            saga.Handle(new FundsReserved { CorrelationId = cid, IsSuccessful = true, ReservationId = $"RSV-{cid}", Amount = 10m });
            saga.Handle(new PaymentSettledInternal { CorrelationId = cid, IsSuccessful = true, SettlementId = $"STL-{cid}" });
            saga.State.Status.Should().Be("Settled");
            successes.Add(cid);
        }

        dlq.PublishedEvents.Should().HaveCount(75); // 25 * 3 failure categories
        validateFails.Should().HaveCount(25);
        reserveFails.Should().HaveCount(25);
        settleFails.Should().HaveCount(25);
        successes.Should().HaveCount(25);
    }

    // ──── Boundary Amount Tests ────

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.50)]
    [InlineData(1)]
    [InlineData(99.99)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(9999.99)]
    [InlineData(100000)]
    [InlineData(999999.99)]
    [InlineData(999999999999.99)]
    public void Saga_WithBoundaryAmount_CompletesSuccessfully(double amount)
    {
        var dlq = new CapturingDLQPublisher();
        var (_, _, _, sagaLogger, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var saga = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger, dlq, metrics);
        var cid = $"BOUND-AMT-{amount}";
        var decimalAmount = (decimal)amount;

        saga.Handle(new PaymentCommand
        {
            CorrelationId = cid,
            Request = TestDataFactory.CreateValidRequest(cid, amount: decimalAmount),
            IdempotencyKey = $"ID-{cid}",
        });

        saga.Handle(new PaymentValidated { CorrelationId = cid, IsValid = true });
        saga.State.Status = "ReservingFunds";
        saga.Handle(new FundsReserved { CorrelationId = cid, IsSuccessful = true, ReservationId = $"RSV-{cid}", Amount = decimalAmount });
        saga.Handle(new PaymentSettledInternal { CorrelationId = cid, IsSuccessful = true, SettlementId = $"STL-{cid}" });
        saga.State.Status.Should().Be("Settled");
    }

    // ──── State Persistence Tests ────

    [Fact]
    public void SagaState_PreservedBetweenSteps()
    {
        var dlq = new CapturingDLQPublisher();
        var (_, _, _, sagaLogger, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var saga = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger, dlq, metrics);
        var cid = "PERSIST-001";

        saga.Handle(new PaymentCommand
        {
            CorrelationId = cid,
            Request = TestDataFactory.CreateValidRequest(cid, amount: 444.44m, currency: "GBP"),
            IdempotencyKey = "ID-PERSIST",
        });

        // State has request data
        saga.State.PaymentRequest!.Amount.Should().Be(444.44m);
        saga.State.PaymentRequest.Currency.Should().Be("GBP");

        saga.Handle(new PaymentValidated { CorrelationId = cid, IsValid = true });

        // State still has request data after validation
        saga.State.PaymentRequest.Should().NotBeNull();
        saga.State.PaymentRequest.Amount.Should().Be(444.44m);

        saga.State.Status = "ReservingFunds";
        saga.Handle(new FundsReserved { CorrelationId = cid, IsSuccessful = true, ReservationId = "RSV-PERSIST", Amount = 444.44m });

        // Reservation ID persisted
        saga.State.ReservationId.Should().Be("RSV-PERSIST");
        saga.State.Status.Should().Be("Settling");

        saga.Handle(new PaymentSettledInternal { CorrelationId = cid, IsSuccessful = true, SettlementId = "STL-PERSIST" });

        // Completion metadata
        saga.State.Status.Should().Be("Settled");
        saga.State.CompletedAt.Should().NotBeNull();
        saga.State.Version.Should().Be(0);
    }

    // ──── Error Code Consistency Tests ────

    [Fact]
    public void Failure_CorrectErrorCodes_ForEachStep()
    {
        // Validate failure
        var dlq1 = new CapturingDLQPublisher();
        var (_, _, _, slog1, _, met1) = TestFixtureFactory.CreateLoggersAndMetrics();
        var s1 = new PaymentService.Workers.Sagas.PaymentSaga(slog1, dlq1, met1);
        s1.Handle(new PaymentCommand { CorrelationId = "EC-V", Request = TestDataFactory.CreateValidRequest("EC-V"), IdempotencyKey = "ID" });
        s1.Handle(new PaymentValidated { CorrelationId = "EC-V", IsValid = false });
        s1.State.ErrorCode.Should().Be("VALIDATION_FAILED");

        // Reserve failure
        var dlq2 = new CapturingDLQPublisher();
        var (_, _, _, slog2, _, met2) = TestFixtureFactory.CreateLoggersAndMetrics();
        var s2 = new PaymentService.Workers.Sagas.PaymentSaga(slog2, dlq2, met2);
        s2.Handle(new PaymentCommand { CorrelationId = "EC-R", Request = TestDataFactory.CreateValidRequest("EC-R"), IdempotencyKey = "ID" });
        s2.Handle(new PaymentValidated { CorrelationId = "EC-R", IsValid = true });
        s2.State.Status = "ReservingFunds";
        s2.Handle(new FundsReserved { CorrelationId = "EC-R", IsSuccessful = false, ErrorMessage = "Funds" });
        s2.State.ErrorCode.Should().Be("RESERVATION_FAILED");

        // Settle failure
        var dlq3 = new CapturingDLQPublisher();
        var (_, _, _, slog3, _, met3) = TestFixtureFactory.CreateLoggersAndMetrics();
        var s3 = new PaymentService.Workers.Sagas.PaymentSaga(slog3, dlq3, met3);
        s3.Handle(new PaymentCommand { CorrelationId = "EC-S", Request = TestDataFactory.CreateValidRequest("EC-S"), IdempotencyKey = "ID" });
        s3.Handle(new PaymentValidated { CorrelationId = "EC-S", IsValid = true });
        s3.State.Status = "ReservingFunds";
        s3.Handle(new FundsReserved { CorrelationId = "EC-S", IsSuccessful = true, ReservationId = "RSV", Amount = 100m });
        s3.Handle(new PaymentSettledInternal { CorrelationId = "EC-S", IsSuccessful = false, ErrorMessage = "Settle" });
        s3.State.ErrorCode.Should().Be("SETTLEMENT_FAILED");
    }

    // ──── Concurrent Stress Simulation ────

    [Fact]
    public async Task ParallelBulkSagas_200Sagas_NoCrossContamination()
    {
        const int count = 200;
        var dlq = new CapturingDLQPublisher();
        var (_, _, _, sagaLogger, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, count).Select(i => Task.Run(() =>
        {
            try
            {
                var saga = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger, dlq, metrics);
                var cid = $"PAR-{i:D4}";
                saga.Handle(new PaymentCommand
                {
                    CorrelationId = cid,
                    Request = TestDataFactory.CreateValidRequest(cid, amount: 5m * (i + 1)),
                    IdempotencyKey = $"ID-PAR-{i:D4}",
                });
                saga.Handle(new PaymentValidated { CorrelationId = cid, IsValid = true });
                saga.State.Status = "ReservingFunds";
                saga.Handle(new FundsReserved { CorrelationId = cid, IsSuccessful = true, ReservationId = $"RSV-PAR-{i:D4}", Amount = 5m * (i + 1) });
                saga.Handle(new PaymentSettledInternal { CorrelationId = cid, IsSuccessful = true, SettlementId = $"STL-PAR-{i:D4}" });

                if (saga.State.Status != "Settled")
                    errors.Add(new Exception($"Saga {cid} status: {saga.State.Status}"));
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        }));

        await Task.WhenAll(tasks);

        errors.Should().BeEmpty($"All {count} parallel sagas should complete without errors");
    }

    // ──── Command/Event Field Validation Tests ────

    [Fact]
    public void ValidatePaymentCommand_WithAllFields_IsComplete()
    {
        var cmd = new ValidatePaymentCommand
        {
            CorrelationId = "CMD-FULL-001",
            PaymentRequest = TestDataFactory.CreateValidRequest("CMD-FULL-001"),
            CreatedAt = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc),
        };

        cmd.CorrelationId.Should().Be("CMD-FULL-001");
        cmd.PaymentRequest.CorrelationId.Should().Be("CMD-FULL-001");
        cmd.CreatedAt.Should().Be(new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ReserveFundsCommand_WithAllFields_IsComplete()
    {
        var cmd = new ReserveFundsCommand
        {
            CorrelationId = "CMD-RSV-001",
            Amount = 999.99m,
            SenderAccount = "DE89370400440532013000",
            CreatedAt = DateTime.UtcNow,
        };

        cmd.CorrelationId.Should().Be("CMD-RSV-001");
        cmd.Amount.Should().Be(999.99m);
        cmd.SenderAccount.Should().Be("DE89370400440532013000");
    }

    [Fact]
    public void SettlePaymentCommand_WithAllFields_IsComplete()
    {
        var cmd = new SettlePaymentCommand
        {
            CorrelationId = "CMD-STL-001",
            ReservationId = "RSV-REF-ABC",
            Amount = 2500.50m,
            ReceiverAccount = "FR1420041010050500013M02606",
            CreatedAt = DateTime.UtcNow,
        };

        cmd.CorrelationId.Should().Be("CMD-STL-001");
        cmd.ReservationId.Should().Be("RSV-REF-ABC");
        cmd.Amount.Should().Be(2500.50m);
        cmd.ReceiverAccount.Should().Be("FR1420041010050500013M02606");
    }

    // ──── Saga State Edge Cases ────

    [Fact]
    public void SagaState_NewlyCreated_HasDefaults()
    {
        var state = new PaymentSagaState();
        state.Id.Should().BeEmpty();
        state.CorrelationId.Should().BeEmpty();
        state.Status.Should().Be("Validating");
        state.PaymentRequest.Should().BeNull();
        state.ReservationId.Should().BeNull();
        state.ErrorReason.Should().BeNull();
        state.ErrorCode.Should().BeNull();
        state.RetryCount.Should().Be(0);
        state.CompletedAt.Should().BeNull();
        state.Version.Should().Be(0);
    }

    [Fact]
    public void SagaState_CanBeMutated()
    {
        var state = new PaymentSagaState();
        state.Id = "test-123";
        state.CorrelationId = "corr-456";
        state.Status = "Settled";
        state.ReservationId = "RSV-789";
        state.ErrorReason = "None";
        state.RetryCount = 1;
        state.CompletedAt = DateTime.UtcNow;
        state.Version = 42;

        state.Id.Should().Be("test-123");
        state.CorrelationId.Should().Be("corr-456");
        state.Status.Should().Be("Settled");
        state.ReservationId.Should().Be("RSV-789");
        state.ErrorReason.Should().Be("None");
        state.RetryCount.Should().Be(1);
        state.CompletedAt.Should().NotBeNull();
        state.Version.Should().Be(42);
    }

    // ──── Event Equality Tests ────

    [Fact]
    public void PaymentValidated_WithSameData_AreEqual()
    {
        var a = new PaymentValidated { CorrelationId = "EQ-1", IsValid = true };
        var b = new PaymentValidated { CorrelationId = "EQ-1", IsValid = true };
        a.Should().BeEquivalentTo(b);
    }

    [Fact]
    public void FundsReserved_WithDifferentReservationIds_AreDifferent()
    {
        var a = new FundsReserved { CorrelationId = "EQ-1", ReservationId = "RSV-A" };
        var b = new FundsReserved { CorrelationId = "EQ-1", ReservationId = "RSV-B" };
        a.Should().NotBeEquivalentTo(b);
    }

    [Fact]
    public void PaymentSettledInternal_SuccessAndFailure_HaveDifferentShape()
    {
        var success = new PaymentSettledInternal
        {
            CorrelationId = "EQ-1",
            IsSuccessful = true,
            SettlementId = "STL-1",
            ErrorMessage = null,
        };

        var failure = new PaymentSettledInternal
        {
            CorrelationId = "EQ-1",
            IsSuccessful = false,
            SettlementId = "",
            ErrorMessage = "Error occurred",
        };

        success.IsSuccessful.Should().BeTrue();
        failure.IsSuccessful.Should().BeFalse();
        success.ErrorMessage.Should().BeNull();
        failure.ErrorMessage.Should().NotBeNull();
    }

    [Fact]
    public void FakeLedgerService_ReleaseTracking_Works()
    {
        var ledger = new FakeLedgerService();
        ledger.ReleasedReservations.Should().BeEmpty();

        ledger.ReleaseReservationAsync("CID-1", "RSV-1").Wait();
        ledger.ReleaseReservationAsync("CID-2", "RSV-2").Wait();
        ledger.ReleaseReservationAsync("CID-3", "RSV-3").Wait();

        ledger.ReleasedReservations.Should().BeEquivalentTo(new[] { "RSV-1", "RSV-2", "RSV-3" });
    }

    [Theory]
    [InlineData("USD", 1)]
    [InlineData("EUR", 2)]
    [InlineData("GBP", 3)]
    [InlineData("CHF", 4)]
    [InlineData("JPY", 5)]
    public void Saga_WithVariousCurrenciesAndAmounts_CompletesSuccessfully(string currency, int multiplier)
    {
        var dlq = new CapturingDLQPublisher();
        var (_, _, _, sagaLogger, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var saga = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger, dlq, metrics);
        var cid = $"CUR-{currency}";
        var amount = 100m * multiplier;

        saga.Handle(new PaymentCommand
        {
            CorrelationId = cid,
            Request = TestDataFactory.CreateValidRequest(cid, amount: amount, currency: currency),
            IdempotencyKey = $"ID-{cid}",
        });
        saga.Handle(new PaymentValidated { CorrelationId = cid, IsValid = true });
        saga.State.Status = "ReservingFunds";
        saga.Handle(new FundsReserved { CorrelationId = cid, IsSuccessful = true, ReservationId = $"RSV-{cid}", Amount = amount });
        saga.Handle(new PaymentSettledInternal { CorrelationId = cid, IsSuccessful = true, SettlementId = $"STL-{cid}" });
        saga.State.Status.Should().Be("Settled");
    }
}
// END_BLOCK_TESTS
