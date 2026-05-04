// FILE: tests/PaymentService.Persistence.IntegrationTests/PaymentDocumentRepositoryTests.cs
// VERSION: 1.0.0

using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using PaymentService.Persistence.Repositories;
using PaymentService.Shared.Dtos;
using PaymentService.Shared.Models;

namespace PaymentService.Persistence.IntegrationTests;

/// <summary>
/// Integration tests for PaymentDocumentRepository using real MongoDB.
/// </summary>
public class PaymentDocumentRepositoryTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;
    private readonly IPaymentDocumentRepository _repository;

    public PaymentDocumentRepositoryTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        var logger = A.Fake<ILogger<PaymentDocumentRepository>>();
        _repository = new PaymentDocumentRepository(fixture.Context!, logger);
    }

    private PaymentDocument CreateTestPayment(string correlationId, string status = "Pending") =>
        new()
        {
            CorrelationId = correlationId,
            Status = status,
            SagaState = "Validating",
            CreatedAt = DateTime.UtcNow,
            Request = new PaymentRequestDto
            {
                CorrelationId = correlationId,
                SenderAccount = "ACC001",
                ReceiverAccount = "ACC002",
                Amount = 100m,
                Currency = "USD",
                Description = "Test payment"
            }
        };

    [Fact]
    public async Task InsertAndQuery_ReturnsPayment()
    {
        // Arrange
        var payment = CreateTestPayment("corr-1");

        // Act
        await _repository.InsertAsync(payment);
        var result = await _repository.GetByCorrelationIdAsync("corr-1");

        // Assert
        result.Should().NotBeNull();
        result!.CorrelationId.Should().Be("corr-1");
        result.Status.Should().Be("Pending");
        result.Request.Amount.Should().Be(100m);
    }

    [Fact]
    public async Task GetByCorrelationId_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByCorrelationIdAsync("nonexistent");
        result.Should().BeNull();
    }

    [Fact]
    public async Task Insert_DuplicateCorrelationId_ThrowsMongoWriteException()
    {
        var payment1 = CreateTestPayment("corr-dup");
        var payment2 = CreateTestPayment("corr-dup");

        await _repository.InsertAsync(payment1);

        await Assert.ThrowsAsync<MongoWriteException>(() =>
            _repository.InsertAsync(payment2));
    }

    [Fact]
    public async Task Update_Payment_ModifiesStatus()
    {
        var payment = CreateTestPayment("corr-update");
        await _repository.InsertAsync(payment);

        var updated = payment with { Status = "Settled", SettledAt = DateTime.UtcNow };
        await _repository.UpdateAsync(updated);

        var result = await _repository.GetByCorrelationIdAsync("corr-update");
        result!.Status.Should().Be("Settled");
        result.SettledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByStatus_FiltersByStatus()
    {
        await _repository.InsertAsync(CreateTestPayment("corr-p1", "Pending"));
        await _repository.InsertAsync(CreateTestPayment("corr-p2", "Pending"));
        await _repository.InsertAsync(CreateTestPayment("corr-s1", "Settled"));

        var pending = await _repository.GetByStatusAsync("Pending");
        var settled = await _repository.GetByStatusAsync("Settled");

        pending.Should().HaveCount(2);
        settled.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByStatus_PaginationWorks()
    {
        for (int i = 0; i < 25; i++)
        {
            await _repository.InsertAsync(CreateTestPayment($"corr-page-{i}", "Pending"));
        }

        var page1 = await _repository.GetByStatusAsync("Pending", skip: 0, limit: 10);
        var page2 = await _repository.GetByStatusAsync("Pending", skip: 10, limit: 10);
        var page3 = await _repository.GetByStatusAsync("Pending", skip: 20, limit: 10);

        page1.Should().HaveCount(10);
        page2.Should().HaveCount(10);
        page3.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetByStatus_EmptyResult_ReturnsEmptyList()
    {
        var result = await _repository.GetByStatusAsync("NonExistent");
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBatch_RetrievesSpecifiedCorrelationIds()
    {
        await _repository.InsertAsync(CreateTestPayment("corr-b1"));
        await _repository.InsertAsync(CreateTestPayment("corr-b2"));
        await _repository.InsertAsync(CreateTestPayment("corr-b3"));

        var results = await _repository.GetBatchAsync(
            new List<string> { "corr-b1", "corr-b3" });

        results.Should().HaveCount(2);
        results.Select(p => p.CorrelationId).Should().Contain(new[] { "corr-b1", "corr-b3" });
        results.Select(p => p.CorrelationId).Should().NotContain("corr-b2");
    }

    [Fact]
    public async Task GetBatch_EmptyList_ReturnsEmpty()
    {
        var results = await _repository.GetBatchAsync(new List<string>());
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ExistsByCorrelationId_Existing_ReturnsTrue()
    {
        await _repository.InsertAsync(CreateTestPayment("corr-exists"));

        var exists = await _repository.ExistsByCorrelationIdAsync("corr-exists");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByCorrelationId_NotExisting_ReturnsFalse()
    {
        var exists = await _repository.ExistsByCorrelationIdAsync("corr-no-exist");
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task InsertAndQuery_MultiplePayments_AllRetrievable()
    {
        var ids = new List<string>();
        for (int i = 0; i < 20; i++)
        {
            var id = $"corr-multi-{i}";
            ids.Add(id);
            await _repository.InsertAsync(new PaymentDocument
            {
                CorrelationId = id,
                Status = i % 2 == 0 ? "Pending" : "Settled",
                SagaState = "Completed",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                Request = new PaymentRequestDto
                {
                    CorrelationId = id,
                    Amount = 100m * (i + 1),
                    Currency = i % 3 == 0 ? "EUR" : "USD",
                    SenderAccount = $"S{i}",
                    ReceiverAccount = $"R{i}"
                }
            });
        }

        var batch = await _repository.GetBatchAsync(ids);
        batch.Should().HaveCount(20);

        foreach (var id in ids)
        {
            var single = await _repository.GetByCorrelationIdAsync(id);
            single.Should().NotBeNull();
            single!.CorrelationId.Should().Be(id);
        }
    }

    [Fact]
    public async Task PaymentDocument_PreservesAllFields()
    {
        var original = new PaymentDocument
        {
            CorrelationId = "corr-full",
            Status = "Pending",
            SagaState = "Validating",
            CreatedAt = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc),
            Request = new PaymentRequestDto
            {
                CorrelationId = "corr-full",
                SenderAccount = "SRC-ACC-001",
                ReceiverAccount = "DST-ACC-002",
                Amount = 1234.56m,
                Currency = "CHF",
                ValueDate = new DateTime(2026, 6, 1),
                Description = "Payment for services rendered"
            }
        };

        await _repository.InsertAsync(original);

        var result = await _repository.GetByCorrelationIdAsync("corr-full");
        result.Should().NotBeNull();
        result!.SenderAccount().Should().Be("SRC-ACC-001");
        result.Status.Should().Be("Pending");
        result.Request.Amount.Should().Be(1234.56m);
        result.Request.Currency.Should().Be("CHF");
        result.Request.ValueDate.Should().Be(new DateTime(2026, 6, 1));
    }
}

// Extension for cleaner test assertions (avoids accessing Request.SenderAccount everywhere)
internal static class PaymentDocumentExtensions
{
    public static string SenderAccount(this PaymentDocument doc) => doc.Request.SenderAccount;
    public static string ReceiverAccount(this PaymentDocument doc) => doc.Request.ReceiverAccount;
}
