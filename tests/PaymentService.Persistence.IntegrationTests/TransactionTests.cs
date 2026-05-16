// FILE: tests/.../Persistence.IntegrationTests/TransactionTests.cs
// VERSION: 2.0.0
// MODULE: M-INTEGRATION
// PURPOSE: Test specification
// SEMANTIC_TAG: [TEST]
// START_MODULE M_INTEGRATION

// FILE: tests/.../Persistence.IntegrationTests/TransactionTests.cs
// VERSION: 1.0.0

using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using PaymentService.Persistence.Repositories;
using PaymentService.Shared.Dtos;
using PaymentService.Shared.Models;
using PaymentService.Persistence.MongoDB;

namespace PaymentService.Persistence.IntegrationTests;

/// <summary>
/// MongoDB transaction/integration tests.
/// </summary>
public class TransactionTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;
    private readonly IPaymentDocumentRepository _paymentRepo;
    private readonly IIdempotencyLedger _idempotency;
    private readonly IMongoClient _client;

    public TransactionTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        var pLogger = A.Fake<ILogger<PaymentDocumentRepository>>();
        var iLogger = A.Fake<ILogger<IdempotencyLedger>>();
        _paymentRepo = new PaymentDocumentRepository(fixture.Context!, pLogger);
        _idempotency = new IdempotencyLedger(fixture.Context!, iLogger);
        _client = new MongoClient(((MongoClient)fixture.Database!.Client.GetType()
            .GetProperty("Settings")!.GetValue(null)!).ToString() ?? string.Empty);

        // Using the same client from fixture
        _client = fixture.Database!.Client;
    }

    [Fact]
    public async Task InsertAndQuery_PaymentWithNullSagaState()
    {
        var payment = new PaymentDocument
        {
            CorrelationId = "txn-null-saga",
            Status = "Pending",
            SagaState = "None",
            Request = new PaymentRequestDto
            {
                CorrelationId = "txn-null-saga",
                SenderAccount = "SRC",
                ReceiverAccount = "DST",
                Amount = 100m,
                Currency = "USD"
            }
        };

        await _paymentRepo.InsertAsync(payment);

        var result = await _paymentRepo.GetByCorrelationIdAsync("txn-null-saga");
        result!.SagaState.Should().Be("None");
    }

    [Fact]
    public async Task BatchRetrieval_EmptyIds_ReturnsEmptyList()
    {
        await _paymentRepo.InsertAsync(new PaymentDocument
        {
            CorrelationId = "batch-empty-src",
            Status = "Pending",
            Request = new() { CorrelationId = "batch-empty-src", SenderAccount = "A", ReceiverAccount = "B", Amount = 1, Currency = "USD" }
        });

        var results = await _paymentRepo.GetBatchAsync(new List<string>());
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task BatchRetrieval_AllMissing_ReturnsEmpty()
    {
        var results = await _paymentRepo.GetBatchAsync(
            new List<string> { "missing-1", "missing-2" });
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task BatchRetrieval_PartialMatch_ReturnsOnlyFound()
    {
        await _paymentRepo.InsertAsync(new PaymentDocument
        {
            CorrelationId = "partial-1",
            Status = "Pending",
            Request = new() { CorrelationId = "partial-1", SenderAccount = "A", ReceiverAccount = "B", Amount = 1, Currency = "USD" }
        });

        var results = await _paymentRepo.GetBatchAsync(
            new List<string> { "partial-1", "missing" });

        results.Should().HaveCount(1);
        results[0].CorrelationId.Should().Be("partial-1");
    }

    [Fact]
    public async Task Update_NonexistentPayment_DoesNotThrow()
    {
        var payment = new PaymentDocument
        {
            CorrelationId = "no-exist-update",
            Status = "Settled",
            Request = new() { CorrelationId = "no-exist-update", SenderAccount = "A", ReceiverAccount = "B", Amount = 1, Currency = "USD" }
        };

        await _paymentRepo.UpdateAsync(payment); // Should not throw; ReplaceOneAsync is upsert=false by default

        var exists = await _paymentRepo.ExistsByCorrelationIdAsync("no-exist-update");
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Idempotency_ConcurrentMark_ReturnsFalseForAllButOne()
    {
        var tasks = new List<Task<bool>>();
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(_idempotency.TryMarkCompleteAsync("concurrent-20", "Validate"));
        }

        var results = await Task.WhenAll(tasks);
        results.Count(r => r).Should().Be(1);
    }

    [Fact]
    public async Task Idempotency_MultiStep_MultiCorrelation()
    {
        var steps = new[] { "Validate", "Enrich", "Settle", "Notify" };
        var corrIds = new[] { "multi-a", "multi-b", "multi-c" };

        foreach (var corrId in corrIds)
        foreach (var step in steps)
        {
            var mark = await _idempotency.TryMarkCompleteAsync(corrId, step);
            mark.Should().BeTrue();
        }

        foreach (var corrId in corrIds)
        foreach (var step in steps)
        {
            var isComplete = await _idempotency.IsStepCompleteAsync(corrId, step);
            isComplete.Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetByStatus_EmptyString_ReturnsNothing()
    {
        await _paymentRepo.InsertAsync(new PaymentDocument
        {
            CorrelationId = "empty-status-test",
            Status = "Pending",
            Request = new() { CorrelationId = "empty-status-test", SenderAccount = "A", ReceiverAccount = "B", Amount = 1, Currency = "USD" }
        });

        var result = await _paymentRepo.GetByStatusAsync("");
        result.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetByStatus_CaseSensitive()
    {
        await _paymentRepo.InsertAsync(new PaymentDocument
        {
            CorrelationId = "case-sensitive",
            Status = "Pending",
            Request = new() { CorrelationId = "case-sensitive", SenderAccount = "A", ReceiverAccount = "B", Amount = 1, Currency = "USD" }
        });

        var lower = await _paymentRepo.GetByStatusAsync("pending");
        lower.Count.Should().Be(0, "MongoDB queries are case-sensitive");

        var upper = await _paymentRepo.GetByStatusAsync("Pending");
        upper.Count.Should().Be(1);
    }

    [Fact]
    public async Task PaymentDocument_WithZeroAmount_PersistsCorrectly()
    {
        var payment = new PaymentDocument
        {
            CorrelationId = "zero-amount",
            Status = "Pending",
            Request = new PaymentRequestDto
            {
                CorrelationId = "zero-amount",
                SenderAccount = "A",
                ReceiverAccount = "B",
                Amount = 0m,
                Currency = "USD"
            }
        };

        await _paymentRepo.InsertAsync(payment);
        var result = await _paymentRepo.GetByCorrelationIdAsync("zero-amount");
        result!.Request.Amount.Should().Be(0);
    }
}
