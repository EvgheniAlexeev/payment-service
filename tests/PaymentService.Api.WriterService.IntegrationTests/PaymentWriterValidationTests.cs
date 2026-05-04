// FILE: tests/.../WriterService.IntegrationTests/PaymentWriterValidationTests.cs
// VERSION: 1.0.0

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PaymentService.Api.WriterService.Models;
using PaymentService.Shared.Models;

namespace PaymentService.Api.WriterService.IntegrationTests;

/// <summary>
/// Comprehensive validation edge case tests for payment creation.
/// </summary>
public class PaymentWriterValidationTests : IClassFixture<WriterApiFixture>
{
    private readonly WriterApiFixture _fixture;

    public PaymentWriterValidationTests(WriterApiFixture fixture)
    {
        _fixture = fixture;
    }

    private static CreatePaymentRequest CreateValid(string id) => new()
    {
        CorrelationId = id,
        SenderAccount = "ACC001",
        ReceiverAccount = "ACC002",
        Amount = 500m,
        Currency = "USD"
    };

    [Fact]
    public async Task CreatePayment_PastValueDate_Returns400()
    {
        var request = CreateValid("past-date") with
        {
            ValueDate = DateTime.UtcNow.Date.AddDays(-5)
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayment_YesterdayValueDate_Returns400()
    {
        var request = CreateValid("yesterday-date") with
        {
            ValueDate = DateTime.UtcNow.Date.AddDays(-1)
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayment_TodayValueDate_Returns202()
    {
        var request = CreateValid("today-date") with
        {
            ValueDate = DateTime.UtcNow.Date
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task CreatePayment_FutureValueDate_Returns202()
    {
        var request = CreateValid("future-date") with
        {
            ValueDate = DateTime.UtcNow.Date.AddYears(1)
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CreatePayment_EmptyOrWhitespaceReceiverAccount_Returns400(string receiverAccount)
    {
        var request = CreateValid("bad-recv") with { ReceiverAccount = receiverAccount };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CreatePayment_EmptyOrWhitespaceSenderAccount_Returns400(string senderAccount)
    {
        var request = CreateValid("bad-send") with { SenderAccount = senderAccount };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    public async Task CreatePayment_NonPositiveAmount_Returns400(decimal amount)
    {
        var request = CreateValid("bad-amt") with { Amount = amount };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayment_MinimalValidAmount_Returns202()
    {
        var request = CreateValid("min-amt") with { Amount = 0.01m };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task CreatePayment_MaxValidAmount_Returns202()
    {
        var request = CreateValid("max-amt") with { Amount = 999_999_999_999M };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("Usd")]
    [InlineData("123")]
    public async Task CreatePayment_NonUppercaseCurrency_Returns400(string currency)
    {
        var request = CreateValid("bad-curr-case") with { Currency = currency };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePayment_DescriptionAtMaxLength_Returns202()
    {
        var request = CreateValid("max-desc") with
        {
            Description = new string('a', 500) // Max is not enforced by validator anymore, just passthrough
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task CreatePayment_SameSenderReceiver_Accepted()
    {
        var request = CreateValid("same-acct") with
        {
            SenderAccount = "ACC-SAME",
            ReceiverAccount = "ACC-SAME"
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        // Not a validation rule at API level (saga may handle)
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task CreatePayment_OnlyCorrelationIdDiffers_PersistsIndependently()
    {
        var baseRequest = new CreatePaymentRequest
        {
            SenderAccount = "COMMON-SRC",
            ReceiverAccount = "COMMON-DST",
            Amount = 100m,
            Currency = "USD"
        };

        var r1 = baseRequest with { CorrelationId = "uniq-1" };
        var r2 = baseRequest with { CorrelationId = "uniq-2" };

        await _fixture.Client.PostAsJsonAsync("/api/payment", r1);
        await _fixture.Client.PostAsJsonAsync("/api/payment", r2);

        var p1 = await _fixture.GetPaymentAsync("uniq-1");
        var p2 = await _fixture.GetPaymentAsync("uniq-2");

        p1.Should().NotBeNull();
        p2.Should().NotBeNull();
        p1!.CorrelationId.Should().NotBe(p2!.CorrelationId);
    }

    [Fact]
    public async Task CreatePayment_UnicodeCorrelationId_Accepted()
    {
        var request = CreateValid("тест-платеж-001");

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var payment = await _fixture.GetPaymentAsync("тест-платеж-001");
        payment.Should().NotBeNull();
    }
}
