// FILE: tests/.../WriterService.IntegrationTests/PaymentWriterRequestIntegrityTests.cs
// VERSION: 2.0.0
// MODULE: M-INTEGRATION
// PURPOSE: Test specification
// SEMANTIC_TAG: [TEST]
// START_MODULE M_INTEGRATION

// FILE: tests/.../WriterService.IntegrationTests/PaymentWriterRequestIntegrityTests.cs
// VERSION: 1.0.0

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PaymentService.Shared.Commands;
using PaymentService.Shared.Dtos;

namespace PaymentService.Api.WriterService.IntegrationTests;

/// <summary>
/// Request/response integrity tests for the writer API.
/// </summary>
public class PaymentWriterRequestIntegrityTests : IClassFixture<WriterApiFixture>
{
    private readonly WriterApiFixture _fixture;

    public PaymentWriterRequestIntegrityTests(WriterApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ResponseJson_ContainsCorrelationId()
    {
        var request = new CreatePaymentRequest
        {
            CorrelationId = "resp-json",
            SenderAccount = "SRC",
            ReceiverAccount = "DST",
            Amount = 100m,
            Currency = "USD"
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("correlationId");
        content.Should().Contain("resp-json");
    }

    [Fact]
    public async Task AcceptedResponse_HasBody()
    {
        var request = new CreatePaymentRequest
        {
            CorrelationId = "has-body",
            SenderAccount = "SRC",
            ReceiverAccount = "DST",
            Amount = 100m,
            Currency = "USD"
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        var body = await response.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        body.Should().NotBeNull();
        body!.CorrelationId.Should().Be("has-body");
        body.AcceptedAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task BadRequestResponse_HasErrorBody()
    {
        var request = new CreatePaymentRequest
        {
            CorrelationId = "",
            SenderAccount = "SRC",
            ReceiverAccount = "DST",
            Amount = 100m,
            Currency = "USD"
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("error");
    }

    [Fact]
    public async Task CreatePayment_PurchaseForMaxLiquidity_Accepted()
    {
        var request = new CreatePaymentRequest
        {
            CorrelationId = "max-liq",
            SenderAccount = "FED-RESERVE",
            ReceiverAccount = "TREASURY",
            Amount = 500_000_000_000m,  // 500 billion
            Currency = "USD"
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task CreatePayment_MicroTransaction_Accepted()
    {
        var request = new CreatePaymentRequest
        {
            CorrelationId = "micro-trans",
            SenderAccount = "A",
            ReceiverAccount = "B",
            Amount = 0.0001m,  // Very tiny amount (accepted by API, saga may reject)
            Currency = "USD"
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task CreatePayment_VariousCurrencies_LargeAmounts()
    {
        var currencies = new[] { "USD", "EUR", "GBP", "JPY", "CHF", "AUD", "CAD", "NOK", "SEK" };

        foreach (var curr in currencies)
        {
            _fixture.MessagePublisher.Clear();
            var request = new CreatePaymentRequest
            {
                CorrelationId = $"large-{curr}",
                SenderAccount = "LARGE-SRC",
                ReceiverAccount = "LARGE-DST",
                Amount = 1_000_000m,
                Currency = curr
            };

            var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
            response.StatusCode.Should().Be(HttpStatusCode.Accepted);

            var payment = await _fixture.GetPaymentAsync($"large-{curr}");
            payment!.Request.Currency.Should().Be(curr);
            payment.Request.Amount.Should().Be(1_000_000m);
        }
    }

    [Fact]
    public async Task CreatePayment_SpecialCharacterDescription_Accepted()
    {
        var request = new CreatePaymentRequest
        {
            CorrelationId = "special-desc",
            SenderAccount = "SRC",
            ReceiverAccount = "DST",
            Amount = 200m,
            Currency = "USD",
            Description = "Payment & for \"services\" rendered @ 100% rate — invoice #42/2026"
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var payment = await _fixture.GetPaymentAsync("special-desc");
        payment!.Request.Description.Should().Be(
            "Payment & for \"services\" rendered @ 100% rate — invoice #42/2026");
    }

    [Fact]
    public async Task CreatePayment_UnicodeDescription_Accepted()
    {
        var request = new CreatePaymentRequest
        {
            CorrelationId = "unicode-desc",
            SenderAccount = "SRC",
            ReceiverAccount = "DST",
            Amount = 300m,
            Currency = "EUR",
            Description = "Оплата за услуги по договору №2026-05 от 01.05.2026"
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var payment = await _fixture.GetPaymentAsync("unicode-desc");
        payment!.Request.Description.Should().Contain("договору");
    }

    [Fact]
    public async Task CreatePayment_SameAmounts_DifferentCurrencies_AllPersisted()
    {
        var pairs = new[] { ("usd-1", "USD"), ("eur-1", "EUR"), ("gbp-1", "GBP") };

        foreach (var (id, curr) in pairs)
        {
            var request = new CreatePaymentRequest
            {
                CorrelationId = id,
                SenderAccount = "SRC",
                ReceiverAccount = "DST",
                Amount = 1000m,
                Currency = curr
            };
            await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        }

        foreach (var (id, _) in pairs)
        {
            var payment = await _fixture.GetPaymentAsync(id);
            payment.Should().NotBeNull();
            payment!.Request.Amount.Should().Be(1000m);
        }
    }

    [Fact]
    public async Task CreatePayment_PayloadMatchesPersisted_Exactly()
    {
        var expected = new CreatePaymentRequest
        {
            CorrelationId = "exact-match",
            SenderAccount = "EXACT-SRC-001",
            ReceiverAccount = "EXACT-DST-002",
            Amount = 7777.77m,
            Currency = "CHF",
            ValueDate = new DateTime(2027, 1, 1)
        };

        await _fixture.Client.PostAsJsonAsync("/api/payment", expected);

        var actual = await _fixture.GetPaymentAsync("exact-match");
        actual!.CorrelationId.Should().Be(expected.CorrelationId);
        actual.Request.SenderAccount.Should().Be(expected.SenderAccount);
        actual.Request.ReceiverAccount.Should().Be(expected.ReceiverAccount);
        actual.Request.Amount.Should().Be(expected.Amount);
        actual.Request.Currency.Should().Be(expected.Currency);
        actual.Request.ValueDate.Should().Be(expected.ValueDate);
        actual.Status.Should().Be("Pending");
        actual.SagaState.Should().Be("Validating");
    }
}
