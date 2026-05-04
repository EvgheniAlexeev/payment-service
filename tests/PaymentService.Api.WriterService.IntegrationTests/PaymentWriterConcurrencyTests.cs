// FILE: tests/.../WriterService.IntegrationTests/PaymentWriterConcurrencyTests.cs
// VERSION: 1.0.0

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PaymentService.Api.WriterService.Models;

namespace PaymentService.Api.WriterService.IntegrationTests;

/// <summary>
/// Concurrency and edge case tests for writer API.
/// </summary>
public class PaymentWriterConcurrencyTests : IClassFixture<WriterApiFixture>
{
    private readonly WriterApiFixture _fixture;

    public PaymentWriterConcurrencyTests(WriterApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConcurrentUniqueRequests_AllAccepted()
    {
        _fixture.MessagePublisher.Clear();
        var tasks = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < 20; i++)
        {
            var id = $"concurrent-{i:D3}";
            var request = new CreatePaymentRequest
            {
                CorrelationId = id,
                SenderAccount = $"SRC-{i}",
                ReceiverAccount = $"DST-{i}",
                Amount = 100m,
                Currency = "USD"
            };
            tasks.Add(_fixture.Client.PostAsJsonAsync("/api/payment", request));
        }

        var responses = await Task.WhenAll(tasks);

        foreach (var r in responses)
            r.StatusCode.Should().Be(HttpStatusCode.Accepted);

        _fixture.GetPublishedCommands().Should().HaveCount(20);
    }

    [Fact]
    public async Task CreatePayment_NullValueDate_Accepted()
    {
        var request = new CreatePaymentRequest
        {
            CorrelationId = "null-date",
            SenderAccount = "SRC",
            ReceiverAccount = "DST",
            Amount = 100m,
            Currency = "USD",
            ValueDate = null
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var payment = await _fixture.GetPaymentAsync("null-date");
        payment!.Request.ValueDate.Should().BeNull();
    }

    [Fact]
    public async Task CreatePayment_EmptyDescription_Accepted()
    {
        var request = new CreatePaymentRequest
        {
            CorrelationId = "empty-desc",
            SenderAccount = "SRC",
            ReceiverAccount = "DST",
            Amount = 100m,
            Currency = "USD",
            Description = ""
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task CreatePayment_WithSpacesInAccount_Accepted()
    {
        var request = new CreatePaymentRequest
        {
            CorrelationId = "space-acct",
            SenderAccount = "ACC 001",
            ReceiverAccount = "ACC 002",
            Amount = 100m,
            Currency = "USD"
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task CreatePayment_MinimalValidFields_Accepted()
    {
        var request = new CreatePaymentRequest
        {
            CorrelationId = "minimal",
            SenderAccount = "A",
            ReceiverAccount = "B",
            Amount = 0.01m,
            Currency = "USD"
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task CreatePayment_MaxLengthCorrelationId_Accepted()
    {
        var id = new string('x', 100);
        var request = new CreatePaymentRequest
        {
            CorrelationId = id,
            SenderAccount = "SRC",
            ReceiverAccount = "DST",
            Amount = 100m,
            Currency = "USD"
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task CreatePayment_MaxLengthSenderAccount_Accepted()
    {
        var request = new CreatePaymentRequest
        {
            CorrelationId = "max-sender",
            SenderAccount = new string('A', 50),
            ReceiverAccount = "DST",
            Amount = 100m,
            Currency = "USD"
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task CreatePayment_MaxLengthReceiverAccount_Accepted()
    {
        var request = new CreatePaymentRequest
        {
            CorrelationId = "max-receiver",
            SenderAccount = "SRC",
            ReceiverAccount = new string('B', 50),
            Amount = 100m,
            Currency = "USD"
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task CreatePayment_ResponseContentType_IsJson()
    {
        var request = new CreatePaymentRequest
        {
            CorrelationId = "content-type",
            SenderAccount = "SRC",
            ReceiverAccount = "DST",
            Amount = 100m,
            Currency = "USD"
        };

        var response = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task CreatePayment_PublishedCommandHasSameIdempotencyKey()
    {
        _fixture.MessagePublisher.Clear();
        var request = new CreatePaymentRequest
        {
            CorrelationId = "idem-key-check",
            SenderAccount = "SRC",
            ReceiverAccount = "DST",
            Amount = 500m,
            Currency = "EUR"
        };

        await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        var cmd = _fixture.GetPublishedCommands()[0];
        cmd.IdempotencyKey.Should().Be("idem-key-check");
        cmd.CorrelationId.Should().Be("idem-key-check");
    }

    [Fact]
    public async Task CreatePayment_PublishedCommandHasFullRequestDto()
    {
        _fixture.MessagePublisher.Clear();
        var request = new CreatePaymentRequest
        {
            CorrelationId = "full-cmd",
            SenderAccount = "BANK-A",
            ReceiverAccount = "BANK-B",
            Amount = 9876.54m,
            Currency = "GBP",
            ValueDate = new DateTime(2026, 8, 15),
            Description = "Quarterly settlement"
        };

        await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        var cmd = _fixture.GetPublishedCommands()[0];
        cmd.Request.CorrelationId.Should().Be("full-cmd");
        cmd.Request.SenderAccount.Should().Be("BANK-A");
        cmd.Request.ReceiverAccount.Should().Be("BANK-B");
        cmd.Request.Amount.Should().Be(9876.54m);
        cmd.Request.Currency.Should().Be("GBP");
        cmd.Request.ValueDate.Should().Be(new DateTime(2026, 8, 15));
        cmd.Request.Description.Should().Be("Quarterly settlement");
    }

    [Fact]
    public async Task CreatePayment_PaymentDocumentHasAllFields()
    {
        var request = new CreatePaymentRequest
        {
            CorrelationId = "all-doc-fields",
            SenderAccount = "SRC-123",
            ReceiverAccount = "DST-456",
            Amount = 999.99m,
            Currency = "JPY",
            ValueDate = new DateTime(2026, 10, 1)
        };

        await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        var doc = await _fixture.GetPaymentAsync("all-doc-fields");
        doc.Should().NotBeNull();
        doc!.CorrelationId.Should().Be("all-doc-fields");
        doc.Status.Should().Be("Pending");
        doc.SagaState.Should().Be("Validating");
        doc.Request.SenderAccount.Should().Be("SRC-123");
        doc.Request.ReceiverAccount.Should().Be("DST-456");
        doc.Request.Amount.Should().Be(999.99m);
        doc.Request.Currency.Should().Be("JPY");
        doc.Request.ValueDate.Should().Be(new DateTime(2026, 10, 1));
        doc.CreatedAt.Should().BeAfter(DateTime.UtcNow.AddSeconds(-10));
    }
}
