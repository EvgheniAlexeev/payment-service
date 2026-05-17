// FILE: tests/PaymentService.Shared.UnitTests/ModelTests.cs
// VERSION: 2.0.0
// MODULE: M-TEST
// PURPOSE: Test specification
// SEMANTIC_TAG: [TEST]
// START_MODULE M_TEST

// FILE: tests/PaymentService.Shared.UnitTests/ModelTests.cs
// VERSION: 1.0.0

using FluentAssertions;
using PaymentService.Shared.Commands;
using PaymentService.Shared.Dtos;
using PaymentService.Shared.Events;
using PaymentService.Shared.Models;

namespace PaymentService.Shared.UnitTests;

/// <summary>
/// Model instantiation and property tests.
/// </summary>
public class ModelTests
{
    [Fact]
    public void PaymentDocument_DefaultsCorrectly()
    {
        var doc = new PaymentDocument();

        doc.CorrelationId.Should().BeEmpty();
        doc.Status.Should().Be("Pending");
        doc.SagaState.Should().Be("None");
        doc.SettledAt.Should().BeNull();
        doc.SettlementId.Should().BeNull();
        doc.Request.Should().NotBeNull();
    }

    [Fact]
    public void PaymentDocument_FullCreation()
    {
        var doc = new PaymentDocument
        {
            Id = "mongo-id-123",
            CorrelationId = "corr-123",
            Status = "Settled",
            SagaState = "Completed",
            CreatedAt = new DateTime(2026, 5, 1),
            SettledAt = new DateTime(2026, 5, 1, 12, 0, 0),
            SettlementId = "SET-001",
            ModifiedAt = new DateTime(2026, 5, 1, 12, 0, 0),
            Request = new PaymentRequestDto
            {
                CorrelationId = "corr-123",
                Amount = 1000m,
                Currency = "USD",
                SenderAccount = "SRC",
                ReceiverAccount = "DST"
            }
        };

        doc.Id.Should().Be("mongo-id-123");
        doc.CorrelationId.Should().Be("corr-123");
        doc.Status.Should().Be("Settled");
        doc.SagaState.Should().Be("Completed");
        doc.SettledAt.Should().NotBeNull();
        doc.SettlementId.Should().Be("SET-001");
        doc.Request.Amount.Should().Be(1000m);
    }

    [Fact]
    public void SagaState_DefaultsCorrectly()
    {
        var state = new SagaState();

        state.CorrelationId.Should().BeEmpty();
        state.CurrentStep.Should().Be("None");
        state.CompletedSteps.Should().BeEmpty();
        state.RetryCount.Should().Be(0);
    }

    [Fact]
    public void SagaState_TracksCompletedSteps()
    {
        var state = new SagaState
        {
            CorrelationId = "corr-123",
            CurrentStep = "Settling",
            CompletedSteps = new List<string> { "Validating", "Enriching" },
            RetryCount = 1
        };

        state.CompletedSteps.Should().HaveCount(2);
        state.CompletedSteps.Should().Contain("Validating");
        state.CompletedSteps.Should().Contain("Enriching");
        state.CurrentStep.Should().Be("Settling");
    }

    [Fact]
    public void PaymentCommand_DefaultsCorrectly()
    {
        var cmd = new PaymentCommand();

        cmd.IdempotencyKey.Should().BeEmpty();
        cmd.CorrelationId.Should().BeEmpty();
        cmd.PaymentRequest.Should().NotBeNull();
    }

    [Fact]
    public void PaymentCommand_FullCreation()
    {
        var cmd = new PaymentCommand
        {
            IdempotencyKey = "idem-123",
            CorrelationId = "corr-123",
            PaymentRequest = new PaymentRequestDto
            {
                CorrelationId = "corr-123",
                Amount = 500m,
                Currency = "EUR"
            }
        };

        cmd.IdempotencyKey.Should().Be("idem-123");
        cmd.CorrelationId.Should().Be("corr-123");
        cmd.PaymentRequest.Amount.Should().Be(500m);
    }

    [Fact]
    public void PaymentSettled_DefaultsCorrectly()
    {
        var evt = new PaymentSettled();

        evt.CorrelationId.Should().BeEmpty();
        evt.SettlementId.Should().BeEmpty();
        evt.Status.Should().Be("Settled");
    }

    [Fact]
    public void PaymentFailed_DefaultsCorrectly()
    {
        var evt = new PaymentFailed();

        evt.CorrelationId.Should().BeEmpty();
        evt.FailedStep.Should().BeEmpty();
        evt.ErrorMessage.Should().BeEmpty();
        evt.ErrorCode.Should().BeEmpty();
        evt.RetryCount.Should().Be(0);
        evt.FailedAt.Should().Be(default);
    }

    [Fact]
    public void PaymentFailed_FullCreation()
    {
        var evt = new PaymentFailed
        {
            CorrelationId = "corr-fail",
            FailedStep = "Settle",
            ErrorMessage = "Insufficient funds",
            ErrorCode = "ERR-001",
            RetryCount = 3,
            FailedAt = new DateTime(2026, 5, 1, 12, 0, 0)
        };

        evt.FailedStep.Should().Be("Settle");
        evt.ErrorCode.Should().Be("ERR-001");
        evt.RetryCount.Should().Be(3);
    }

    [Fact]
    public void Result_Generic_SuccessWrapsData()
    {
        var result = Result<PaymentStatusDto>.Success(
            new PaymentStatusDto { CorrelationId = "c1" });

        result.IsSuccess.Should().BeTrue();
        result.Data!.CorrelationId.Should().Be("c1");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Result_Generic_FailureHasError()
    {
        var result = Result<PaymentStatusDto>.Failure("Something broke");

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().Be("Something broke");
    }

    [Fact]
    public void Result_Generic_NotFoundHasError()
    {
        var result = Result<PaymentStatusDto>.NotFound("Not here");

        result.IsSuccess.Should().BeFalse();
        result.IsNotFound.Should().BeTrue();
        result.Error.Should().Be("Not here");
    }

    [Fact]
    public void Result_NonGeneric_Success()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Result_NonGeneric_Failure()
    {
        var result = Result.Failure("Error");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Error");
    }
}
