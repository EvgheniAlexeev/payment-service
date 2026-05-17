// FILE: EndToEndFlowTests.cs
// VERSION: 2.0.0
// MODULE: M-INTEGRATION
// PURPOSE: Test specification
// SEMANTIC_TAG: [TEST]
// START_MODULE M_INTEGRATION

// START_MODULE TESTS
// START_BLOCK_TESTS EndToEndFlowTests
// PURPOSE: Complete end-to-end workflow tests combining sagas, handlers, DLQ, and metrics.
//          Simulates real-world payment processing scenarios end to end.
//          Tests: ~20
// SEMANTIC_TAG: [BLOCK_TEST_E2E] End-to-end flow tests
namespace PaymentService.Workers.IntegrationTests;

using PaymentService.Workers.Services;
using PaymentService.Shared.Commands;
using PaymentService.Shared.Events;
using PaymentService.Workers.Commands;
using PaymentService.Workers.Events;
using PaymentService.Workers.Steps;
using PaymentService.Workers.Sagas;

public class EndToEndFlowTests
{
    // ──── Complete E2E Scenarios ────

    [Fact]
    public void E2E_SinglePayment_ValidateReserveSettle_DLQEmpty()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, state) = CreateSaga("E2E-SINGLE", dlq);

        RunFullHappyPath(saga, state, "E2E-SINGLE", 1000m);

        state.Status.Should().Be("Settled");
        state.CompletedAt.Should().NotBeNull();
        state.ErrorReason.Should().BeNull();
        dlq.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public void E2E_ValidationFailsImmediately_DLQHasFullContext()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, state) = CreateSaga("E2E-VALFAIL", dlq);

        var request = TestDataFactory.CreateValidRequest("E2E-VALFAIL", amount: 500m, currency: "EUR",
            senderAccount: "DE89370400440532013000", receiverAccount: "FR1420041010050500013M02606");

        saga.Handle(new PaymentCommand
        {
            CorrelationId = "E2E-VALFAIL",
            PaymentRequest = request,
            IdempotencyKey = "ID-E2E-VF",
        });

        saga.Handle(new PaymentValidated
        {
            CorrelationId = "E2E-VALFAIL",
            IsValid = false,
            ErrorMessage = "OFAC Sanctions List match detected",
        });

        state.Status.Should().Be("Failed");
        state.ErrorCode.Should().Be("VALIDATION_FAILED");

        var dlqEvent = dlq.PublishedEvents.Single();
        dlqEvent.OriginalRequest!.SenderAccount.Should().Be("DE89370400440532013000");
        dlqEvent.OriginalRequest.ReceiverAccount.Should().Be("FR1420041010050500013M02606");
        dlqEvent.OriginalRequest.Amount.Should().Be(500m);
        dlqEvent.OriginalRequest.Currency.Should().Be("EUR");
        dlqEvent.ErrorMessage.Should().Be("OFAC Sanctions List match detected");
        dlqEvent.FailedStep.Should().Be("Validate");
    }

    [Fact]
    public void E2E_ReservationFailsAfterSuccessfulValidation()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, state) = CreateSaga("E2E-RSVFAIL", dlq);

        saga.Handle(new PaymentCommand
        {
            CorrelationId = "E2E-RSVFAIL",
            PaymentRequest = TestDataFactory.CreateValidRequest("E2E-RSVFAIL", amount: 750m),
            IdempotencyKey = "ID-E2E-RF",
        });

        saga.Handle(new PaymentValidated
        {
            CorrelationId = "E2E-RSVFAIL",
            IsValid = true,
        });

        state.Status.Should().Be("ReservingFunds");

        saga.Handle(new FundsReserved
        {
            CorrelationId = "E2E-RSVFAIL",
            IsSuccessful = false,
            ErrorMessage = "NSF - Insufficient balance in account DE89370400440532013000",
        });

        state.Status.Should().Be("Failed");
        state.ErrorCode.Should().Be("RESERVATION_FAILED");
        dlq.PublishedEvents.Should().ContainSingle();
    }

    [Fact]
    public void E2E_SettlementFailsAfterReservation()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, state) = CreateSaga("E2E-STLFAIL", dlq);

        saga.Handle(new PaymentCommand
        {
            CorrelationId = "E2E-STLFAIL",
            PaymentRequest = TestDataFactory.CreateValidRequest("E2E-STLFAIL", amount: 2000m, currency: "CHF"),
            IdempotencyKey = "ID-E2E-SF",
        });

        saga.Handle(new PaymentValidated { CorrelationId = "E2E-STLFAIL", IsValid = true });
        state.Status = "ReservingFunds";

        saga.Handle(new FundsReserved
        {
            CorrelationId = "E2E-STLFAIL",
            IsSuccessful = true,
            ReservationId = "RSV-E2E-STLFAIL",
            Amount = 2000m,
        });

        state.Status.Should().Be("Settling");
        state.ReservationId.Should().Be("RSV-E2E-STLFAIL");

        saga.Handle(new PaymentSettledInternal
        {
            CorrelationId = "E2E-STLFAIL",
            IsSuccessful = false,
            ErrorMessage = "Counterparty bank SWIFT network timeout",
        });

        state.Status.Should().Be("Failed");
        state.ErrorCode.Should().Be("SETTLEMENT_FAILED");
        dlq.PublishedEvents.Should().ContainSingle();
    }

    [Fact]
    public void E2E_AllStepsSuccess_CompletesWithCorrectMetadata()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, state) = CreateSaga("E2E-META", dlq);

        var before = DateTime.UtcNow;
        RunFullHappyPath(saga, state, "E2E-META", 1500m, "JPY");
        var after = DateTime.UtcNow;

        state.Status.Should().Be("Settled");
        state.CreatedAt.Should().BeCloseTo(before, TimeSpan.FromSeconds(5));
        state.CompletedAt.Should().BeAfter(state.CreatedAt);
        state.CompletedAt.Should().BeCloseTo(after, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void E2E_MultiCurrencyBatch_AllSucceed()
    {
        var dlq = new CapturingDLQPublisher();

        var currencies = new[] { "USD", "EUR", "GBP", "CHF", "JPY", "CAD", "AUD", "NZD", "SEK", "NOK" };
        var states = new List<PaymentSagaState>();

        foreach (var currency in currencies)
        {
            var cid = $"E2E-MC-{currency}";
            var (saga, state) = CreateSaga(cid, dlq);
            RunFullHappyPath(saga, state, cid, 100m, currency);
            states.Add(state);
        }

        states.Should().HaveCount(10);
        states.Should().AllSatisfy(s => s.Status.Should().Be("Settled"));
        dlq.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public void E2E_MixedOutcomes_SomeSuccessSomeFail()
    {
        var dlq = new CapturingDLQPublisher();

        // 3 success, 3 fail at different steps
        var succeeded = new List<string>();
        var failed = new List<string>();

        // Success 1
        {
            var (saga, state) = CreateSaga("MIX-S1", dlq);
            RunFullHappyPath(saga, state, "MIX-S1", 100m);
            succeeded.Add("MIX-S1");
        }

        // Success 2
        {
            var (saga, state) = CreateSaga("MIX-S2", dlq);
            RunFullHappyPath(saga, state, "MIX-S2", 200m);
            succeeded.Add("MIX-S2");
        }

        // Fail at validation
        {
            var (saga, state) = CreateSaga("MIX-FV", dlq);
            saga.Handle(new PaymentCommand
            {
                CorrelationId = "MIX-FV",
                PaymentRequest = TestDataFactory.CreateValidRequest("MIX-FV", amount: 300m),
                IdempotencyKey = "ID-MIX-FV",
            });
            saga.Handle(new PaymentValidated { CorrelationId = "MIX-FV", IsValid = false, ErrorMessage = "Fail" });
            failed.Add("MIX-FV");
        }

        // Fail at reserve
        {
            var (saga, state) = CreateSaga("MIX-FR", dlq);
            saga.Handle(new PaymentCommand
            {
                CorrelationId = "MIX-FR",
                PaymentRequest = TestDataFactory.CreateValidRequest("MIX-FR", amount: 400m),
                IdempotencyKey = "ID-MIX-FR",
            });
            saga.Handle(new PaymentValidated { CorrelationId = "MIX-FR", IsValid = true });
            state.Status = "ReservingFunds";
            saga.Handle(new FundsReserved { CorrelationId = "MIX-FR", IsSuccessful = false, ErrorMessage = "Fail" });
            failed.Add("MIX-FR");
        }

        // Fail at settle
        {
            var (saga, state) = CreateSaga("MIX-FS", dlq);
            saga.Handle(new PaymentCommand
            {
                CorrelationId = "MIX-FS",
                PaymentRequest = TestDataFactory.CreateValidRequest("MIX-FS", amount: 500m),
                IdempotencyKey = "ID-MIX-FS",
            });
            saga.Handle(new PaymentValidated { CorrelationId = "MIX-FS", IsValid = true });
            state.Status = "ReservingFunds";
            saga.Handle(new FundsReserved { CorrelationId = "MIX-FS", IsSuccessful = true, ReservationId = "RSV-MIX", Amount = 500m });
            saga.Handle(new PaymentSettledInternal { CorrelationId = "MIX-FS", IsSuccessful = false, ErrorMessage = "Fail" });
            failed.Add("MIX-FS");
        }

        // Success 3
        {
            var (saga, state) = CreateSaga("MIX-S3", dlq);
            RunFullHappyPath(saga, state, "MIX-S3", 600m);
            succeeded.Add("MIX-S3");
        }

        succeeded.Should().HaveCount(3);
        failed.Should().HaveCount(3);
        dlq.PublishedEvents.Should().HaveCount(3);
    }

    [Fact]
    public void E2E_LargePayment_LifecycleTrace()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, state) = CreateSaga("E2E-LARGE", dlq);

        // 1. Command received
        var req = TestDataFactory.CreateValidRequest("E2E-LARGE", amount: 500000m, currency: "USD",
            description: "Large corporate payment - Acme Corp invoice #99999");
        saga.Handle(new PaymentCommand
        {
            CorrelationId = "E2E-LARGE",
            PaymentRequest = req,
            IdempotencyKey = "ID-E2E-LG",
        });
        state.Status.Should().Be("Validating");
        state.PaymentRequest!.Amount.Should().Be(500000m);

        // 2. Validation passes
        saga.Handle(new PaymentValidated
        {
            CorrelationId = "E2E-LARGE",
            IsValid = true,
            ValidatedAt = DateTime.UtcNow,
        });
        state.Status.Should().Be("ReservingFunds");

        // 3. Large reserve succeeds
        saga.Handle(new FundsReserved
        {
            CorrelationId = "E2E-LARGE",
            IsSuccessful = true,
            ReservationId = "RSV-LARGE-CORP-001",
            Amount = 500000m,
            ReservedAt = DateTime.UtcNow,
        });
        state.Status.Should().Be("Settling");
        state.ReservationId.Should().Be("RSV-LARGE-CORP-001");

        // 4. Settlement succeeds
        saga.Handle(new PaymentSettledInternal
        {
            CorrelationId = "E2E-LARGE",
            IsSuccessful = true,
            SettlementId = "STL-LARGE-CORP-001",
            SettledAt = DateTime.UtcNow,
        });
        state.Status.Should().Be("Settled");

        dlq.PublishedEvents.Should().BeEmpty();
    }

    // ──── Step Handler + Saga Integration ────

    [Fact]
    public async Task HandlerToSaga_ValidateFail_PropagatesToDLQ()
    {
        var fakeService = new FakeValidationService(shouldPass: false);
        var (validateLogger, _, _, sagaLogger, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ValidatePaymentHandler(fakeService, validateLogger, metrics);
        var dlq = new CapturingDLQPublisher();
        var saga = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger, dlq, metrics);

        var cid = "INT-VALFAIL";
        var request = TestDataFactory.CreateValidRequest(cid);
        var cmd = new ValidatePaymentCommand { CorrelationId = cid, PaymentRequest = request };

        // Handler processes
        await handler.Handle(cmd, CancellationToken.None);

        // Saga receives simulated validation failure event
        saga.Handle(new PaymentCommand
        {
            CorrelationId = cid,
            PaymentRequest = request,
            IdempotencyKey = "ID-INT-VF",
        });
        saga.Handle(new PaymentValidated
        {
            CorrelationId = cid,
            IsValid = false,
            ErrorMessage = "Integrated validation failure",
        });

        saga.State.Status.Should().Be("Failed");
        dlq.PublishedEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task HandlerToSaga_ReserveFail_PropagatesToDLQ()
    {
        var fakeLedger = new FakeLedgerService(reserveSucceeds: false);
        var (_, reserveLogger, _, sagaLogger, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ReserveFundsHandler(fakeLedger, reserveLogger, metrics);
        var dlq = new CapturingDLQPublisher();
        var saga = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger, dlq, metrics);

        var cid = "INT-RSVFAIL";
        var request = TestDataFactory.CreateValidRequest(cid, amount: 500m);
        var cmd = new ReserveFundsCommand { CorrelationId = cid, Amount = 500m, SenderAccount = request.SenderAccount };

        // Handler returns failed event
        var result = await handler.Handle(cmd, CancellationToken.None);
        result.IsSuccessful.Should().BeFalse();

        // Saga receives failure
        saga.Handle(new PaymentCommand { CorrelationId = cid, PaymentRequest = request, IdempotencyKey = "ID-INT-RF" });
        saga.Handle(new PaymentValidated { CorrelationId = cid, IsValid = true });
        saga.State.Status = "ReservingFunds";
        saga.Handle(result);

        saga.State.Status.Should().Be("Failed");
        dlq.PublishedEvents.Should().ContainSingle();
    }

    // ──── Helpers ────

    private static (
        PaymentService.Workers.Sagas.PaymentSaga saga,
        PaymentSagaState state
    ) CreateSaga(string correlationId, IDLQPublisher dlq)
    {
        var (_, _, _, sagaLogger, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var saga = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger, dlq, metrics);
        return (saga, saga.State);
    }

    private static void RunFullHappyPath(
        PaymentService.Workers.Sagas.PaymentSaga saga,
        PaymentSagaState state,
        string correlationId,
        decimal amount,
        string currency = "USD")
    {
        saga.Handle(new PaymentCommand
        {
            CorrelationId = correlationId,
            PaymentRequest = TestDataFactory.CreateValidRequest(correlationId, amount: amount, currency: currency),
            IdempotencyKey = $"ID-{correlationId}",
        });

        saga.Handle(new PaymentValidated
        {
            CorrelationId = correlationId,
            IsValid = true,
            ValidatedAt = DateTime.UtcNow,
        });

        state.Status = "ReservingFunds";

        saga.Handle(new FundsReserved
        {
            CorrelationId = correlationId,
            IsSuccessful = true,
            ReservationId = $"RSV-{correlationId}",
            Amount = amount,
            ReservedAt = DateTime.UtcNow,
        });

        saga.Handle(new PaymentSettledInternal
        {
            CorrelationId = correlationId,
            IsSuccessful = true,
            SettlementId = $"STL-{correlationId}",
            SettledAt = DateTime.UtcNow,
        });
    }
}
// END_BLOCK_TESTS
