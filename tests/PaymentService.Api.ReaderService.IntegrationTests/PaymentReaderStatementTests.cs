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

        // Query first page (limit 3)
        var page1 = await _fixture.Client.GetAsync(
            $"/api/accounts/{accountId}/transactions?skip=0&limit=3");
        page1.StatusCode.Should().Be(HttpStatusCode.OK);
        var result1 = await page1.Content.ReadFromJsonAsync<GetTransactionsResponse>();
        result1!.Transactions.Should().HaveCount(3);
        result1.TotalCount.Should().Be(10);

        // Query second page
        var page2 = await _fixture.Client.GetAsync(
            $"/api/accounts/{accountId}/transactions?skip=3&limit=3");
        var result2 = await page2.Content.ReadFromJsonAsync<GetTransactionsResponse>();
        result2!.Transactions.Should().HaveCount(3);
        result2.TotalCount.Should().Be(10);

        // Verify pages are different
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
}
