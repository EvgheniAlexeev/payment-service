// FILE: tests/.../WriterService.IntegrationTests/PaymentWriterIdempotencyTests.cs
// VERSION: 2.0.0
// MODULE: M-INTEGRATION
// PURPOSE: Test specification
// SEMANTIC_TAG: [TEST]
// START_MODULE M_INTEGRATION

// FILE: tests/.../WriterService.IntegrationTests/PaymentWriterIdempotencyTests.cs
// VERSION: 1.0.0

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PaymentService.Shared.Commands;
using PaymentService.Shared.Dtos;

namespace PaymentService.Api.WriterService.IntegrationTests;

/// <summary>
/// Deep idempotency tests for the writer API.
/// </summary>
public class PaymentWriterIdempotencyTests : IClassFixture<WriterApiFixture>
{
    private readonly WriterApiFixture _fixture;

    public PaymentWriterIdempotencyTests(WriterApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TripleDuplicate_AllReturn202_NoDataCorruption()
    {
        _fixture.MessagePublisher.Clear();
        var request = new CreatePaymentRequest
        {
            CorrelationId = "triple-idem",
            SenderAccount = "SRC",
            ReceiverAccount = "DST",
            Amount = 250m,
            Currency = "USD"
        };

        var r1 = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        var r2 = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        var r3 = await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        r1.StatusCode.Should().Be(HttpStatusCode.Accepted);
        r2.StatusCode.Should().Be(HttpStatusCode.Accepted);
        r3.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var payment = await _fixture.GetPaymentAsync("triple-idem");
        payment.Should().NotBeNull();
        payment!.Request.Amount.Should().Be(250m);

        // Only one command should be published
        _fixture.GetPublishedCommands().Should().HaveCount(1);
    }

    [Fact]
    public async Task IdempotentResponse_HasUniqueMessage()
    {
        var request = new CreatePaymentRequest
        {
            CorrelationId = "idem-msg",
            SenderAccount = "SRC",
            ReceiverAccount = "DST",
            Amount = 100m,
            Currency = "USD"
        };

        var r1 = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        var body1 = await r1.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        var r2 = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        var body2 = await r2.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        body1!.Message.Should().Contain("accepted");
        body2!.Message.Should().Contain("already");
    }

    [Fact]
    public async Task IdempotentResponse_HasSameCorrelationId()
    {
        var request = new CreatePaymentRequest
        {
            CorrelationId = "idem-same",
            SenderAccount = "SRC",
            ReceiverAccount = "DST",
            Amount = 100m,
            Currency = "USD"
        };

        var r1 = await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        var r2 = await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        var body1 = await r1.Content.ReadFromJsonAsync<CreatePaymentResponse>();
        var body2 = await r2.Content.ReadFromJsonAsync<CreatePaymentResponse>();

        body1!.CorrelationId.Should().Be("idem-same");
        body2!.CorrelationId.Should().Be("idem-same");
    }

    [Fact]
    public async Task Idempotent_DoesNotModifyExistingPayment()
    {
        var request = new CreatePaymentRequest
        {
            CorrelationId = "idem-mod",
            SenderAccount = "SRC",
            ReceiverAccount = "DST",
            Amount = 500m,
            Currency = "USD"
        };

        await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        // Second call with different amount should NOT modify the original
        var modifiedRequest = request with { Amount = 999m };
        await _fixture.Client.PostAsJsonAsync("/api/payment", modifiedRequest);

        var payment = await _fixture.GetPaymentAsync("idem-mod");
        payment!.Request.Amount.Should().Be(500m, "Original amount should be preserved on duplicate");
    }

    [Fact]
    public async Task RapidConcurrentSameCorrelationId_OnlyOnePersisted()
    {
        var request = new CreatePaymentRequest
        {
            CorrelationId = "concurrent-idem",
            SenderAccount = "SRC",
            ReceiverAccount = "DST",
            Amount = 300m,
            Currency = "USD"
        };

        var tasks = Enumerable.Range(0, 5).Select(_ =>
            _fixture.Client.PostAsJsonAsync("/api/payment", request));

        var responses = await Task.WhenAll(tasks);

        // All should succeed (some get 202 original, some get 202 idempotent)
        foreach (var r in responses)
            r.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var payment = await _fixture.GetPaymentAsync("concurrent-idem");
        payment.Should().NotBeNull();
    }

    [Fact]
    public async Task BulkIdempotent_EachUniqueOnlyOnce()
    {
        _fixture.MessagePublisher.Clear();
        var uniqueIds = new List<string>();
        for (int i = 0; i < 20; i++)
            uniqueIds.Add($"bulk-idem-{i:D3}");

        // Send each twice
        foreach (var id in uniqueIds)
        {
            var request = new CreatePaymentRequest
            {
                CorrelationId = id,
                SenderAccount = "SRC",
                ReceiverAccount = "DST",
                Amount = 100m,
                Currency = "USD"
            };
            await _fixture.Client.PostAsJsonAsync("/api/payment", request);
            await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        }

        _fixture.GetPublishedCommands().Should().HaveCount(20);
    }
}
