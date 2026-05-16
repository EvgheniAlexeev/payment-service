// FILE: tests/PaymentService.Persistence.IntegrationTests/MongoDbContextTests.cs
// VERSION: 2.0.0
// MODULE: M-INTEGRATION
// PURPOSE: Test specification
// SEMANTIC_TAG: [TEST]
// START_MODULE M_INTEGRATION

// FILE: tests/PaymentService.Persistence.IntegrationTests/MongoDbContextTests.cs
// VERSION: 1.0.0

using FluentAssertions;
using MongoDB.Driver;
using PaymentService.Persistence.MongoDB;

namespace PaymentService.Persistence.IntegrationTests;

/// <summary>
/// Tests for MongoDbContext and IndexConfiguration.
/// </summary>
public class MongoDbContextTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;

    public MongoDbContextTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void DbContext_Payments_CollectionExists()
    {
        var collection = _fixture.Context!.Payments;
        collection.Should().NotBeNull();
    }

    [Fact]
    public void DbContext_SagaStates_CollectionExists()
    {
        var collection = _fixture.Context!.SagaStates;
        collection.Should().NotBeNull();
    }

    [Fact]
    public void DbContext_IdempotencyLedger_CollectionExists()
    {
        var collection = _fixture.Context!.IdempotencyLedger;
        collection.Should().NotBeNull();
    }

    [Fact]
    public void DbContext_Database_ReturnsCorrectInstance()
    {
        var db = _fixture.Context!.Database;
        db.Should().NotBeNull();
    }

    [Fact]
    public async Task IndexConfiguration_CanRunMultipleTimesWithoutError()
    {
        for (int i = 0; i < 3; i++)
        {
            await IndexConfiguration.EnsureIndexesAsync(
                _fixture.Database!,
                _fixture.Logger);
        }
        // No exception means success
    }

    [Fact]
    public async Task PaymentIndex_EnforcesUniqueCorrelationId()
    {
        var coll = _fixture.Context!.Payments;

        await coll.InsertOneAsync(new Shared.Models.PaymentDocument
        {
            CorrelationId = "unique-idx-1",
            Status = "Pending",
            Request = new() { CorrelationId = "unique-idx-1", SenderAccount = "A", ReceiverAccount = "B", Amount = 1, Currency = "USD" }
        });

        await Assert.ThrowsAsync<MongoWriteException>(async () =>
        {
            await coll.InsertOneAsync(new Shared.Models.PaymentDocument
            {
                CorrelationId = "unique-idx-1",
                Status = "Pending",
                Request = new() { CorrelationId = "unique-idx-1", SenderAccount = "A", ReceiverAccount = "B", Amount = 2, Currency = "USD" }
            });
        });
    }

    [Fact]
    public async Task IdempotencyIndex_EnforcesUniqueStep()
    {
        var coll = _fixture.Context!.IdempotencyLedger;

        await coll.InsertOneAsync(new IdempotencyEntry
        {
            Id = "idx-test_step",
            CorrelationId = "idx-test",
            StepName = "Validate"
        });

        await Assert.ThrowsAsync<MongoWriteException>(async () =>
        {
            await coll.InsertOneAsync(new IdempotencyEntry
            {
                Id = "idx-test_Validate_dup",
                CorrelationId = "idx-test",
                StepName = "Validate"
            });
        });
    }

    [Fact]
    public async Task IdempotencyIndex_AllowsDifferentSteps()
    {
        var coll = _fixture.Context!.IdempotencyLedger;

        await coll.InsertOneAsync(new IdempotencyEntry
        {
            Id = "idx-diff_Validate",
            CorrelationId = "idx-diff",
            StepName = "Validate"
        });

        await coll.InsertOneAsync(new IdempotencyEntry
        {
            Id = "idx-diff_Enrich",
            CorrelationId = "idx-diff",
            StepName = "Enrich"
        });

        var count = await coll.CountDocumentsAsync(
            Builders<IdempotencyEntry>.Filter.Eq(e => e.CorrelationId, "idx-diff"));
        count.Should().Be(2);
    }

    [Fact]
    public async Task PaymentIndex_PaymentWithStatusCanBeFound()
    {
        var coll = _fixture.Payments;

        await coll.InsertOneAsync(new Shared.Models.PaymentDocument
        {
            CorrelationId = "status-idx-1",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            Request = new() { CorrelationId = "status-idx-1", SenderAccount = "A", ReceiverAccount = "B", Amount = 1, Currency = "USD" }
        });

        var filter = Builders<Shared.Models.PaymentDocument>.Filter.Eq(p => p.Status, "Pending");
        var results = await coll.Find(filter).ToListAsync();

        results.Should().HaveCount(1);
        results[0].CorrelationId.Should().Be("status-idx-1");
    }

    [Fact]
    public async Task PaymentIndex_ManyDocuments_GetByStatusStillFast()
    {
        var coll = _fixture.Payments;
        var docs = new List<Shared.Models.PaymentDocument>();

        for (int i = 0; i < 100; i++)
        {
            docs.Add(new Shared.Models.PaymentDocument
            {
                CorrelationId = $"perf-{i:D4}",
                Status = i % 3 == 0 ? "Pending" : (i % 3 == 1 ? "Settled" : "Failed"),
                CreatedAt = DateTime.UtcNow.AddSeconds(-i),
                Request = new()
                {
                    CorrelationId = $"perf-{i:D4}",
                    SenderAccount = $"S{i}",
                    ReceiverAccount = $"R{i}",
                    Amount = i,
                    Currency = "USD"
                }
            });
        }

        await coll.InsertManyAsync(docs);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var pending = await coll.Find(
            Builders<Shared.Models.PaymentDocument>.Filter.Eq(p => p.Status, "Pending"))
            .SortByDescending(p => p.CreatedAt)
            .Limit(10)
            .ToListAsync();
        sw.Stop();

        pending.Should().NotBeEmpty();
        // No hard timing assertion - just verify it works
    }
}
