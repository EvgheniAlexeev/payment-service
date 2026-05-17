// FILE: PaymentSagaTests.cs
// VERSION: 2.0.0
// MODULE: M-INTEGRATION
// PURPOSE: Test specification
// SEMANTIC_TAG: [TEST]
// START_MODULE M_INTEGRATION

// START_MODULE TESTS
// START_BLOCK_TESTS PaymentSagaTests
// PURPOSE: Comprehensive tests for the PaymentSaga orchestrator.
//          Covers: full happy-path, each failure step, DLQ publishing,
//          state transitions, edge cases, multi-saga scenarios.
//          Tests: ~50
// SEMANTIC_TAG: [BLOCK_TEST_SAGA] PaymentSaga orchestration tests
namespace PaymentService.Workers.IntegrationTests;

using Microsoft.Extensions.Logging;
using PaymentService.Shared.Commands;
using PaymentService.Shared.Dtos;
using PaymentService.Shared.Events;
using PaymentService.Workers.Commands;
using PaymentService.Workers.Events;
using PaymentService.Workers.Sagas;
using PaymentService.Workers.Services;
using PaymentService.Workers.Services.Implementations;

public class PaymentSagaTests
{
    // ──────────────── Saga Start Tests ────────────────

    [Fact]
    public void Handle_PaymentCommand_ReturnsValidatePaymentCommand()
    {
        var (saga, state) = CreateSaga();
        var request = TestDataFactory.CreateValidRequest("SAGA-START-001", amount: 100m);
        var command = new PaymentCommand
        {
            CorrelationId = "SAGA-START-001",
            PaymentRequest = request,
            IdempotencyKey = "IDEM-START-001",
        };

        var result = saga.Handle(command);

        result.Should().BeOfType<ValidatePaymentCommand>();
        var validateCmd = (ValidatePaymentCommand)result;
        validateCmd.CorrelationId.Should().Be("SAGA-START-001");
        validateCmd.PaymentRequest.Should().BeEquivalentTo(request);

        state.CorrelationId.Should().Be("SAGA-START-001");
        state.Status.Should().Be("Validating");
        state.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        state.PaymentRequest.Should().BeEquivalentTo(request);
    }

    [Fact]
    public void Handle_PaymentCommand_SetsSagaStateCorrectly()
    {
        var (saga, state) = CreateSaga();
        var request = TestDataFactory.CreateValidRequest("SAGA-START-002", amount: 250m, currency: "EUR");
        var command = new PaymentCommand
        {
            CorrelationId = "SAGA-START-002",
            PaymentRequest = request,
            IdempotencyKey = "IDEM-002",
        };

        saga.Handle(command);

        state.Id.Should().Be("SAGA-START-002");
        state.Status.Should().Be("Validating");
        state.PaymentRequest!.Currency.Should().Be("EUR");
        state.PaymentRequest.Amount.Should().Be(250m);
        state.ErrorReason.Should().BeNull();
        state.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Handle_PaymentCommand_WithLargeAmount_CreatesSaga()
    {
        var (saga, state) = CreateSaga();
        var request = TestDataFactory.CreateValidRequest("SAGA-LARGE", amount: 999_999m);
        var command = new PaymentCommand
        {
            CorrelationId = "SAGA-LARGE",
            PaymentRequest = request,
            IdempotencyKey = "IDEM-LARGE",
        };

        var result = saga.Handle(command);
        result.Should().BeOfType<ValidatePaymentCommand>();
        state.PaymentRequest!.Amount.Should().Be(999_999m);
    }

    // ──────────────── Validation Response Tests ────────────────

    [Fact]
    public void Handle_PaymentValidated_Success_ReturnsReserveFundsCommand()
    {
        var (saga, state) = CreateSaga(started: true, correlationId: "SAGA-VALID-OK");
        var successEvent = new PaymentValidated
        {
            CorrelationId = "SAGA-VALID-OK",
            IsValid = true,
            ValidatedAt = DateTime.UtcNow,
        };

        var result = saga.Handle(successEvent);

        result.Should().BeOfType<ReserveFundsCommand>();
        var reserveCmd = (ReserveFundsCommand)result;
        reserveCmd.CorrelationId.Should().Be("SAGA-VALID-OK");
        reserveCmd.Amount.Should().Be(state.PaymentRequest!.Amount);
        reserveCmd.SenderAccount.Should().Be(state.PaymentRequest.SenderAccount);

        state.Status.Should().Be("ReservingFunds");
    }

    [Fact]
    public void Handle_PaymentValidated_Failure_ReturnsFailedEvent_AndMarksComplete()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, state) = CreateSaga(started: true, correlationId: "SAGA-VALID-FAIL", dlq: dlq);

        var failEvent = new PaymentValidated
        {
            CorrelationId = "SAGA-VALID-FAIL",
            IsValid = false,
            ErrorMessage = "Invalid sender account format",
            ValidatedAt = DateTime.UtcNow,
        };

        var result = saga.Handle(failEvent);

        result.Should().BeOfType<PaymentFailed>();
        var failed = (PaymentFailed)result;
        failed.CorrelationId.Should().Be("SAGA-VALID-FAIL");
        failed.FailedStep.Should().Be("Validate");
        failed.ErrorCode.Should().Be("VALIDATION_FAILED");
        failed.ErrorMessage.Should().Be("Invalid sender account format");

        state.Status.Should().Be("Failed");
        state.ErrorReason.Should().Be("Invalid sender account format");
        state.ErrorCode.Should().Be("VALIDATION_FAILED");
        state.CompletedAt.Should().NotBeNull();

        dlq.PublishedEvents.Should().ContainSingle()
            .Which.CorrelationId.Should().Be("SAGA-VALID-FAIL");
    }

    [Fact]
    public void Handle_PaymentValidated_NullErrorMessage_UsesDefault()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, state) = CreateSaga(started: true, correlationId: "SAGA-VALID-NULL", dlq: dlq);

        var failEvent = new PaymentValidated
        {
            CorrelationId = "SAGA-VALID-NULL",
            IsValid = false,
            ErrorMessage = null,
        };

        var result = saga.Handle(failEvent);

        var failed = (PaymentFailed)result;
        failed.ErrorMessage.Should().Be("Validation failed");
        dlq.PublishedEvents.Should().ContainSingle();
    }

    [Fact]
    public void Handle_PaymentValidated_Failure_PublishesToDLQ()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, state) = CreateSaga(started: true, correlationId: "SAGA-DLQ-TEST", dlq: dlq);

        var failEvent = new PaymentValidated
        {
            CorrelationId = "SAGA-DLQ-TEST",
            IsValid = false,
            ErrorMessage = "Compliance check failed: OFAC sanction match",
        };

        saga.Handle(failEvent);

        dlq.PublishedEvents.Should().ContainSingle();
        var dlqEvent = dlq.PublishedEvents[0];
        dlqEvent.CorrelationId.Should().Be("SAGA-DLQ-TEST");
        dlqEvent.OriginalRequest.Should().NotBeNull();
        dlqEvent.FailedStep.Should().Be("Validate");
        dlqEvent.ErrorCode.Should().Be("VALIDATION_FAILED");
    }

    // ──────────────── Funds Reserved Response Tests ────────────────

    [Fact]
    public void Handle_FundsReserved_Success_ReturnsSettlePaymentCommand()
    {
        var (saga, state) = CreateSaga(started: true, correlationId: "SAGA-RSV-OK");
        state.Status = "ReservingFunds";
        state.PaymentPaymentRequest = TestDataFactory.CreateValidRequest("SAGA-RSV-OK", amount: 500m);

        var successEvent = new FundsReserved
        {
            CorrelationId = "SAGA-RSV-OK",
            IsSuccessful = true,
            ReservationId = "RSV-ABCDEF123456",
            Amount = 500m,
            ReservedAt = DateTime.UtcNow,
        };

        var result = saga.Handle(successEvent);

        result.Should().BeOfType<SettlePaymentCommand>();
        var settleCmd = (SettlePaymentCommand)result;
        settleCmd.CorrelationId.Should().Be("SAGA-RSV-OK");
        settleCmd.ReservationId.Should().Be("RSV-ABCDEF123456");
        settleCmd.Amount.Should().Be(500m);

        state.Status.Should().Be("Settling");
        state.ReservationId.Should().Be("RSV-ABCDEF123456");
    }

    [Fact]
    public void Handle_FundsReserved_Failure_ReturnsFailedEvent()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, state) = CreateSaga(started: true, correlationId: "SAGA-RSV-FAIL", dlq: dlq);
        state.Status = "ReservingFunds";
        state.PaymentPaymentRequest = TestDataFactory.CreateValidRequest("SAGA-RSV-FAIL");

        var failEvent = new FundsReserved
        {
            CorrelationId = "SAGA-RSV-FAIL",
            IsSuccessful = false,
            ErrorMessage = "Insufficient funds in sender account",
            ReservationId = string.Empty,
        };

        var result = saga.Handle(failEvent);

        result.Should().BeOfType<PaymentFailed>();
        var failed = (PaymentFailed)result;
        failed.FailedStep.Should().Be("ReserveFunds");
        failed.ErrorCode.Should().Be("RESERVATION_FAILED");

        state.Status.Should().Be("Failed");
        dlq.PublishedEvents.Should().ContainSingle();
    }

    [Fact]
    public void Handle_FundsReserved_Failure_NullErrorMessage_UsesDefault()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, state) = CreateSaga(started: true, correlationId: "SAGA-RSV-NULL", dlq: dlq);
        state.Status = "ReservingFunds";
        state.PaymentPaymentRequest = TestDataFactory.CreateValidRequest("SAGA-RSV-NULL");

        var failEvent = new FundsReserved
        {
            CorrelationId = "SAGA-RSV-NULL",
            IsSuccessful = false,
            ErrorMessage = null,
        };

        var result = saga.Handle(failEvent);
        var failed = (PaymentFailed)result;
        failed.ErrorMessage.Should().Be("Fund reservation failed");
    }

    // ──────────────── Settlement Response Tests ────────────────

    [Fact]
    public void Handle_PaymentSettledInternal_Success_ReturnsPaymentSettled_AndMarksComplete()
    {
        var (saga, state) = CreateSaga(started: true, correlationId: "SAGA-STL-OK");
        state.Status = "Settling";
        state.PaymentPaymentRequest = TestDataFactory.CreateValidRequest("SAGA-STL-OK", amount: 750m);
        state.ReservationId = "RSV-SAGA-STL";

        var successEvent = new PaymentSettledInternal
        {
            CorrelationId = "SAGA-STL-OK",
            IsSuccessful = true,
            SettlementId = "STL-FINAL-12345",
            SettledAt = DateTime.UtcNow,
        };

        var result = saga.Handle(successEvent);

        result.Should().BeOfType<PaymentSettled>();
        var settled = (PaymentSettled)result;
        settled.CorrelationId.Should().Be("SAGA-STL-OK");
        settled.SettlementId.Should().Be("STL-FINAL-12345");
        settled.Status.Should().Be("Settled");

        state.Status.Should().Be("Settled");
        state.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Handle_PaymentSettledInternal_Failure_ReturnsFailedEvent()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, state) = CreateSaga(started: true, correlationId: "SAGA-STL-FAIL", dlq: dlq);
        state.Status = "Settling";
        state.PaymentPaymentRequest = TestDataFactory.CreateValidRequest("SAGA-STL-FAIL");
        state.ReservationId = "RSV-STL-FAIL";

        var failEvent = new PaymentSettledInternal
        {
            CorrelationId = "SAGA-STL-FAIL",
            IsSuccessful = false,
            ErrorMessage = "Counterparty bank unreachable",
            SettlementId = string.Empty,
        };

        var result = saga.Handle(failEvent);

        result.Should().BeOfType<PaymentFailed>();
        var failed = (PaymentFailed)result;
        failed.FailedStep.Should().Be("Settle");
        failed.ErrorCode.Should().Be("SETTLEMENT_FAILED");

        state.Status.Should().Be("Failed");
        dlq.PublishedEvents.Should().ContainSingle();
    }

    // ───────────────── Full Happy-Path Flow Test ───────────────

    [Fact]
    public void FullHappyPath_ValidateReserveSettle_CompletesSuccessfully()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, state) = CreateSaga(correlationId: "FULL-HAPPY-001", dlq: dlq);
        var request = TestDataFactory.CreateValidRequest("FULL-HAPPY-001", amount: 1000m, currency: "USD");

        // Step 1: Start saga
        var startCmd = new PaymentCommand
        {
            CorrelationId = "FULL-HAPPY-001",
            PaymentRequest = request,
            IdempotencyKey = "IDEM-FULL-001",
        };
        var step1 = saga.Handle(startCmd);
        step1.Should().BeOfType<ValidatePaymentCommand>();
        state.Status.Should().Be("Validating");

        // Step 2: Validation passes
        var validated = new PaymentValidated
        {
            CorrelationId = "FULL-HAPPY-001",
            IsValid = true,
            ValidatedAt = DateTime.UtcNow,
        };
        var step2 = saga.Handle(validated);
        step2.Should().BeOfType<ReserveFundsCommand>();
        state.Status.Should().Be("ReservingFunds");

        // Step 3: Reservation succeeds
        var reserved = new FundsReserved
        {
            CorrelationId = "FULL-HAPPY-001",
            IsSuccessful = true,
            ReservationId = "RSV-FULL-001",
            Amount = 1000m,
            ReservedAt = DateTime.UtcNow,
        };
        var step3 = saga.Handle(reserved);
        step3.Should().BeOfType<SettlePaymentCommand>();
        var settleCmd = (SettlePaymentCommand)step3;
        settleCmd.ReservationId.Should().Be("RSV-FULL-001");
        state.Status.Should().Be("Settling");

        // Step 4: Settlement succeeds
        var settled = new PaymentSettledInternal
        {
            CorrelationId = "FULL-HAPPY-001",
            IsSuccessful = true,
            SettlementId = "STL-FULL-001",
            SettledAt = DateTime.UtcNow,
        };
        var step4 = saga.Handle(settled);
        step4.Should().BeOfType<PaymentSettled>();
        state.Status.Should().Be("Settled");
        state.CompletedAt.Should().NotBeNull();

        dlq.PublishedEvents.Should().BeEmpty();
    }

    // ───────────────── Failure at Each Step ───────────────

    [Theory]
    [InlineData("STEP1-FAIL-VALIDATE")]
    [InlineData("STEP1-FAIL-RESERVE")]
    [InlineData("STEP1-FAIL-SETTLE")]
    public void PartialSaga_FailAtEachStep_DLQPublished(string correlationId)
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, state) = CreateSaga(correlationId: correlationId, dlq: dlq);
        var request = TestDataFactory.CreateValidRequest(correlationId, amount: 500m);

        // Start saga
        var startCmd = new PaymentCommand
        {
            CorrelationId = correlationId,
            PaymentRequest = request,
            IdempotencyKey = $"IDEM-{correlationId}",
        };
        saga.Handle(startCmd);

        if (correlationId.Contains("VALIDATE"))
        {
            saga.Handle(new PaymentValidated
            {
                CorrelationId = correlationId,
                IsValid = false,
                ErrorMessage = "Test validation failure",
            });
            state.Status.Should().Be("Failed");
            state.ErrorCode.Should().Be("VALIDATION_FAILED");
        }
        else if (correlationId.Contains("RESERVE"))
        {
            state.Status = "ReservingFunds";
            saga.Handle(new PaymentValidated
            {
                CorrelationId = correlationId,
                IsValid = true,
            });
            saga.Handle(new FundsReserved
            {
                CorrelationId = correlationId,
                IsSuccessful = false,
                ErrorMessage = "Test reservation failure",
            });
            state.Status.Should().Be("Failed");
            state.ErrorCode.Should().Be("RESERVATION_FAILED");
        }
        else
        {
            state.Status = "ReservingFunds";
            saga.Handle(new PaymentValidated
            {
                CorrelationId = correlationId,
                IsValid = true,
            });
            state.Status = "Settling";
            saga.Handle(new FundsReserved
            {
                CorrelationId = correlationId,
                IsSuccessful = true,
                ReservationId = "RSV-TEST",
                Amount = 500m,
            });
            saga.Handle(new PaymentSettledInternal
            {
                CorrelationId = correlationId,
                IsSuccessful = false,
                ErrorMessage = "Test settlement failure",
            });
            state.Status.Should().Be("Failed");
            state.ErrorCode.Should().Be("SETTLEMENT_FAILED");
        }

        dlq.PublishedEvents.Should().NotBeEmpty();
        dlq.PublishedEvents[0].OriginalRequest.Should().NotBeNull();
    }

    // ───────────────── Multiple Sagas ───────────────

    [Fact]
    public void MultipleSagas_EachIndependent_NoCrossContamination()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga1, state1) = CreateSaga(correlationId: "MULTI-SAGA-1", dlq: dlq);
        var (saga2, state2) = CreateSaga(correlationId: "MULTI-SAGA-2", dlq: dlq);

        // Saga 1: success path
        var request1 = TestDataFactory.CreateValidRequest("MULTI-SAGA-1", amount: 100m);
        saga1.Handle(new PaymentCommand
        {
            CorrelationId = "MULTI-SAGA-1",
            PaymentRequest = request1,
            IdempotencyKey = "IDEM-M1",
        });
        saga1.Handle(new PaymentValidated { CorrelationId = "MULTI-SAGA-1", IsValid = true });
        saga1.Handle(new FundsReserved
        {
            CorrelationId = "MULTI-SAGA-1",
            IsSuccessful = true,
            ReservationId = "RSV1",
            Amount = 100m,
        });
        saga1.Handle(new PaymentSettledInternal
        {
            CorrelationId = "MULTI-SAGA-1",
            IsSuccessful = true,
            SettlementId = "STL1",
        });

        // Saga 2: fails at validation
        var request2 = TestDataFactory.CreateValidRequest("MULTI-SAGA-2", amount: 200m);
        saga2.Handle(new PaymentCommand
        {
            CorrelationId = "MULTI-SAGA-2",
            PaymentRequest = request2,
            IdempotencyKey = "IDEM-M2",
        });
        saga2.Handle(new PaymentValidated
        {
            CorrelationId = "MULTI-SAGA-2",
            IsValid = false,
            ErrorMessage = "Invalid data",
        });

        state1.Status.Should().Be("Settled");
        state2.Status.Should().Be("Failed");

        dlq.PublishedEvents.Should().HaveCount(1);
        dlq.PublishedEvents[0].CorrelationId.Should().Be("MULTI-SAGA-2");
    }

    // ───────────────── DLQ Event Content Tests ───────────────

    [Fact]
    public void DLQEvent_ContainsOriginalRequest_ForOperatorReview()
    {
        var dlq = new CapturingDLQPublisher();
        var (saga, _) = CreateSaga(started: true, correlationId: "DLQ-CONTENT-001", dlq: dlq);

        var request = TestDataFactory.CreateValidRequest(
            "DLQ-CONTENT-001",
            amount: 1234.56m,
            currency: "EUR",
            senderAccount: "DE89370400440532013000",
            receiverAccount: "FR1420041010050500013M02606",
            description: "Invoice payment #45678");

        saga.Handle(new PaymentCommand
        {
            CorrelationId = "DLQ-CONTENT-001",
            PaymentRequest = request,
            IdempotencyKey = "IDEM-DLQ-001",
        });

        saga.Handle(new PaymentValidated
        {
            CorrelationId = "DLQ-CONTENT-001",
            IsValid = false,
            ErrorMessage = "Compliance: high-risk jurisdiction",
        });

        var dlqEvent = dlq.PublishedEvents.Single();
        dlqEvent.OriginalRequest!.Amount.Should().Be(1234.56m);
        dlqEvent.OriginalRequest.Currency.Should().Be("EUR");
        dlqEvent.OriginalRequest.SenderAccount.Should().Be("DE89370400440532013000");
        dlqEvent.OriginalRequest.ReceiverAccount.Should().Be("FR1420041010050500013M02606");
        dlqEvent.OriginalRequest.Description.Should().Be("Invoice payment #45678");
        dlqEvent.FailedStep.Should().Be("Validate");
        dlqEvent.ErrorCode.Should().Be("VALIDATION_FAILED");
    }

    // ───────────────── Helpers ───────────────

    private static (
        PaymentService.Workers.Sagas.PaymentSaga saga,
        PaymentSagaState state
    ) CreateSaga(
        bool started = false,
        string correlationId = "TEST-CORR",
        IDLQPublisher? dlq = null)
    {
        var (_, _, _, sagaLogger, dlqLogger, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        dlq ??= new CapturingDLQPublisher();

        var saga = new PaymentService.Workers.Sagas.PaymentSaga(
            sagaLogger,
            dlq,
            metrics);

        if (started)
        {
            saga.State.Id = correlationId;
            saga.State.CorrelationId = correlationId;
            saga.State.PaymentPaymentRequest = TestDataFactory.CreateValidRequest(correlationId);
            saga.State.Status = "Validating";
            saga.State.CreatedAt = DateTime.UtcNow;
        }

        return (saga, saga.State);
    }
}
// END_BLOCK_TESTS
