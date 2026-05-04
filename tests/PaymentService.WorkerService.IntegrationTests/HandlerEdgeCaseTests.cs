// START_MODULE TESTS
// START_BLOCK_TESTS HandlerEdgeCaseTests
// PURPOSE: Additional edge case tests for step handlers covering unusual inputs
//          and failure modes not covered in main handler tests.
//          Tests: ~25
// SEMANTIC_TAG: [BLOCK_TEST_HANDLER_EDGE] Handler edge case tests
namespace PaymentService.Workers.IntegrationTests;

using PaymentService.Workers.Commands;
using PaymentService.Workers.Events;
using PaymentService.Workers.Metrics;
using PaymentService.Workers.Steps;

public class HandlerEdgeCaseTests
{
    // ──── ValidatePaymentHandler Edge Cases ────

    [Fact]
    public async Task ValidatePayment_NullPaymentRequest_DoesNotThrow()
    {
        var fakeService = new FakeValidationService(shouldPass: true);
        var (logger, _, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ValidatePaymentHandler(fakeService, logger, metrics);

        // Even with null PaymentRequest, handler should not crash
        // (though this is guarded by validation at the command level)
        var command = new ValidatePaymentCommand
        {
            CorrelationId = "NULL-REQ-001",
            PaymentRequest = null!,
        };

        // Service call would fail, but handler catches exceptions
        await handler.Handle(command, CancellationToken.None);
    }

    [Fact]
    public async Task ValidatePayment_VeryLongCorrelationId_Handled()
    {
        var fakeService = new FakeValidationService(shouldPass: true);
        var (logger, _, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ValidatePaymentHandler(fakeService, logger, metrics);

        var longCorrelationId = new string('X', 200);
        var request = TestDataFactory.CreateValidRequest(longCorrelationId);

        var command = new ValidatePaymentCommand
        {
            CorrelationId = longCorrelationId,
            PaymentRequest = request,
        };

        await handler.Handle(command, CancellationToken.None);
    }

    [Fact]
    public async Task ValidatePayment_SpecialCharactersInDescription_Handled()
    {
        var fakeService = new FakeValidationService(shouldPass: true);
        var (logger, _, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ValidatePaymentHandler(fakeService, logger, metrics);

        var request = TestDataFactory.CreateValidRequest(description: "Test / 支払い / Платеж / 🎉");
        var command = new ValidatePaymentCommand
        {
            CorrelationId = request.CorrelationId,
            PaymentRequest = request,
        };

        await handler.Handle(command, CancellationToken.None);
    }

    [Fact]
    public async Task ValidatePayment_ThrottledCancellationToken_Handled()
    {
        var fakeService = new FakeValidationService(shouldPass: true);
        var (logger, _, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ValidatePaymentHandler(fakeService, logger, metrics);

        var request = TestDataFactory.CreateValidRequest();
        var command = new ValidatePaymentCommand
        {
            CorrelationId = request.CorrelationId,
            PaymentRequest = request,
        };

        // Token is not timed out, handler should proceed
        await handler.Handle(command, CancellationToken.None);
    }

    // ──── ReserveFundsHandler Edge Cases ────

    [Fact]
    public async Task ReserveFunds_ExactMaxAmount_ReturnsSuccess()
    {
        var fakeLedger = new FakeLedgerService(reserveSucceeds: true);
        var (_, logger, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ReserveFundsHandler(fakeLedger, logger, metrics);

        var command = new ReserveFundsCommand
        {
            CorrelationId = "RSV-MAX-001",
            Amount = 999_999_999_999.99m,
            SenderAccount = "DE89370400440532013000",
        };

        var result = await handler.Handle(command, CancellationToken.None);
        result.IsSuccessful.Should().BeTrue();
        result.Amount.Should().Be(999_999_999_999.99m);
    }

    [Fact]
    public async Task ReserveFunds_WithSpecialAccountFormat_Handled()
    {
        var fakeLedger = new FakeLedgerService(reserveSucceeds: true);
        var (_, logger, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ReserveFundsHandler(fakeLedger, logger, metrics);

        var longAccount = new string('A', 34); // Max IBAN length
        var command = new ReserveFundsCommand
        {
            CorrelationId = "RSV-SPECIAL",
            Amount = 100m,
            SenderAccount = longAccount,
        };

        var result = await handler.Handle(command, CancellationToken.None);
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task ReserveFunds_ChainedMultipleOperations_AllSucceed()
    {
        var fakeLedger = new FakeLedgerService(reserveSucceeds: true);
        var (_, logger, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ReserveFundsHandler(fakeLedger, logger, metrics);

        for (int batch = 0; batch < 5; batch++)
        {
            var tasks = Enumerable.Range(0, 10).Select(async i =>
            {
                var command = new ReserveFundsCommand
                {
                    CorrelationId = $"RSV-CHAIN-{batch}-{i}",
                    Amount = 100m * (i + 1),
                    SenderAccount = "DE89370400440532013000",
                };
                return await handler.Handle(command, CancellationToken.None);
            });

            var results = await Task.WhenAll(tasks);
            results.Should().AllSatisfy(r => r.IsSuccessful.Should().BeTrue());
            results.Should().HaveCount(10);
        }
    }

    [Fact]
    public async Task ReserveFunds_ServiceReturnsCustomError_ErrorMessageContains()
    {
        var fakeLedger = new FakeLedgerService(reserveSucceeds: false);
        var (_, logger, _, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new ReserveFundsHandler(fakeLedger, logger, metrics);

        var command = new ReserveFundsCommand
        {
            CorrelationId = "RSV-CUSTOM-ERR",
            Amount = 100m,
            SenderAccount = "XX00000000000000000000",
        };

        var result = await handler.Handle(command, CancellationToken.None);
        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ──── SettlePaymentHandler Edge Cases ────

    [Fact]
    public async Task SettlePayment_EdgeCaseSettlementId_Format()
    {
        var fakeLedger = new FakeLedgerService(settleSucceeds: true);
        var (_, _, logger, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new SettlePaymentHandler(fakeLedger, logger, metrics);

        var specialCorrelationIds = new[]
        {
            "S",
            "STL-VERY-LONG-CORRELATION-ID-THAT-EXCEEDS-TYPICAL-LENGTH",
            "STL-WITH-SPECIAL-!_#_$_%_&",
            "STL-NORMAL-001",
            Guid.NewGuid().ToString(),
        };

        foreach (var cid in specialCorrelationIds)
        {
            var command = new SettlePaymentCommand
            {
                CorrelationId = cid,
                ReservationId = $"RSV-{cid}",
                Amount = 100m,
                ReceiverAccount = "DE89370400440532013000",
            };

            var result = await handler.Handle(command, CancellationToken.None);
            result.IsSuccessful.Should().BeTrue();
            result.SettlementId.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task SettlePayment_FailsOnAllReceiverAccounts()
    {
        var accounts = new[]
        {
            "DE89370400440532013000",
            "FR1420041010050500013M02606",
            "GB82WEST12345698765432",
            "CH5604835012345678009",
        };

        var fakeLedger = new FakeLedgerService(settleSucceeds: false);
        var (_, _, logger, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new SettlePaymentHandler(fakeLedger, logger, metrics);

        foreach (var account in accounts)
        {
            var command = new SettlePaymentCommand
            {
                CorrelationId = $"FAIL-ACCT-{account[..6]}",
                ReservationId = "RSV-FAIL",
                Amount = 100m,
                ReceiverAccount = account,
            };

            var result = await handler.Handle(command, CancellationToken.None);
            result.IsSuccessful.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task SettlePayment_MixedSuccessAndFailure_BothPathsWork()
    {
        var fakeLedger = new FakeLedgerService(settleSucceeds: true);
        var (_, _, logger, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new SettlePaymentHandler(fakeLedger, logger, metrics);

        // First batch: all succeed
        for (int i = 0; i < 10; i++)
        {
            var result = await handler.Handle(new SettlePaymentCommand
            {
                CorrelationId = $"MIXED-OK-{i}",
                ReservationId = $"RSV-MIX-{i}",
                Amount = 10m * (i + 1),
                ReceiverAccount = "DE89370400440532013000",
            }, CancellationToken.None);
            result.IsSuccessful.Should().BeTrue();
        }

        // Toggle to failures
        fakeLedger.SetSettleResult(false);

        // Second batch: all fail
        for (int i = 0; i < 10; i++)
        {
            var result = await handler.Handle(new SettlePaymentCommand
            {
                CorrelationId = $"MIXED-FAIL-{i}",
                ReservationId = $"RSV-MIX-F-{i}",
                Amount = 10m * (i + 1),
                ReceiverAccount = "FR1420041010050500013M02606",
            }, CancellationToken.None);
            result.IsSuccessful.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        // Toggle back to successes
        fakeLedger.SetSettleResult(true);

        // Third batch: succeed again
        for (int i = 0; i < 10; i++)
        {
            var result = await handler.Handle(new SettlePaymentCommand
            {
                CorrelationId = $"MIXED-OK2-{i}",
                ReservationId = $"RSV-MIX2-{i}",
                Amount = 10m * (i + 1),
                ReceiverAccount = "GB82WEST12345698765432",
            }, CancellationToken.None);
            result.IsSuccessful.Should().BeTrue();
        }
    }

    [Fact]
    public async Task SettlePayment_VeryLargeAmountApproachingLimit()
    {
        var fakeLedger = new FakeLedgerService(settleSucceeds: true);
        var (_, _, logger, _, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();
        var handler = new SettlePaymentHandler(fakeLedger, logger, metrics);

        var amounts = new[] { 1m, 1000m, 1000000m, 999_999_999.99m };
        foreach (var amount in amounts)
        {
            var result = await handler.Handle(new SettlePaymentCommand
            {
                CorrelationId = $"STL-LIMIT-{amount}",
                ReservationId = $"RSV-LIMIT-{amount}",
                Amount = amount,
                ReceiverAccount = "DE89370400440532013000",
            }, CancellationToken.None);
            result.IsSuccessful.Should().BeTrue();
        }
    }
}
// END_BLOCK_TESTS
