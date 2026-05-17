// FILE: SharedEventTests.cs
// VERSION: 2.0.0
// MODULE: M-INTEGRATION
// PURPOSE: Test specification
// SEMANTIC_TAG: [TEST]
// START_MODULE M_INTEGRATION

// START_MODULE TESTS
// START_BLOCK_TESTS SharedEventTests
// PURPOSE: Tests for shared events (PaymentEnriched, PaymentSettled, PaymentFailed) 
//          and shared models used across the sagas.
//          Tests: ~15
// SEMANTIC_TAG: [BLOCK_TEST_SHARED] Shared event and model tests
namespace PaymentService.Workers.IntegrationTests;

using PaymentService.Shared.Commands;
using PaymentService.Shared.Dtos;
using PaymentService.Shared.Events;

public class SharedEventTests
{
    [Fact]
    public void PaymentFailed_Creation_AllFieldsSet()
    {
        var request = TestDataFactory.CreateValidRequest("SHARED-FAIL-001");
        var failed = new PaymentFailed
        {
            CorrelationId = "SHARED-FAIL-001",
            OriginalRequest = request,
            FailedStep = "Validate",
            ErrorMessage = "Compliance violation",
            ErrorCode = "COMPLIANCE_VIOLATION",
            RetryCount = 2,
            FailedAt = DateTime.UtcNow,
        };

        failed.CorrelationId.Should().Be("SHARED-FAIL-001");
        failed.OriginalRequest.Should().BeEquivalentTo(request);
        failed.FailedStep.Should().Be("Validate");
        failed.ErrorMessage.Should().Be("Compliance violation");
        failed.ErrorCode.Should().Be("COMPLIANCE_VIOLATION");
        failed.RetryCount.Should().Be(2);
        failed.FailedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void PaymentSettled_Creation_CorrectValues()
    {
        var settled = new PaymentSettled
        {
            CorrelationId = "SHARED-STL-001",
            SettlementId = "STL-ABC123",
            SettledAt = DateTime.UtcNow,
            Status = "Settled",
        };

        settled.CorrelationId.Should().Be("SHARED-STL-001");
        settled.SettlementId.Should().Be("STL-ABC123");
        settled.Status.Should().Be("Settled");
    }

    [Fact]
    public void PaymentEnriched_Creation_CorrectValues()
    {
        var enriched = new PaymentEnriched
        {
            CorrelationId = "SHARED-ENR-001",
            EnrichmentData = "Counterparty: ACME Corp, Risk: Low",
            EnrichedAt = DateTime.UtcNow,
        };

        enriched.CorrelationId.Should().Be("SHARED-ENR-001");
        enriched.EnrichmentData.Should().Contain("ACME Corp");
    }

    [Theory]
    [InlineData("USD", "EUR", "GBP", "JPY")]
    [InlineData("CHF", "CAD", "AUD", "NZD")]
    public void PaymentRequestDto_MultiCurrency_Creation(params string[] currencies)
    {
        foreach (var currency in currencies)
        {
            var request = TestDataFactory.CreateValidRequest(currency: currency);
            request.Currency.Should().Be(currency);
        }
    }

    [Theory]
    [InlineData("DE89370400440532013000")]
    [InlineData("FR1420041010050500013M02606")]
    [InlineData("GB82WEST12345698765432")]
    [InlineData("CH5604835012345678009")]
    [InlineData("ES9121000418450200051332")]
    [InlineData("NL91ABNA0417164300")]
    [InlineData("SE4550000000058398257466")]
    [InlineData("DK5000400440116243")]
    [InlineData("PL61109010140000071219812874")]
    [InlineData("AT611904300234573201")]
    public void PaymentRequestDto_InternationalAccounts_SenderAccount(string account)
    {
        var request = TestDataFactory.CreateValidRequest(senderAccount: account);
        request.SenderAccount.Should().Be(account);
    }

    [Theory]
    [InlineData("DE89370400440532013000")]
    [InlineData("FR1420041010050500013M02606")]
    [InlineData("GB82WEST12345698765432")]
    [InlineData("CH5604835012345678009")]
    [InlineData("ES9121000418450200051332")]
    [InlineData("NL91ABNA0417164300")]
    [InlineData("SE4550000000058398257466")]
    [InlineData("DK5000400440116243")]
    [InlineData("PL61109010140000071219812874")]
    [InlineData("AT611904300234573201")]
    public void PaymentRequestDto_InternationalAccounts_ReceiverAccount(string account)
    {
        var request = TestDataFactory.CreateValidRequest(receiverAccount: account);
        request.ReceiverAccount.Should().Be(account);
    }
}

public class PaymentRequestDtoValidationTests
{
    [Fact]
    public void CreateValidRequest_Default_IsValid()
    {
        var request = TestDataFactory.CreateValidRequest();
        request.CorrelationId.Should().NotBeEmpty();
        request.SenderAccount.Should().NotBeEmpty();
        request.ReceiverAccount.Should().NotBeEmpty();
        request.Amount.Should().BeGreaterThan(0);
        request.Currency.Should().Be("USD");
        request.ValueDate.Should().BeOnOrAfter(DateTime.UtcNow.Date);
    }

    [Fact]
    public void CreateZeroAmountRequest_HasZeroAmount()
    {
        var request = TestDataFactory.CreateZeroAmountRequest();
        request.Amount.Should().Be(0m);
    }

    [Fact]
    public void CreateNegativeAmountRequest_HasNegativeAmount()
    {
        var request = TestDataFactory.CreateNegativeAmountRequest();
        request.Amount.Should().Be(-50m);
    }

    [Fact]
    public void CreateInvalidCurrencyRequest_HasInvalidCurrency()
    {
        var request = TestDataFactory.CreateInvalidCurrencyRequest();
        request.Currency.Should().Be("XXX");
    }

    [Fact]
    public void CreateEmptySenderRequest_HasEmptySender()
    {
        var request = TestDataFactory.CreateEmptySenderRequest();
        request.SenderAccount.Should().BeEmpty();
    }

    [Fact]
    public void CreateEmptyReceiverRequest_HasEmptyReceiver()
    {
        var request = TestDataFactory.CreateEmptyReceiverRequest();
        request.ReceiverAccount.Should().BeEmpty();
    }

    [Fact]
    public void CreatePastValueDateRequest_HasPastDate()
    {
        var request = TestDataFactory.CreatePastValueDateRequest();
        request.ValueDate.Should().BeBefore(DateTime.UtcNow.Date);
    }

    [Fact]
    public void CreateBatch_ReturnsCorrectCount()
    {
        var batch = TestDataFactory.CreateBatch(25);
        batch.Should().HaveCount(25);
        batch.Should().AllSatisfy(r => r.CorrelationId.Should().NotBeEmpty());
    }

    [Fact]
    public void CreateMultiCurrencyRequests_CoversAllCurrencies()
    {
        var requests = TestDataFactory.CreateMultiCurrencyRequests();
        requests.Should().HaveCount(10);
        requests.Select(r => r.Currency).Should()
            .BeEquivalentTo(new[] { "USD", "EUR", "GBP", "CHF", "JPY", "CAD", "AUD", "NZD", "SEK", "NOK" });
    }

    [Fact]
    public void CreateBoundaryAmounts_CorrectValidFlags()
    {
        var boundaries = TestDataFactory.CreateBoundaryAmounts();
        boundaries.Where(b => b.ShouldBeValid).Should().HaveCount(5);
        boundaries.Where(b => !b.ShouldBeValid).Should().HaveCount(2);
    }
}

public class PaymentCommandTests
{
    [Fact]
    public void PaymentCommand_Creation_FullPayload()
    {
        var request = TestDataFactory.CreateValidRequest();
        var command = new PaymentCommand
        {
            CorrelationId = request.CorrelationId,
            PaymentRequest = request,
            IdempotencyKey = "idem-key-12345",
        };

        command.CorrelationId.Should().Be(request.CorrelationId);
        command.PaymentRequest.Should().BeEquivalentTo(request);
        command.IdempotencyKey.Should().Be("idem-key-12345");
    }

    [Fact]
    public void PaymentCommand_Defaults_AreEmpty()
    {
        var command = new PaymentCommand();
        command.CorrelationId.Should().BeEmpty();
        command.PaymentRequest.Should().BeNull();
        command.IdempotencyKey.Should().BeEmpty();
    }

    [Fact]
    public void PaymentFailedEvent_WithNullOriginalRequest_Constructable()
    {
        var failed = new PaymentFailed
        {
            CorrelationId = "NULL-REQ-FAIL",
            OriginalRequest = null!,
            FailedStep = "Unknown",
            ErrorMessage = "Unknown error with no context",
            ErrorCode = "UNKNOWN",
            FailedAt = DateTime.UtcNow,
        };

        failed.CorrelationId.Should().Be("NULL-REQ-FAIL");
        failed.OriginalRequest.Should().BeNull();
        failed.FailedStep.Should().Be("Unknown");
    }
}
// END_BLOCK_TESTS
