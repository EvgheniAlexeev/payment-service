// FILE: tests/.../ReaderService.IntegrationTests/PaymentReaderEdgeCaseTests.cs
// VERSION: 1.0.0

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PaymentService.Shared.Dtos;
using PaymentService.Shared.Models;

namespace PaymentService.Api.ReaderService.IntegrationTests;

/// <summary>
/// Additional edge case tests for the reader API.
/// </summary>
public class PaymentReaderEdgeCaseTests : IClassFixture<ApiIntegrationFixture>
{
    private readonly ApiIntegrationFixture _fixture;

    public PaymentReaderEdgeCaseTests(ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetPayment_EmptyCorrelationId_ReturnsNotFound()
    {
        // Empty path segment may give 404 or 405 depending on routing
        var response = await _fixture.Client.GetAsync("/api/payment/");
        // Should not be 200 OK
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPayment_LongCorrelationId_ReturnsOk()
    {
        var id = new string('a', 100);
        await _fixture.SeedPaymentAsync(new PaymentDocument
        {
            CorrelationId = id,
            Status = "Pending",
            Request = new PaymentRequestDto { CorrelationId = id, SenderAccount = "A", ReceiverAccount = "B", Amount = 1, Currency = "USD" }
        });

        var response = await _fixture.Client.GetAsync($"/api/payment/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPayment_UnicodeCorrelationId_ReturnsOk()
    {
        var id = "тест-оплата-001";
        await _fixture.SeedPaymentAsync(new PaymentDocument
        {
            CorrelationId = id,
            Status = "Pending",
            Request = new PaymentRequestDto { CorrelationId = id, SenderAccount = "A", ReceiverAccount = "B", Amount = 100m, Currency = "USD" }
        });

        var response = await _fixture.Client.GetAsync($"/api/payment/{Uri.EscapeDataString(id)}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentStatusDto>();
        body!.CorrelationId.Should().Be(id);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Validating")]
    [InlineData("Enriching")]
    [InlineData("Settling")]
    [InlineData("Settled")]
    [InlineData("Failed")]
    [InlineData("Compensated")]
    public async Task GetAllStatuses_ReturnsCorrectly(string status)
    {
        var id = $"status-{status}";
        await _fixture.SeedPaymentAsync(new PaymentDocument
        {
            CorrelationId = id,
            Status = status,
            Request = new PaymentRequestDto { CorrelationId = id, SenderAccount = "A", ReceiverAccount = "B", Amount = 100m, Currency = "USD" }
        });

        var response = await _fixture.Client.GetAsync($"/api/payment/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentStatusDto>();
        body!.Status.Should().Be(status);
    }

    [Fact]
    public async Task GetPaymentsByStatus_PageZero_ReturnsEmptyOrFirstPage()
    {
        var response = await _fixture.Client.GetAsync("/api/payment/by-status/Pending?page=0&pageSize=5");
        // Should handle gracefully - exact behavior depends on handler's Math.Max(skip, 0)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPaymentsByStatus_NegativePage_ReturnOk()
    {
        var response = await _fixture.Client.GetAsync("/api/payment/by-status/Pending?page=-1&pageSize=5");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
