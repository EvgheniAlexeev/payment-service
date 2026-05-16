// FILE: tests/.../ReaderService.IntegrationTests/PaymentReaderIntegrationTests.cs
// VERSION: 2.0.0
// MODULE: M-INTEGRATION
// PURPOSE: Test specification
// SEMANTIC_TAG: [TEST]
// START_MODULE M_INTEGRATION

// FILE: tests/.../ReaderService.IntegrationTests/PaymentReaderIntegrationTests.cs
// VERSION: 1.0.0

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PaymentService.Shared.Dtos;
using PaymentService.Shared.Models;

namespace PaymentService.Api.ReaderService.IntegrationTests;

/// <summary>
/// Integration tests for PaymentQueryController endpoints.
/// Tests HTTP layer end-to-end with real MongoDB via Testcontainers.
/// </summary>
public class PaymentReaderIntegrationTests : IClassFixture<ApiIntegrationFixture>
{
    private readonly ApiIntegrationFixture _fixture;

    public PaymentReaderIntegrationTests(ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    // ============================================
    // GET /api/payment/{correlationId} Tests
    // ============================================

    [Fact]
    public async Task GetPayment_ReturnsStatusOk_WhenPaymentExists()
    {
        // Arrange
        var payment = new PaymentDocument
        {
            CorrelationId = "test-read-1",
            Status = "Settled",
            SagaState = "Completed",
            Request = new PaymentRequestDto
            {
                CorrelationId = "test-read-1",
                SenderAccount = "ACC001",
                ReceiverAccount = "ACC002",
                Amount = 1000m,
                Currency = "USD"
            }
        };
        await _fixture.SeedPaymentAsync(payment);

        // Act
        var response = await _fixture.Client.GetAsync("/api/payment/test-read-1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentStatusDto>();
        body.Should().NotBeNull();
        body!.CorrelationId.Should().Be("test-read-1");
        body.Status.Should().Be("Settled");
        body.Amount.Should().Be(1000m);
        body.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task GetPayment_ReturnsNotFound_WhenPaymentDoesNotExist()
    {
        var response = await _fixture.Client.GetAsync("/api/payment/nonexistent");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPayment_ReturnsPendingStatus()
    {
        var payment = new PaymentDocument
        {
            CorrelationId = "test-pending",
            Status = "Pending",
            SagaState = "Validating",
            Request = new PaymentRequestDto
            {
                CorrelationId = "test-pending",
                SenderAccount = "SRC",
                ReceiverAccount = "DST",
                Amount = 500m,
                Currency = "EUR"
            }
        };
        await _fixture.SeedPaymentAsync(payment);

        var response = await _fixture.Client.GetAsync("/api/payment/test-pending");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentStatusDto>();
        body!.Status.Should().Be("Pending");
        body.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task GetPayment_ReturnsCorrectAmountAndCurrency()
    {
        var payment = new PaymentDocument
        {
            CorrelationId = "test-amount",
            Status = "Settled",
            Request = new PaymentRequestDto
            {
                CorrelationId = "test-amount",
                SenderAccount = "A1",
                ReceiverAccount = "A2",
                Amount = 12345.67m,
                Currency = "GBP"
            }
        };
        await _fixture.SeedPaymentAsync(payment);

        var response = await _fixture.Client.GetAsync("/api/payment/test-amount");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentStatusDto>();
        body!.Amount.Should().Be(12345.67m);
        body.Currency.Should().Be("GBP");
    }

    [Fact]
    public async Task GetPayment_ReturnsSettledAt_WhenSettled()
    {
        var settledAt = new DateTime(2026, 5, 1, 15, 30, 0, DateTimeKind.Utc);
        var payment = new PaymentDocument
        {
            CorrelationId = "test-settled",
            Status = "Settled",
            SagaState = "Completed",
            SettledAt = settledAt,
            Request = new PaymentRequestDto
            {
                CorrelationId = "test-settled",
                SenderAccount = "A1",
                ReceiverAccount = "A2",
                Amount = 100m,
                Currency = "USD"
            }
        };
        await _fixture.SeedPaymentAsync(payment);

        var response = await _fixture.Client.GetAsync("/api/payment/test-settled");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentStatusDto>();
        body!.SettledAt.Should().NotBeNull();
        body.SettledAt!.Value.Year.Should().Be(2026);
    }

    [Fact]
    public async Task GetPayment_ReturnsSagaState()
    {
        var payment = new PaymentDocument
        {
            CorrelationId = "test-saga",
            Status = "Enriching",
            SagaState = "Enriching",
            Request = new PaymentRequestDto
            {
                CorrelationId = "test-saga",
                SenderAccount = "SRC",
                ReceiverAccount = "DST",
                Amount = 250m,
                Currency = "USD"
            }
        };
        await _fixture.SeedPaymentAsync(payment);

        var response = await _fixture.Client.GetAsync("/api/payment/test-saga");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentStatusDto>();
        body!.SagaState.Should().Be("Enriching");
    }

    // ============================================
    // GET /api/payment/by-status/{status} Tests
    // ============================================

    [Fact]
    public async Task GetPaymentsByStatus_ReturnsFilteredResults()
    {
        await _fixture.SeedPaymentsAsync(
            new PaymentDocument
            {
                CorrelationId = "status-p1", Status = "Pending",
                Request = new() { CorrelationId = "status-p1", SenderAccount = "A", ReceiverAccount = "B", Amount = 1, Currency = "USD" }
            },
            new PaymentDocument
            {
                CorrelationId = "status-p2", Status = "Pending",
                Request = new() { CorrelationId = "status-p2", SenderAccount = "A", ReceiverAccount = "B", Amount = 2, Currency = "USD" }
            },
            new PaymentDocument
            {
                CorrelationId = "status-s1", Status = "Settled",
                Request = new() { CorrelationId = "status-s1", SenderAccount = "A", ReceiverAccount = "B", Amount = 3, Currency = "USD" }
            }
        );

        var response = await _fixture.Client.GetAsync("/api/payment/by-status/Pending");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedPaymentStatusResponse>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCount(2);
        body.Items.All(i => i.Status == "Pending").Should().BeTrue();
    }

    [Fact]
    public async Task GetPaymentsByStatus_NoResults_ReturnsEmptyList()
    {
        var response = await _fixture.Client.GetAsync("/api/payment/by-status/NonExistent");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedPaymentStatusResponse>();
        body!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPaymentsByStatus_WithPagination()
    {
        for (int i = 0; i < 15; i++)
        {
            await _fixture.SeedPaymentAsync(new PaymentDocument
            {
                CorrelationId = $"page-{i:D3}",
                Status = "Pending",
                Request = new()
                {
                    CorrelationId = $"page-{i:D3}",
                    SenderAccount = "A",
                    ReceiverAccount = "B",
                    Amount = i,
                    Currency = "USD"
                },
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        var page1 = await _fixture.Client.GetAsync("/api/payment/by-status/Pending?page=1&pageSize=10");
        var p1 = await page1.Content.ReadFromJsonAsync<PagedPaymentStatusResponse>();
        p1!.Items.Should().HaveCount(10);

        var page2 = await _fixture.Client.GetAsync("/api/payment/by-status/Pending?page=2&pageSize=10");
        var p2 = await page2.Content.ReadFromJsonAsync<PagedPaymentStatusResponse>();
        p2!.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetPaymentsByStatus_DefaultPageSize()
    {
        for (int i = 0; i < 25; i++)
        {
            await _fixture.SeedPaymentAsync(new PaymentDocument
            {
                CorrelationId = $"def-{i:D3}",
                Status = "Settled",
                Request = new()
                {
                    CorrelationId = $"def-{i:D3}",
                    SenderAccount = "A",
                    ReceiverAccount = "B",
                    Amount = i,
                    Currency = "USD"
                }
            });
        }

        var response = await _fixture.Client.GetAsync("/api/payment/by-status/Settled");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedPaymentStatusResponse>();
        body!.Items.Should().HaveCount(20); // Default pageSize=20
    }

    // ============================================
    // Edge Cases
    // ============================================

    [Fact]
    public async Task GetPayment_WithSpecialCharacters_ReturnsNotFound()
    {
        var response = await _fixture.Client.GetAsync("/api/payment/special%23char%21");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPayment_MultipleRequests_ConsistentResults()
    {
        var payment = new PaymentDocument
        {
            CorrelationId = "test-consistent",
            Status = "Pending",
            Request = new PaymentRequestDto
            {
                CorrelationId = "test-consistent",
                SenderAccount = "SRC",
                ReceiverAccount = "DST",
                Amount = 777m,
                Currency = "USD"
            }
        };
        await _fixture.SeedPaymentAsync(payment);

        for (int i = 0; i < 10; i++)
        {
            var response = await _fixture.Client.GetAsync("/api/payment/test-consistent");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<PaymentStatusDto>();
            body!.Amount.Should().Be(777m);
            body.Status.Should().Be("Pending");
        }
    }

    [Fact]
    public async Task GetPayment_LargeAmount_ReturnsCorrectly()
    {
        var payment = new PaymentDocument
        {
            CorrelationId = "test-large",
            Status = "Pending",
            Request = new PaymentRequestDto
            {
                CorrelationId = "test-large",
                SenderAccount = "SRC",
                ReceiverAccount = "DST",
                Amount = 999_999_999_999m,
                Currency = "USD"
            }
        };
        await _fixture.SeedPaymentAsync(payment);

        var response = await _fixture.Client.GetAsync("/api/payment/test-large");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentStatusDto>();
        body!.Amount.Should().Be(999_999_999_999m);
    }

    [Fact]
    public async Task GetPayment_MinimalAmount_ReturnsCorrectly()
    {
        var payment = new PaymentDocument
        {
            CorrelationId = "test-small",
            Status = "Pending",
            Request = new PaymentRequestDto
            {
                CorrelationId = "test-small",
                SenderAccount = "SRC",
                ReceiverAccount = "DST",
                Amount = 0.01m,
                Currency = "USD"
            }
        };
        await _fixture.SeedPaymentAsync(payment);

        var response = await _fixture.Client.GetAsync("/api/payment/test-small");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaymentStatusDto>();
        body!.Amount.Should().Be(0.01m);
    }

    [Fact]
    public async Task GetPayment_AllCurrencies_ReturnCorrectly()
    {
        foreach (var currency in new[] { "USD", "EUR", "GBP", "JPY", "CHF", "CAD", "AUD" })
        {
            var corrId = $"test-{currency}";
            await _fixture.SeedPaymentAsync(new PaymentDocument
            {
                CorrelationId = corrId,
                Status = "Pending",
                Request = new PaymentRequestDto
                {
                    CorrelationId = corrId,
                    SenderAccount = "A",
                    ReceiverAccount = "B",
                    Amount = 100m,
                    Currency = currency
                }
            });

            var response = await _fixture.Client.GetAsync($"/api/payment/{corrId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<PaymentStatusDto>();
            body!.Currency.Should().Be(currency);
        }
    }

    [Fact]
    public async Task GetPaymentsByStatus_Failed_ReturnsFailedPayments()
    {
        await _fixture.SeedPaymentsAsync(
            new PaymentDocument
            {
                CorrelationId = "fail-1", Status = "Failed",
                Request = new() { CorrelationId = "fail-1", SenderAccount = "A", ReceiverAccount = "B", Amount = 1, Currency = "USD" }
            },
            new PaymentDocument
            {
                CorrelationId = "fail-2", Status = "Failed",
                Request = new() { CorrelationId = "fail-2", SenderAccount = "A", ReceiverAccount = "B", Amount = 2, Currency = "USD" }
            }
        );

        var response = await _fixture.Client.GetAsync("/api/payment/by-status/Failed");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedPaymentStatusResponse>();
        body!.Items.Should().HaveCount(2);
    }
}
