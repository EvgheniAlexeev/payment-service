// FILE: tests/.../WriterService.IntegrationTests/PaymentWriterIntegrationTests.cs
// VERSION: 2.0.0
// MODULE: M-INTEGRATION
// PURPOSE: Test specification
// SEMANTIC_TAG: [TEST]
// START_MODULE M_INTEGRATION

// FILE: tests/.../WriterService.IntegrationTests/PaymentWriterIntegrationTests.cs
// VERSION: 1.0.0

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PaymentService.Shared.Commands;
using PaymentService.Shared.Dtos;
using PaymentService.Shared.Models;

namespace PaymentService.Api.WriterService.IntegrationTests;

/// <summary>
/// Integration tests for PaymentCommandController endpoints.
/// Tests HTTP layer end-to-end with real MongoDB via Testcontainers.
/// </summary>
public class PaymentWriterIntegrationTests : IClassFixture<WriterApiFixture>
{
    private readonly WriterApiFixture _fixture;

    public PaymentWriterIntegrationTests(WriterApiFixture fixture)
    {
        _fixture = fixture;
    }

    private CreatePaymentRequest CreateValidRequest(string correlationId = "new-001") => new()
    {
        CorrelationId = correlationId,
        SenderAccount = "ACC001",
        ReceiverAccount = "ACC002",
        Amount = 1000m,
        Currency = "USD",
        Description = "Test payment"
    };

    // ============================================
    // Happy Path
    // ============================================

    [Fact]
    public async Task CreatePayment_ValidRequest_Returns202Accepted()
    {
        var request = CreateValidRequest("new-happy-1");

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task CreatePayment_ValidRequest_ReturnsCorrelationId()
    {
        var request = CreateValidRequest("new-happy-2");

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        var body = await response.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        body.Should().NotBeNull();
        body!.CorrelationId.Should().Be("new-happy-2");
        body.Message.Should().Contain("accepted");
    }

    [Fact]
    public async Task CreatePayment_PersistsPaymentDocument()
    {
        var request = CreateValidRequest("new-persist");

        await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        var payment = await _fixture.GetPaymentAsync("new-persist");
        payment.Should().NotBeNull();
        payment!.CorrelationId.Should().Be("new-persist");
        payment.Status.Should().Be("Pending");
        payment.SagaState.Should().Be("Validating");
        payment.Request.Amount.Should().Be(1000m);
        payment.Request.Currency.Should().Be("USD");
        payment.Request.SenderAccount.Should().Be("ACC001");
        payment.Request.ReceiverAccount.Should().Be("ACC002");
    }

    [Fact]
    public async Task CreatePayment_PublishesSagaCommand()
    {
        _fixture.MessagePublisher.Clear();
        var request = CreateValidRequest("new-saga");

        await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        var commands = _fixture.GetPublishedCommands();
        commands.Should().HaveCount(1);
        commands[0].CorrelationId.Should().Be("new-saga");
        commands[0].IdempotencyKey.Should().Be("new-saga");
        commands[0].PaymentRequest.Amount.Should().Be(1000m);
    }

    [Fact]
    public async Task CreatePayment_CreatedAtTimestampIsSet()
    {
        var request = CreateValidRequest("new-created");

        await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        var payment = await _fixture.GetPaymentAsync("new-created");
        payment!.CreatedAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
        payment.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task CreatePayment_WithValueDate_PersistsCorrectly()
    {
        var request = CreateValidRequest("new-valuedate") with
        {
            ValueDate = new DateTime(2026, 12, 25)
        };

        await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        var payment = await _fixture.GetPaymentAsync("new-valuedate");
        payment!.Request.ValueDate.Should().Be(new DateTime(2026, 12, 25));
    }

    [Fact]
    public async Task CreatePayment_DifferentCurrencies()
    {
        foreach (var currency in new[] { "USD", "EUR", "GBP", "JPY", "CHF" })
        {
            _fixture.MessagePublisher.Clear();
            var id = $"curr-{currency}";
            var request = CreateValidRequest(id) with { Currency = currency };

            var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
            response.StatusCode.Should().Be(HttpStatusCode.Accepted);

            var payment = await _fixture.GetPaymentAsync(id);
            payment!.Request.Currency.Should().Be(currency);
        }
    }

    // ============================================
    // Validation Tests
    // ============================================

    [Fact]
    public async Task CreatePayment_MissingCorrelationId_Returns400()
    {
        var request = CreateValidRequest() with { CorrelationId = "" };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayment_MissingSenderAccount_Returns400()
    {
        var request = CreateValidRequest("bad-sender") with { SenderAccount = "" };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayment_MissingReceiverAccount_Returns400()
    {
        var request = CreateValidRequest("bad-receiver") with { ReceiverAccount = "" };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayment_ZeroAmount_Returns400()
    {
        var request = CreateValidRequest("bad-zero") with { Amount = 0 };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayment_NegativeAmount_Returns400()
    {
        var request = CreateValidRequest("bad-neg") with { Amount = -100m };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayment_ExcessiveAmount_Returns400()
    {
        var request = CreateValidRequest("bad-huge") with { Amount = 1_000_000_000_000M };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayment_InvalidCurrency_Returns400()
    {
        var request = CreateValidRequest("bad-curr") with { Currency = "ZZ" };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayment_LongCurrency_Returns400()
    {
        var request = CreateValidRequest("bad-curr-long") with { Currency = "USDD" };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayment_LowercaseCurrency_Returns400()
    {
        var request = CreateValidRequest("bad-curr-lower") with { Currency = "usd" };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayment_LongCorrelationId_Returns400()
    {
        var request = CreateValidRequest(new string('x', 101));

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayment_LongSenderAccount_Returns400()
    {
        var request = CreateValidRequest("bad-sender-len") with
        {
            SenderAccount = new string('x', 51)
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ============================================
    // Idempotency Tests
    // ============================================

    [Fact]
    public async Task CreatePayment_DuplicateCorrelationId_Returns202WithSameData()
    {
        var request = CreateValidRequest("idem-dup");

        var r1 = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        r1.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var r2 = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        r2.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var body = await r2.Content.ReadFromJsonAsync<CreatePaymentResponse>();
        body!.CorrelationId.Should().Be("idem-dup");
        body.Message.Should().Contain("already");

        // Only one payment document should exist
        var payment = await _fixture.GetPaymentAsync("idem-dup");
        payment.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePayment_DuplicateDoesNotPublishSecondCommand()
    {
        _fixture.MessagePublisher.Clear();
        var request = CreateValidRequest("idem-nopub");

        await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        var afterFirst = _fixture.GetPublishedCommands().Count;

        _fixture.MessagePublisher.Clear();
        await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        var afterSecond = _fixture.GetPublishedCommands().Count;

        afterFirst.Should().Be(1);
        afterSecond.Should().Be(0, "Duplicate should not publish second command");
    }

    // ============================================
    // Large volume tests
    // ============================================

    [Fact]
    public async Task CreatePayment_MultipleUnique_CreateAll()
    {
        _fixture.MessagePublisher.Clear();
        for (int i = 0; i < 30; i++)
        {
            var id = $"batch-{i:D3}";
            var request = new CreatePaymentRequest
            {
                CorrelationId = id,
                SenderAccount = $"SRC{i:D3}",
                ReceiverAccount = $"DST{i:D3}",
                Amount = 100m * (i + 1),
                Currency = i % 2 == 0 ? "USD" : "EUR"
            };

            var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
            response.StatusCode.Should().Be(HttpStatusCode.Accepted, $"Payment {id} should be accepted");
        }

        // Verify all created
        for (int i = 0; i < 30; i++)
        {
            var payment = await _fixture.GetPaymentAsync($"batch-{i:D3}");
            payment.Should().NotBeNull();
        }

        // Verify 30 commands published
        _fixture.GetPublishedCommands().Should().HaveCount(30);
    }

    [Fact]
    public async Task CreatePayment_WithDescription_PersistsCorrectly()
    {
        var request = CreateValidRequest("desc-001") with
        {
            Description = "Payment for invoice #INV-2026-0042"
        };

        await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        var payment = await _fixture.GetPaymentAsync("desc-001");
        payment!.Request.Description.Should().Be("Payment for invoice #INV-2026-0042");
    }

    [Fact]
    public async Task CreatePayment_ResponseHasAcceptedAtTimestamp()
    {
        var request = CreateValidRequest("accepted-ts");

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        var body = await response.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        body!.AcceptedAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task CreatePayment_MultipleRequestsAllReturn202()
    {
        for (int i = 0; i < 10; i++)
        {
            var request = CreateValidRequest($"multi-{i}");

            var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }
    }
}
