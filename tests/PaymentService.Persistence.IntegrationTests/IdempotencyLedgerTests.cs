// FILE: tests/PaymentService.Persistence.IntegrationTests/IdempotencyLedgerTests.cs
// VERSION: 1.0.0

using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using PaymentService.Persistence.Repositories;

namespace PaymentService.Persistence.IntegrationTests;

/// <summary>
/// Integration tests for IdempotencyLedger using real MongoDB.
/// </summary>
public class IdempotencyLedgerTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;
    private readonly IIdempotencyLedger _ledger;

    public IdempotencyLedgerTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        var logger = A.Fake<ILogger<IdempotencyLedger>>();
        _ledger = new IdempotencyLedger(fixture.Context!, logger);
    }

    [Fact]
    public async Task TryMarkComplete_FirstAttempt_ReturnsTrue()
    {
        var result = await _ledger.TryMarkCompleteAsync("corr-idem-1", "Validate");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TryMarkComplete_DuplicateAttempt_ReturnsFalse()
    {
        await _ledger.TryMarkCompleteAsync("corr-idem-2", "Validate");

        var result = await _ledger.TryMarkCompleteAsync("corr-idem-2", "Validate");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryMarkComplete_DifferentSteps_SameCorrelationId_BothTrue()
    {
        var r1 = await _ledger.TryMarkCompleteAsync("corr-idem-3", "Validate");
        var r2 = await _ledger.TryMarkCompleteAsync("corr-idem-3", "Enrich");

        r1.Should().BeTrue();
        r2.Should().BeTrue();
    }

    [Fact]
    public async Task TryMarkComplete_SameStep_DifferentCorrelationIds_BothTrue()
    {
        var r1 = await _ledger.TryMarkCompleteAsync("corr-idem-4a", "Validate");
        var r2 = await _ledger.TryMarkCompleteAsync("corr-idem-4b", "Validate");

        r1.Should().BeTrue();
        r2.Should().BeTrue();
    }

    [Fact]
    public async Task IsStepComplete_Completed_ReturnsTrue()
    {
        await _ledger.TryMarkCompleteAsync("corr-idem-5", "Settle");

        var isComplete = await _ledger.IsStepCompleteAsync("corr-idem-5", "Settle");
        isComplete.Should().BeTrue();
    }

    [Fact]
    public async Task IsStepComplete_NotCompleted_ReturnsFalse()
    {
        var isComplete = await _ledger.IsStepCompleteAsync("corr-idem-6", "Settle");
        isComplete.Should().BeFalse();
    }

    [Fact]
    public async Task IsStepComplete_DifferentStep_ReturnsFalse()
    {
        await _ledger.TryMarkCompleteAsync("corr-idem-7", "Validate");

        var isComplete = await _ledger.IsStepCompleteAsync("corr-idem-7", "Settle");
        isComplete.Should().BeFalse();
    }

    [Fact]
    public async Task FullSagaLifecycle_AllStepsTracked()
    {
        var steps = new[] { "Validate", "Enrich", "Settle", "Notify" };

        foreach (var step in steps)
        {
            var result = await _ledger.TryMarkCompleteAsync("corr-saga", step);
            result.Should().BeTrue($"Step {step} should mark first time");
        }

        foreach (var step in steps)
        {
            var isComplete = await _ledger.IsStepCompleteAsync("corr-saga", step);
            isComplete.Should().BeTrue($"Step {step} should be complete");
        }

        // Idempotent replay: all steps should return false
        foreach (var step in steps)
        {
            var result = await _ledger.TryMarkCompleteAsync("corr-saga", step);
            result.Should().BeFalse($"Step {step} should be idempotent");
        }
    }

    [Fact]
    public async Task Concurrency_DuplicateKeys_OnlyOneSucceeds()
    {
        var tasks = new List<Task<bool>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_ledger.TryMarkCompleteAsync("corr-concurrent", "Validate"));
        }

        var results = await Task.WhenAll(tasks);
        results.Count(r => r).Should().Be(1, "Only one concurrent insert should succeed");
        results.Count(r => !r).Should().Be(9, "Nine should be duplicate-key rejections");
    }
}
