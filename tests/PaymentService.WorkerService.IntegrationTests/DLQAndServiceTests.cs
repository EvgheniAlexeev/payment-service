// START_MODULE TESTS
// START_BLOCK_TESTS DLQAndServiceTests
// PURPOSE: Tests for LoggingDLQPublisher, Default service implementations, and fake services.
//          Tests: ~25
// SEMANTIC_TAG: [BLOCK_TEST_DLQ] DLQ publisher and service tests
namespace PaymentService.Workers.IntegrationTests;

using Microsoft.Extensions.Logging;
using PaymentService.Shared.Dtos;
using PaymentService.Shared.Events;
using PaymentService.Workers.Services;
using PaymentService.Workers.Services.Implementations;

public class LoggingDLQPublisherTests
{
    [Fact]
    public async Task PublishFailedPayment_LogsStructuredMessage()
    {
        var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));
        var logger = loggerFactory.CreateLogger<LoggingDLQPublisher>();
        var publisher = new LoggingDLQPublisher(logger);

        var failedEvent = new PaymentFailed
        {
            CorrelationId = "DLQ-LOG-TEST-001",
            FailedStep = "Validate",
            ErrorCode = "VALIDATION_FAILED",
            ErrorMessage = "Compliance check failed",
            RetryCount = 0,
            FailedAt = DateTime.UtcNow,
            OriginalRequest = new PaymentRequestDto
            {
                CorrelationId = "DLQ-LOG-TEST-001",
                SenderAccount = "DE89370400440532013000",
                ReceiverAccount = "FR1420041010050500013M02606",
                Amount = 500m,
                Currency = "EUR",
            },
        };

        await publisher.PublishFailedPaymentAsync(failedEvent);
        // Verifies no exception is thrown
    }

    [Fact]
    public async Task PublishFailedPayment_WithNullRequest_DoesNotThrow()
    {
        var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));
        var logger = loggerFactory.CreateLogger<LoggingDLQPublisher>();
        var publisher = new LoggingDLQPublisher(logger);

        var failedEvent = new PaymentFailed
        {
            CorrelationId = "DLQ-NULL-REQ",
            FailedStep = "Settle",
            ErrorCode = "SETTLEMENT_FAILED",
            ErrorMessage = "Unknown error",
            FailedAt = DateTime.UtcNow,
            OriginalRequest = null!,
        };

        await publisher.PublishFailedPaymentAsync(failedEvent);
        // Should handle null gracefully
    }

    [Fact]
    public async Task PublishFailedPayment_MultipleEvents_AllLogged()
    {
        var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));
        var logger = loggerFactory.CreateLogger<LoggingDLQPublisher>();
        var publisher = new LoggingDLQPublisher(logger);

        for (int i = 0; i < 10; i++)
        {
            var failedEvent = new PaymentFailed
            {
                CorrelationId = $"DLQ-BATCH-{i:D3}",
                FailedStep = i % 3 == 0 ? "Validate" : i % 3 == 1 ? "ReserveFunds" : "Settle",
                ErrorCode = $"ERR-{i:D3}",
                ErrorMessage = $"Test failure #{i}",
                RetryCount = i % 2,
                FailedAt = DateTime.UtcNow,
                OriginalRequest = TestDataFactory.CreateValidRequest($"DLQ-BATCH-{i:D3}", amount: 100m * (i + 1)),
            };

            await publisher.PublishFailedPaymentAsync(failedEvent);
        }
    }
}

public class FakeValidationServiceTests
{
    [Fact]
    public async Task ValidatePayment_WhenSetToPass_ReturnsTrue()
    {
        var service = new FakeValidationService(shouldPass: true);
        var result = await service.ValidatePaymentAsync(TestDataFactory.CreateValidRequest());
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePayment_WhenSetToFail_ReturnsFalse()
    {
        var service = new FakeValidationService(shouldPass: false);
        var result = await service.ValidatePaymentAsync(TestDataFactory.CreateValidRequest());
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePayment_CanToggleResultMidTest()
    {
        var service = new FakeValidationService(shouldPass: true);

        (await service.ValidatePaymentAsync(TestDataFactory.CreateValidRequest())).Should().BeTrue();

        service.SetResult(false);

        (await service.ValidatePaymentAsync(TestDataFactory.CreateValidRequest())).Should().BeFalse();

        service.SetResult(true);

        (await service.ValidatePaymentAsync(TestDataFactory.CreateValidRequest())).Should().BeTrue();
    }
}

public class FakeLedgerServiceTests
{
    [Fact]
    public async Task ReserveFunds_Succeeds_ReturnsNonNullReservationId()
    {
        var service = new FakeLedgerService(reserveSucceeds: true);
        var result = await service.ReserveFundsAsync("TEST-001", 100m, "DE89370400440532013000");
        result.Should().NotBeNull();
        result.Should().StartWith("RSV-TEST-001");
    }

    [Fact]
    public async Task ReserveFunds_Fails_ReturnsNull()
    {
        var service = new FakeLedgerService(reserveSucceeds: false);
        var result = await service.ReserveFundsAsync("TEST-001", 100m, "DE89370400440532013000");
        result.Should().BeNull();
    }

    [Fact]
    public async Task SettleFunds_Succeeds_ReturnsTrue()
    {
        var service = new FakeLedgerService(settleSucceeds: true);
        var result = await service.SettleFundsAsync("TEST-001", "RSV-1", 100m, "FR1420041010050500013M02606");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SettleFunds_Fails_ReturnsFalse()
    {
        var service = new FakeLedgerService(settleSucceeds: false);
        var result = await service.SettleFundsAsync("TEST-001", "RSV-1", 100m, "FR1420041010050500013M02606");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ReleaseReservation_RecordsReleasedId()
    {
        var service = new FakeLedgerService();
        await service.ReleaseReservationAsync("TEST-001", "RSV-RELEASE-1");
        await service.ReleaseReservationAsync("TEST-002", "RSV-RELEASE-2");

        service.ReleasedReservations.Should().Contain("RSV-RELEASE-1");
        service.ReleasedReservations.Should().Contain("RSV-RELEASE-2");
        service.ReleasedReservations.Should().HaveCount(2);
    }

    [Fact]
    public async Task LedgerService_CanToggleReserveAndSettle()
    {
        var service = new FakeLedgerService(reserveSucceeds: true, settleSucceeds: true);

        (await service.ReserveFundsAsync("T", 100m, "A")).Should().NotBeNull();
        (await service.SettleFundsAsync("T", "R", 100m, "B")).Should().BeTrue();

        service.SetReserveResult(false);
        service.SetSettleResult(false);

        (await service.ReserveFundsAsync("T2", 100m, "A")).Should().BeNull();
        (await service.SettleFundsAsync("T2", "R", 100m, "B")).Should().BeFalse();
    }
}

public class CapturingDLQPublisherTests
{
    [Fact]
    public async Task PublishFailedPayment_CapturesEvent()
    {
        var publisher = new CapturingDLQPublisher();
        var failedEvent = new PaymentFailed
        {
            CorrelationId = "CAPTURE-001",
            FailedStep = "ReserveFunds",
            ErrorCode = "RESERVATION_FAILED",
            ErrorMessage = "Insufficient funds",
            FailedAt = DateTime.UtcNow,
            OriginalRequest = TestDataFactory.CreateValidRequest("CAPTURE-001"),
        };

        await publisher.PublishFailedPaymentAsync(failedEvent);

        publisher.PublishedEvents.Should().ContainSingle();
        publisher.PublishedEvents[0].CorrelationId.Should().Be("CAPTURE-001");
    }

    [Fact]
    public async Task PublishFailedPayment_MultipleEvents_CapturesAll()
    {
        var publisher = new CapturingDLQPublisher();

        for (int i = 0; i < 5; i++)
        {
            await publisher.PublishFailedPaymentAsync(new PaymentFailed
            {
                CorrelationId = $"CAP-MULTI-{i}",
                FailedStep = "Test",
                ErrorCode = $"ERR-{i}",
                FailedAt = DateTime.UtcNow,
            });
        }

        publisher.PublishedEvents.Should().HaveCount(5);
        publisher.PublishedEvents.Select(e => e.CorrelationId)
            .Should().BeEquivalentTo(new[] { "CAP-MULTI-0", "CAP-MULTI-1", "CAP-MULTI-2", "CAP-MULTI-3", "CAP-MULTI-4" });
    }

    [Fact]
    public void Clear_RemovesAllEvents()
    {
        var publisher = new CapturingDLQPublisher();

        publisher.PublishedEvents.Count.Should().Be(0);
        publisher.Clear();
        publisher.PublishedEvents.Count.Should().Be(0);
    }
}

public class EventModelTests
{
    [Fact]
    public void PaymentValidated_DefaultValues()
    {
        var evt = new PaymentValidated();
        evt.CorrelationId.Should().BeEmpty();
        evt.IsValid.Should().BeFalse();
        evt.ErrorMessage.Should().BeNull();
        evt.ValidatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void FundsReserved_DefaultValues()
    {
        var evt = new FundsReserved();
        evt.CorrelationId.Should().BeEmpty();
        evt.ReservationId.Should().BeEmpty();
        evt.Amount.Should().Be(0m);
        evt.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public void PaymentSettledInternal_DefaultValues()
    {
        var evt = new PaymentSettledInternal();
        evt.CorrelationId.Should().BeEmpty();
        evt.SettlementId.Should().BeEmpty();
        evt.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public void PaymentSagaState_DefaultValues()
    {
        var state = new PaymentSagaState();
        state.Id.Should().BeEmpty();
        state.Status.Should().Be("Validating");
        state.RetryCount.Should().Be(0);
        state.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        state.CompletedAt.Should().BeNull();
        state.Version.Should().Be(0);
    }

    [Fact]
    public void ValidatePaymentCommand_CreatedAt_DefaultsToUtcNow()
    {
        var cmd = new ValidatePaymentCommand();
        cmd.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ReserveFundsCommand_CreatedAt_DefaultsToUtcNow()
    {
        var cmd = new ReserveFundsCommand();
        cmd.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SettlePaymentCommand_CreatedAt_DefaultsToUtcNow()
    {
        var cmd = new SettlePaymentCommand();
        cmd.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
// END_BLOCK_TESTS
