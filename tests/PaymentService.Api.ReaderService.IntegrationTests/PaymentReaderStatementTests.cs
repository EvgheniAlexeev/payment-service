// FILE: tests/.../ReaderService.IntegrationTests/PaymentReaderStatementTests.cs
// VERSION: 2.0.0
// MODULE: M-INTEGRATION
// PURPOSE: Test specification for account statement endpoint
// SEMANTIC_TAG: [TEST]
// START_MODULE M_INTEGRATION

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PaymentService.Api.ReaderService.Features.GetTransactions;
using PaymentService.Shared.Dtos;
using PaymentService.Shared.Models;

namespace PaymentService.Api.ReaderService.IntegrationTests;

/// <summary>
/// Integration tests for GET /api/accounts/{accountId}/transactions endpoint.
/// Tests account statement query across sender and receiver scenarios.
/// </summary>
public class PaymentReaderStatementTests : IClassFixture<ApiIntegrationFixture>
{
    private readonly ApiIntegrationFixture _fixture;

    public PaymentReaderStatementTests(ApiIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    // ──────────────── Helper ────────────────

    private PaymentDocument CreatePayment(
        string correlationId,
        string sender,
        string receiver,
        decimal amount,
        string status = "Settled",
        DateTime? createdAt = null)
    {
        return new PaymentDocument
        {
            CorrelationId = correlationId,
            Request = new PaymentRequestDto
            {
                CorrelationId = correlationId,
                SenderAccount = sender,
                ReceiverAccount = receiver,
                Amount = amount,
                Currency = "USD",
                ValueDate = DateTime.UtcNow
            },
            Status = status,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }

    // ──────────────── Query by Sender ────────────────

    [Fact]
    public async Task GetTransactions_AsSender_ReturnsOutgoingPayments()
    {
        var accountId = "ACC-SENDER-001";
        var receiverId = "ACC-RECV-001";

        await _fixture.SeedPaymentAsync(CreatePayment(
            "TXN-SEND-001", accountId, receiverId, 500m));

        var response = await _fixture.Client.GetAsync(
            $"/api/accounts/{accountId}/transactions?skip=0&limit=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetTransactionsResponse>();
        result.Should().NotBeNull();
        result!.Transactions.Should().ContainSingle();
        result.TotalCount.Should().Be(1);
        result.AccountId.Should().Be(accountId);

        var txn = result.Transactions[0];
        txn.Direction.Should().Be("outgoing");
        txn.CounterpartyAccount.Should().Be(receiverId);
        txn.CorrelationId.Should().Be("TXN-SEND-001");
        txn.Amount.Should().Be(500m);
    }

    // ──────────────── Query by Receiver ────────────────

    [Fact]
    public async Task GetTransactions_AsReceiver_ReturnsIncomingPayments()
    {
        var senderId = "ACC-SENDER-002";
        var accountId = "ACC-RECV-002";

        await _fixture.SeedPaymentAsync(CreatePayment(
            "TXN-RECV-001", senderId, accountId, 750m));

        var response = await _fixture.Client.GetAsync(
            $"/api/accounts/{accountId}/transactions?skip=0&limit=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetTransactionsResponse>();
        result.Should().NotBeNull();
        result!.Transactions.Should().ContainSingle();
        result.TotalCount.Should().Be(1);

        var txn = result.Transactions[0];
        txn.Direction.Should().Be("incoming");
        txn.CounterpartyAccount.Should().Be(senderId);
    }

    // ──────────────── Both Sender and Receiver ────────────────

    [Fact]
    public async Task GetTransactions_AccountInBothRoles_ReturnsAll()
    {
        var accountId = "ACC-BOTH-001";

        await _fixture.SeedPaymentsAsync(
            CreatePayment("TXN-BOTH-01", accountId, "OTHER-1", 100m),
            CreatePayment("TXN-BOTH-02", "OTHER-2", accountId, 200m),
            CreatePayment("TXN-BOTH-03", accountId, "OTHER-3", 300m));

        var response = await _fixture.Client.GetAsync(
            $"/api/accounts/{accountId}/transactions?skip=0&limit=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetTransactionsResponse>();
        result.Should().NotBeNull();
        result!.Transactions.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
    }

    // ──────────────── Pagination ────────────────

    [Fact]
    public async Task GetTransactions_WithPagination_ReturnsCorrectPage()
    {
        var accountId = "ACC-PAGE-001";
        var payments = Enumerable.Range(1, 10)
            .Select(i => CreatePayment(
                $"TXN-PAGE-{i:D3}", accountId, $"OTHER-{i}", i * 100m))
            .ToArray();

        await _fixture.SeedPaymentsAsync(payments);

        var page1 = await _fixture.Client.GetAsync(
            $"/api/accounts/{accountId}/transactions?skip=0&limit=3");
        page1.StatusCode.Should().Be(HttpStatusCode.OK);
        var result1 = await page1.Content.ReadFromJsonAsync<GetTransactionsResponse>();
        result1!.Transactions.Should().HaveCount(3);
        result1.TotalCount.Should().Be(10);

        var page2 = await _fixture.Client.GetAsync(
            $"/api/accounts/{accountId}/transactions?skip=3&limit=3");
        var result2 = await page2.Content.ReadFromJsonAsync<GetTransactionsResponse>();
        result2!.Transactions.Should().HaveCount(3);
        result2.TotalCount.Should().Be(10);

        result1.Transactions[0].CorrelationId
            .Should().NotBe(result2.Transactions[0].CorrelationId);
    }

    // ──────────────── Empty Account ────────────────

    [Fact]
    public async Task GetTransactions_EmptyAccount_ReturnsEmptyArray()
    {
        var response = await _fixture.Client.GetAsync(
            "/api/accounts/ACC-EMPTY-001/transactions?skip=0&limit=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetTransactionsResponse>();
        result.Should().NotBeNull();
        result!.Transactions.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ──────────────── Invalid Request ────────────────

    [Fact]
    public async Task GetTransactions_InvalidAccountId_Returns400()
    {
        var response = await _fixture.Client.GetAsync(
            "/api/accounts/%20/transactions?skip=0&limit=20");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ──────────────── Sorting ────────────────

    [Fact]
    public async Task GetTransactions_ReturnsSortedByDateDescending()
    {
        var accountId = "ACC-SORT-001";
        var oldDate = DateTime.UtcNow.AddDays(-5);
        var midDate = DateTime.UtcNow.AddDays(-2);
        var newDate = DateTime.UtcNow;

        await _fixture.SeedPaymentsAsync(
            CreatePayment("TXN-SORT-01", accountId, "OTHER", 100m,
                createdAt: oldDate),
            CreatePayment("TXN-SORT-02", accountId, "OTHER", 200m,
                createdAt: midDate),
            CreatePayment("TXN-SORT-03", accountId, "OTHER", 300m,
                createdAt: newDate));

        var response = await _fixture.Client.GetAsync(
            $"/api/accounts/{accountId}/transactions?skip=0&limit=20");

        var result = await response.Content.ReadFromJsonAsync<GetTransactionsResponse>();
        result!.Transactions.Should().HaveCount(3);
        result.Transactions[0].CorrelationId.Should().Be("TXN-SORT-03");
        result.Transactions[1].CorrelationId.Should().Be("TXN-SORT-02");
        result.Transactions[2].CorrelationId.Should().Be("TXN-SORT-01");
    }

    // ──────────────── Date Range: Default (7 days) ────────────────

    [Fact]
    public async Task GetTransactions_DefaultDateRange_Last7Days()
    {
        var accountId = "ACC-DATE-7D";
        var now = DateTime.UtcNow;

        // Within last 7 days (should be returned by default)
        await _fixture.SeedPaymentAsync(CreatePayment(
            "TXN-7D-WITHIN", accountId, "OTHER", 100m,
            createdAt: now.AddDays(-3)));

        // Outside last 7 days (should NOT be returned by default)
        await _fixture.SeedPaymentAsync(CreatePayment(
            "TXN-7D-OLD", accountId, "OTHER", 200m,
            createdAt: now.AddDays(-10)));

        // No date params → defaults to last 7 days
        var response = await _fixture.Client.GetAsync(
            $"/api/accounts/{accountId}/transactions?skip=0&limit=20");

        var result = await response.Content.ReadFromJsonAsync<GetTransactionsResponse>();
        result!.Transactions.Should().ContainSingle();
        result.Transactions[0].CorrelationId.Should().Be("TXN-7D-WITHIN");
    }

    // ──────────────── Date Range: Explicit period ────────────────

    [Fact]
    public async Task GetTransactions_ExplicitDateRange_FiltersCorrectly()
    {
        var accountId = "ACC-DATE-EXP";
        var baseDate = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        await _fixture.SeedPaymentsAsync(
            CreatePayment("TXN-EXP-01", accountId, "OTHER", 100m,
                createdAt: baseDate.AddDays(-5)),  // May 27
            CreatePayment("TXN-EXP-02", accountId, "OTHER", 200m,
                createdAt: baseDate),               // Jun 1
            CreatePayment("TXN-EXP-03", accountId, "OTHER", 300m,
                createdAt: baseDate.AddDays(5)));   // Jun 6

        // Query range: Jun 1 - Jun 6
        var response = await _fixture.Client.GetAsync(
            $"/api/accounts/{accountId}/transactions" +
            $"?dateFrom=2026-06-01&dateTo=2026-06-06&skip=0&limit=20");

        var result = await response.Content.ReadFromJsonAsync<GetTransactionsResponse>();
        result!.Transactions.Should().HaveCount(2);
        result.Transactions.Should().Contain(t => t.CorrelationId == "TXN-EXP-02");
        result.Transactions.Should().Contain(t => t.CorrelationId == "TXN-EXP-03");
    }

    // ──────────────── Date Range: DateFrom only → to current ────────────────

    [Fact]
    public async Task GetTransactions_DateFromOnly_DefaultsDateToNow()
    {
        var accountId = "ACC-DATE-FROM";
        var now = DateTime.UtcNow;

        await _fixture.SeedPaymentsAsync(
            CreatePayment("TXN-FROM-01", accountId, "OTHER", 100m,
                createdAt: now.AddDays(-1)),
            CreatePayment("TXN-FROM-02", accountId, "OTHER", 200m,
                createdAt: now.AddDays(-60))); // outside range

        var response = await _fixture.Client.GetAsync(
            $"/api/accounts/{accountId}/transactions" +
            $"?dateFrom={now.AddDays(-7):yyyy-MM-dd}&skip=0&limit=20");

        var result = await response.Content.ReadFromJsonAsync<GetTransactionsResponse>();
        result!.Transactions.Should().ContainSingle();
        result.Transactions[0].CorrelationId.Should().Be("TXN-FROM-01");
    }

    // ──────────────── Date Range: DateTo only → from Jan 1 ────────────────

    [Fact]
    public async Task GetTransactions_DateToOnly_DefaultsFromJan1()
    {
        var accountId = "ACC-DATE-TO";
        var thisYear = DateTime.UtcNow.Year;

        await _fixture.SeedPaymentsAsync(
            CreatePayment("TXN-TO-01", accountId, "OTHER", 100m,
                createdAt: new DateTime(thisYear, 3, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreatePayment("TXN-TO-02", accountId, "OTHER", 200m,
                createdAt: new DateTime(thisYear - 1, 12, 1, 0, 0, 0, DateTimeKind.Utc))); // last year

        var response = await _fixture.Client.GetAsync(
            $"/api/accounts/{accountId}/transactions" +
            $"?dateTo={DateTime.UtcNow:yyyy-MM-dd}&skip=0&limit=20");

        var result = await response.Content.ReadFromJsonAsync<GetTransactionsResponse>();
        result!.Transactions.Should().ContainSingle();
        result.Transactions[0].CorrelationId.Should().Be("TXN-TO-01");
    }

    // ──────────────── Date Range: Exceeds year ────────────────

    [Fact]
    public async Task GetTransactions_DateRangeExceedsYear_Returns400()
    {
        var accountId = "ACC-DATE-OVER";
        var daysInYear = DateTime.IsLeapYear(2026) ? 366 : 365;

        var response = await _fixture.Client.GetAsync(
            $"/api/accounts/{accountId}/transactions" +
            $"?dateFrom=2026-01-01&dateTo=2026-12-31&skip=0&limit=20");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ──────────────── Date Range: DateFrom > DateTo ────────────────

    [Fact]
    public async Task GetTransactions_DateFromAfterDateTo_Returns400()
    {
        var accountId = "ACC-DATE-INV";

        var response = await _fixture.Client.GetAsync(
            $"/api/accounts/{accountId}/transactions" +
            $"?dateFrom=2026-06-10&dateTo=2026-06-01&skip=0&limit=20");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
