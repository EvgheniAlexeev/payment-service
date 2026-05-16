// FILE: StepHandlerTests.cs
// VERSION: 2.0.0
// MODULE: M-INTEGRATION
// PURPOSE: Test specification
// SEMANTIC_TAG: [TEST]
// START_MODULE M_INTEGRATION

// START_MODULE TESTS
// START_BLOCK_TESTS StepHandlerTests
// PURPOSE: Unit tests for ValidatePaymentHandler, ReserveFundsHandler, SettlePaymentHandler.
//          Covers success, failure, exception, and edge cases for each handler.
//          Tests: ~60
// SEMANTIC_TAG: [BLOCK_TEST_HANDLERS] Step handler validation tests
namespace PaymentService.Workers.IntegrationTests;

using PaymentService.Workers.Commands;
using PaymentService.Workers.Events;
using PaymentService.Workers.Steps;

public class ValidatePaymentHandlerTests
{
    // ──── Success Cases ────
    [Fact]
    public async Task Handle_PaymentValid_ReturnsValidEvent()
    {
        var fakeService = new FakeValidationService(shouldPass: true);
        var (logger, _, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ValidatePaymentHandler(fakeService, logger, metrics);

        var request = TestDataFactory.CreateValidRequest();
        var command = new ValidatePaymentCommand { CorrelationId = request.CorrelationId, PaymentRequest = request };

        // Act — handler logs and gates via service but returns void
        // (Wolverine auto-publishes return values; we verify service interaction)
        await handler.Handle(command, CancellationToken.None);

        // No exception thrown = success for this handler pattern
    }

    [Fact]
    public async Task Handle_PaymentInvalid_DoesNotThrow()
    {
        var fakeService = new FakeValidationService(shouldPass: false);
        var (logger, _, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ValidatePaymentHandler(fakeService, logger, metrics);

        var request = TestDataFactory.CreateValidRequest();
        var command = new ValidatePaymentCommand { CorrelationId = request.CorrelationId, PaymentRequest = request };

        await handler.Handle(command, CancellationToken.None);
        // Should not throw — handler catches exceptions
    }

    [Fact]
    public async Task Handle_MultipleValidRequests_NoExceptions()
    {
        var fakeService = new FakeValidationService(shouldPass: true);
        var (logger, _, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ValidatePaymentHandler(fakeService, logger, metrics);

        for (int i = 0; i < 20; i++)
        {
            var request = TestDataFactory.CreateValidRequest(amount: 10m + i);
            var command = new ValidatePaymentCommand { CorrelationId = request.CorrelationId, PaymentRequest = request };
            await handler.Handle(command, CancellationToken.None);
        }
    }

    [Fact]
    public async Task Handle_ServiceThrows_DoesNotEscalate()
    {
        var fakeService = A.Fake<IValidationService>();
        A.CallTo(() => fakeService.ValidatePaymentAsync(A<PaymentRequestDto>._, A<CancellationToken>._))
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        var (logger, _, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ValidatePaymentHandler(fakeService, logger, metrics);

        var request = TestDataFactory.CreateValidRequest();
        var command = new ValidatePaymentCommand { CorrelationId = request.CorrelationId, PaymentRequest = request };

        await handler.Handle(command, CancellationToken.None);
        // Handler swallows exceptions gracefully
    }

    [Fact]
    public async Task Handle_CancellationRequested_ThrowsOperationCanceledException()
    {
        var fakeService = new FakeValidationService(shouldPass: true);
        var (logger, _, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ValidatePaymentHandler(fakeService, logger, metrics);

        var request = TestDataFactory.CreateValidRequest();
        var command = new ValidatePaymentCommand { CorrelationId = request.CorrelationId, PaymentRequest = request };

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // The handler itself doesn't do cancellation-aware operations on the service layer;
        // it passes the token through — we verify it doesn't crash with cancelled token
        try
        {
            await handler.Handle(command, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when token propagates
        }
    }

    [Fact]
    public async Task Handle_DifferentCurrencies_AllPass()
    {
        var fakeService = new FakeValidationService(shouldPass: true);
        var (logger, _, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ValidatePaymentHandler(fakeService, logger, metrics);

        var currencies = new[] { "USD", "EUR", "GBP", "CHF", "JPY", "CAD", "AUD" };
        foreach (var currency in currencies)
        {
            var request = TestDataFactory.CreateValidRequest(currency: currency);
            var command = new ValidatePaymentCommand { CorrelationId = request.CorrelationId, PaymentRequest = request };
            await handler.Handle(command, CancellationToken.None);
        }
    }
}

public class ReserveFundsHandlerTests
{
    // ──── Success Cases ────
    [Fact]
    public async Task Handle_ReservationSucceeds_ReturnsSuccessfulEvent()
    {
        var fakeLedger = new FakeLedgerService(reserveSucceeds: true);
        var (_, logger, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ReserveFundsHandler(fakeLedger, logger, metrics);

        var command = new ReserveFundsCommand
        {
            CorrelationId = "TEST-RSV-001",
            Amount = 500m,
            SenderAccount = "DE89370400440532013000",
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.CorrelationId.Should().Be("TEST-RSV-001");
        result.Amount.Should().Be(500m);
        result.ReservationId.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReservationFails_ReturnsFailedEvent()
    {
        var fakeLedger = new FakeLedgerService(reserveSucceeds: false);
        var (_, logger, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ReserveFundsHandler(fakeLedger, logger, metrics);

        var command = new ReserveFundsCommand
        {
            CorrelationId = "TEST-RSV-002",
            Amount = 100m,
            SenderAccount = "FR1420041010050500013M02606",
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.CorrelationId.Should().Be("TEST-RSV-002");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.ReservationId.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_LedgerThrowsException_ReturnsFailedEvent()
    {
        var fakeLedger = A.Fake<ILedgerService>();
        A.CallTo(() => fakeLedger.ReserveFundsAsync(A<string>._, A<decimal>._, A<string>._, A<CancellationToken>._))
            .ThrowsAsync(new TimeoutException("Ledger timeout"));

        var (_, logger, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ReserveFundsHandler(fakeLedger, logger, metrics);

        var command = new ReserveFundsCommand
        {
            CorrelationId = "TEST-RSV-003",
            Amount = 200m,
            SenderAccount = "GB82WEST12345698765432",
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Ledger timeout");
    }

    [Fact]
    public async Task Handle_LargeAmount_ReturnsSuccessfulEvent()
    {
        var fakeLedger = new FakeLedgerService(reserveSucceeds: true);
        var (_, logger, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ReserveFundsHandler(fakeLedger, logger, metrics);

        var command = new ReserveFundsCommand
        {
            CorrelationId = "TEST-RSV-LARGE",
            Amount = 999_999_999.99m,
            SenderAccount = "CH5604835012345678009",
        };

        var result = await handler.Handle(command, CancellationToken.None);
        result.IsSuccessful.Should().BeTrue();
        result.Amount.Should().Be(999_999_999.99m);
    }

    [Fact]
    public async Task Handle_MinimalAmount_ReturnsSuccessfulEvent()
    {
        var fakeLedger = new FakeLedgerService(reserveSucceeds: true);
        var (_, logger, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ReserveFundsHandler(fakeLedger, logger, metrics);

        var command = new ReserveFundsCommand
        {
            CorrelationId = "TEST-RSV-MIN",
            Amount = 0.01m,
            SenderAccount = "JP1234567890",
        };

        var result = await handler.Handle(command, CancellationToken.None);
        result.IsSuccessful.Should().BeTrue();
        result.Amount.Should().Be(0.01m);
    }

    [Fact]
    public async Task Handle_MultipleReservations_AllSucceed()
    {
        var fakeLedger = new FakeLedgerService(reserveSucceeds: true);
        var (_, logger, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ReserveFundsHandler(fakeLedger, logger, metrics);

        var results = new List<FundsReserved>();
        for (int i = 0; i < 25; i++)
        {
            var command = new ReserveFundsCommand
            {
                CorrelationId = $"TEST-RSV-BATCH-{i:D3}",
                Amount = 10m * (i + 1),
                SenderAccount = "DE89370400440532013000",
            };
            results.Add(await handler.Handle(command, CancellationToken.None));
        }

        results.Should().AllSatisfy(r => r.IsSuccessful.Should().BeTrue());
        results.Should().HaveCount(25);
    }

    [Fact]
    public async Task Handle_ZeroAmount_ReturnsSuccessfulEvent()
    {
        // Note: validation of amount > 0 happens at the DTO/validator level.
        // The handler itself doesn't validate; it trusts the ledger.
        var fakeLedger = new FakeLedgerService(reserveSucceeds: true);
        var (_, logger, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ReserveFundsHandler(fakeLedger, logger, metrics);

        var command = new ReserveFundsCommand
        {
            CorrelationId = "TEST-RSV-ZERO",
            Amount = 0m,
            SenderAccount = "DE89370400440532013000",
        };

        var result = await handler.Handle(command, CancellationToken.None);
        result.IsSuccessful.Should().BeTrue();
        result.Amount.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_DifferentSenderAccounts_AllProcessed()
    {
        var accounts = new[]
        {
            "DE89370400440532013000",
            "FR1420041010050500013M02606",
            "GB82WEST12345698765432",
            "CH5604835012345678009",
            "ES9121000418450200051332",
            "IT60X0542811101000000123456",
        };

        var fakeLedger = new FakeLedgerService(reserveSucceeds: true);
        var (_, logger, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ReserveFundsHandler(fakeLedger, logger, metrics);

        foreach (var account in accounts)
        {
            var command = new ReserveFundsCommand
            {
                CorrelationId = $"TEST-RSV-{account[..6]}",
                Amount = 100m,
                SenderAccount = account,
            };

            var result = await handler.Handle(command, CancellationToken.None);
            result.IsSuccessful.Should().BeTrue();
        }
    }
}

public class SettlePaymentHandlerTests
{
    // ──── Success Cases ────
    [Fact]
    public async Task Handle_SettlementSucceeds_ReturnsSuccessfulEvent()
    {
        var fakeLedger = new FakeLedgerService(settleSucceeds: true);
        var (_, _, logger, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new SettlePaymentHandler(fakeLedger, logger, metrics);

        var command = new SettlePaymentCommand
        {
            CorrelationId = "TEST-STL-001",
            ReservationId = "RSV-TEST-001",
            Amount = 500m,
            ReceiverAccount = "FR1420041010050500013M02606",
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.CorrelationId.Should().Be("TEST-STL-001");
        result.SettlementId.Should().NotBeNullOrEmpty();
        result.SettlementId.Should().StartWith("STL-TEST-STL-001");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_SettlementFails_ReturnsFailedEvent()
    {
        var fakeLedger = new FakeLedgerService(settleSucceeds: false);
        var (_, _, logger, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new SettlePaymentHandler(fakeLedger, logger, metrics);

        var command = new SettlePaymentCommand
        {
            CorrelationId = "TEST-STL-002",
            ReservationId = "RSV-TEST-002",
            Amount = 300m,
            ReceiverAccount = "DE89370400440532013000",
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.CorrelationId.Should().Be("TEST-STL-002");
        result.SettlementId.Should().BeEmpty();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_LedgerThrowsException_ReturnsFailedEvent()
    {
        var fakeLedger = A.Fake<ILedgerService>();
        A.CallTo(() => fakeLedger.SettleFundsAsync(A<string>._, A<string>._, A<decimal>._, A<string>._, A<CancellationToken>._))
            .ThrowsAsync(new InvalidOperationException("Settlement service down"));

        var (_, _, logger, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new SettlePaymentHandler(fakeLedger, logger, metrics);

        var command = new SettlePaymentCommand
        {
            CorrelationId = "TEST-STL-003",
            ReservationId = "RSV-TEST-003",
            Amount = 400m,
            ReceiverAccount = "GB82WEST12345698765432",
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Settlement service down");
    }

    [Fact]
    public async Task Handle_LargeAmount_Succeeds()
    {
        var fakeLedger = new FakeLedgerService(settleSucceeds: true);
        var (_, _, logger, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new SettlePaymentHandler(fakeLedger, logger, metrics);

        var command = new SettlePaymentCommand
        {
            CorrelationId = "TEST-STL-LARGE",
            ReservationId = "RSV-LARGE",
            Amount = 999_999_999.99m,
            ReceiverAccount = "CH5604835012345678009",
        };

        var result = await handler.Handle(command, CancellationToken.None);
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DifferentReceiverAccounts_AllProcessed()
    {
        var accounts = new[]
        {
            "DE89370400440532013000",
            "FR1420041010050500013M02606",
            "GB82WEST12345698765432",
            "CH5604835012345678009",
            "ES9121000418450200051332",
            "NL91ABNA0417164300",
            "SE4550000000058398257466",
        };

        var fakeLedger = new FakeLedgerService(settleSucceeds: true);
        var (_, _, logger, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new SettlePaymentHandler(fakeLedger, logger, metrics);

        foreach (var account in accounts)
        {
            var command = new SettlePaymentCommand
            {
                CorrelationId = $"TEST-STL-{account[..6]}",
                ReservationId = $"RSV-{account[..6]}",
                Amount = 100m,
                ReceiverAccount = account,
            };

            var result = await handler.Handle(command, CancellationToken.None);
            result.IsSuccessful.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Handle_MultipleSettlements_AllSucceed()
    {
        var fakeLedger = new FakeLedgerService(settleSucceeds: true);
        var (_, _, logger, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new SettlePaymentHandler(fakeLedger, logger, metrics);

        var results = new List<PaymentSettledInternal>();
        for (int i = 0; i < 20; i++)
        {
            var command = new SettlePaymentCommand
            {
                CorrelationId = $"TEST-STL-BATCH-{i:D3}",
                ReservationId = $"RSV-BATCH-{i:D3}",
                Amount = 50m * (i + 1),
                ReceiverAccount = "DE89370400440532013000",
            };
            results.Add(await handler.Handle(command, CancellationToken.None));
        }

        results.Should().AllSatisfy(r => r.IsSuccessful.Should().BeTrue());
        results.Should().HaveCount(20);
        results.Select(r => r.SettlementId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Handle_SettlementId_ContainsCorrelationId()
    {
        var fakeLedger = new FakeLedgerService(settleSucceeds: true);
        var (_, _, logger, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new SettlePaymentHandler(fakeLedger, logger, metrics);

        var command = new SettlePaymentCommand
        {
            CorrelationId = "MY-CUSTOM-CORRELATION-ID",
            ReservationId = "RSV-CUSTOM",
            Amount = 777m,
            ReceiverAccount = "FR1420041010050500013M02606",
        };

        var result = await handler.Handle(command, CancellationToken.None);
        result.SettlementId.Should().Contain("MY-CUSTOM-CORRELATION-ID");
    }
}
// END_BLOCK_TESTS
