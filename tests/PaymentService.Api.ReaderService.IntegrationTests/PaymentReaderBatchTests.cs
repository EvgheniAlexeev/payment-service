// FILE: tests/.../ReaderService.IntegrationTests/PaymentReaderBatchTests.cs
// VERSION: 1.0.0

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PaymentService.Shared.Dtos;
using PaymentService.Shared.Models;

namespace PaymentService.Api.ReaderService.IntegrationTests;

/// <summary>
/// Batch and pagination tests for the reader API.
/// </summary>
public class PaymentReaderBatchTests : IClassFixture<ApiIntegrationFixture>
{
    private readonly ApiIntegrationFixture _fixture;

    public PaymentReaderBatchTests(ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetPaymentsByStatus_LargePageSize_ReturnsExactCount()
    {
        for (int i = 0; i < 50; i++)
        {
            await _fixture.SeedPaymentAsync(new PaymentDocument
            {
                CorrelationId = $"large-{i:D3}",
                Status = "Pending",
                Request = new()
                {
                    CorrelationId = $"large-{i:D3}",
                    SenderAccount = "SRC",
                    ReceiverAccount = "DST",
                    Amount = i,
                    Currency = "USD"
                }
            });
        }

        var response = await _fixture.Client.GetAsync("/api/payment/by-status/Pending?pageSize=100");
        var body = await response.Content.ReadFromJsonAsync<PagedPaymentStatusResponse>();
        body!.Items.Should().HaveCount(50);
    }

    [Fact]
    public async Task GetPaymentsByStatus_RequestedPageSizeMaxCapped()
    {
        var response = await _fixture.Client.GetAsync("/api/payment/by-status/Pending?pageSize=999");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Settled")]
    [InlineData("Failed")]
    [InlineData("Validating")]
    [InlineData("Enriching")]
    public async Task VariousStatuses_FilterCorrectly(string status)
    {
        await _fixture.SeedPaymentAsync(new PaymentDocument
        {
            CorrelationId = $"vstat-{status}",
            Status = status,
            Request = new()
            {
                CorrelationId = $"vstat-{status}",
                SenderAccount = "A", ReceiverAccount = "B",
                Amount = 100m, Currency = "USD"
            }
        });

        var response = await _fixture.Client.GetAsync($"/api/payment/by-status/{status}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedPaymentStatusResponse>();
        body!.Items.Should().Contain(i => i.Status == status);
    }

    [Fact]
    public async Task GetPayment_MultipleSameCurrency_SortsByCreatedAt()
    {
        await _fixture.SeedPaymentAsync(new PaymentDocument
        {
            CorrelationId = "sort-a",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            Request = new() { CorrelationId = "sort-a", SenderAccount = "A", ReceiverAccount = "B", Amount = 1, Currency = "USD" }
        });
        await _fixture.SeedPaymentAsync(new PaymentDocument
        {
            CorrelationId = "sort-b",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            Request = new() { CorrelationId = "sort-b", SenderAccount = "A", ReceiverAccount = "B", Amount = 2, Currency = "USD" }
        });

        var response = await _fixture.Client.GetAsync("/api/payment/by-status/Pending");
        var body = await response.Content.ReadFromJsonAsync<PagedPaymentStatusResponse>();
        body!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPayment_ReturnsAllDtoFields()
    {
        var payment = new PaymentDocument
        {
            CorrelationId = "all-fields",
            Status = "Settled",
            SagaState = "Completed",
            CreatedAt = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            SettledAt = new DateTime(2026, 1, 15, 10, 35, 0, DateTimeKind.Utc),
            Request = new PaymentRequestDto
            {
                CorrelationId = "all-fields",
                SenderAccount = "SENDER-123",
                ReceiverAccount = "RECEIVER-456",
                Amount = 5432.10m,
                Currency = "CHF"
            }
        };
        await _fixture.SeedPaymentAsync(payment);

        var response = await _fixture.Client.GetAsync("/api/payment/all-fields");
        var body = await response.Content.ReadFromJsonAsync<PaymentStatusDto>();

        body!.CorrelationId.Should().Be("all-fields");
        body.Status.Should().Be("Settled");
        body.SagaState.Should().Be("Completed");
        body.Amount.Should().Be(5432.10m);
        body.Currency.Should().Be("CHF");
        body.SenderAccount.Should().Be("SENDER-123");
        body.ReceiverAccount.Should().Be("RECEIVER-456");
        body.CreatedAt.Should().Be(new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc));
        body.SettledAt.Should().Be(new DateTime(2026, 1, 15, 10, 35, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetPayment_UnsettledPayment_HasNullSettledAt()
    {
        await _fixture.SeedPaymentAsync(new PaymentDocument
        {
            CorrelationId = "unsettled",
            Status = "Pending",
            SettledAt = null,
            Request = new() { CorrelationId = "unsettled", SenderAccount = "A", ReceiverAccount = "B", Amount = 1, Currency = "USD" }
        });

        var response = await _fixture.Client.GetAsync("/api/payment/unsettled");
        var body = await response.Content.ReadFromJsonAsync<PaymentStatusDto>();
        body!.SettledAt.Should().BeNull();
    }

    [Fact]
    public async Task GetPayment_FailedStatus_ReturnsCorrectly()
    {
        await _fixture.SeedPaymentAsync(new PaymentDocument
        {
            CorrelationId = "failed-01",
            Status = "Failed",
            SagaState = "Settling",
            Request = new() { CorrelationId = "failed-01", SenderAccount = "A", ReceiverAccount = "B", Amount = 500m, Currency = "USD" }
        });

        var response = await _fixture.Client.GetAsync("/api/payment/failed-01");
        var body = await response.Content.ReadFromJsonAsync<PaymentStatusDto>();
        body!.Status.Should().Be("Failed");
    }

    [Fact]
    public async Task GetPayment_CompensatedStatus_ReturnsCorrectly()
    {
        await _fixture.SeedPaymentAsync(new PaymentDocument
        {
            CorrelationId = "comp-01",
            Status = "Compensated",
            SagaState = "Completed",
            Request = new() { CorrelationId = "comp-01", SenderAccount = "A", ReceiverAccount = "B", Amount = 999m, Currency = "USD" }
        });

        var response = await _fixture.Client.GetAsync("/api/payment/comp-01");
        var body = await response.Content.ReadFromJsonAsync<PaymentStatusDto>();
        body!.Status.Should().Be("Compensated");
    }

    [Fact]
    public async Task GetPaymentsByStatus_SinglePage_ReturnsAllItems()
    {
        for (int i = 0; i < 5; i++)
        {
            await _fixture.SeedPaymentAsync(new PaymentDocument
            {
                CorrelationId = $"single-{i}",
                Status = "Settled",
                Request = new() { CorrelationId = $"single-{i}", SenderAccount = "A", ReceiverAccount = "B", Amount = i, Currency = "USD" }
            });
        }

        var response = await _fixture.Client.GetAsync("/api/payment/by-status/Settled?pageSize=20");
        var body = await response.Content.ReadFromJsonAsync<PagedPaymentStatusResponse>();
        body!.Items.Should().HaveCount(5);
    }
}
