// FILE: tests/.../WriterService.IntegrationTests/PaymentWriterSagaPublishingTests.cs
// VERSION: 2.0.0
// MODULE: M-INTEGRATION
// PURPOSE: Test specification
// SEMANTIC_TAG: [TEST]
// START_MODULE M_INTEGRATION

// FILE: tests/.../WriterService.IntegrationTests/PaymentWriterSagaPublishingTests.cs
// VERSION: 1.0.0

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PaymentService.Api.WriterService.Models;
using PaymentService.Shared.Commands;

namespace PaymentService.Api.WriterService.IntegrationTests;

/// <summary>
/// Tests focused on saga publishing behavior.
/// </summary>
public class PaymentWriterSagaPublishingTests : IClassFixture<WriterApiFixture>
{
    private readonly WriterApiFixture _fixture;

    public PaymentWriterSagaPublishingTests(WriterApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CommandPublished_ContainsFullRequest()
    {
        _fixture.MessagePublisher.Clear();
        var request = new CreatePaymentRequest
        {
            CorrelationId = "full-req",
            SenderAccount = "SRC-X",
            ReceiverAccount = "DST-Y",
            Amount = 1234.56m,
            Currency = "EUR",
            ValueDate = new DateTime(2026, 7, 1),
            Description = "Full request test"
        };

        await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        var commands = _fixture.GetPublishedCommands();
        commands.Should().HaveCount(1);
        var cmd = commands[0];
        cmd.CorrelationId.Should().Be("full-req");
        cmd.Request.SenderAccount.Should().Be("SRC-X");
        cmd.Request.ReceiverAccount.Should().Be("DST-Y");
        cmd.Request.Amount.Should().Be(1234.56m);
        cmd.Request.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task EachUniqueRequest_PublishesDistinctCommand()
    {
        _fixture.MessagePublisher.Clear();
        for (int i = 0; i < 10; i++)
        {
            var request = new CreatePaymentRequest
            {
                CorrelationId = $"distinct-{i}",
                SenderAccount = $"SRC-{i}",
                ReceiverAccount = $"DST-{i}",
                Amount = 100m * i,
                Currency = "USD"
            };
            await _fixture.Client.PostAsJsonAsync("/api/payment", request);
        }

        var commands = _fixture.GetPublishedCommands();
        commands.Should().HaveCount(10);
        commands.Select(c => c.CorrelationId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task IdempotencyKey_MatchesCorrelationId()
    {
        _fixture.MessagePublisher.Clear();
        var request = new CreatePaymentRequest
        {
            CorrelationId = "idem-key",
            SenderAccount = "SRC",
            ReceiverAccount = "DST",
            Amount = 100m,
            Currency = "USD"
        };

        await _fixture.Client.PostAsJsonAsync("/api/payment", request);

        var cmd = _fixture.GetPublishedCommands()[0];
        cmd.IdempotencyKey.Should().Be(cmd.CorrelationId);
    }

    [Fact]
    public async Task MultiplePayments_AllPublishCommands()
    {
        _fixture.MessagePublisher.Clear();
        var ids = new List<string>();
        for (int i = 0; i < 25; i++)
        {
            ids.Add($"vol-{i:D3}");
        }

        var tasks = ids.Select(id =>
            _fixture.Client.PostAsJsonAsync("/api/payment", new CreatePaymentRequest
            {
                CorrelationId = id,
                SenderAccount = "SRC",
                ReceiverAccount = "DST",
                Amount = 100m,
                Currency = "USD"
            }));

        var responses = await Task.WhenAll(tasks);
        foreach (var r in responses)
            r.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var commands = _fixture.GetPublishedCommands();
        commands.Should().HaveCount(25);
    }
}
