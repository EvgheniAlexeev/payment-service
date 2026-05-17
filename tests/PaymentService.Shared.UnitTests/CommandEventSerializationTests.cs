// FILE: tests/PaymentService.Shared.UnitTests/CommandEventSerializationTests.cs
// VERSION: 2.0.0
// MODULE: M-TEST
// PURPOSE: Test specification
// SEMANTIC_TAG: [TEST]
// START_MODULE M_TEST

// FILE: tests/PaymentService.Shared.UnitTests/CommandEventSerializationTests.cs
// VERSION: 1.0.0

using System.Text.Json;
using FluentAssertions;
using PaymentService.Shared.Commands;
using PaymentService.Shared.Dtos;
using PaymentService.Shared.Events;

namespace PaymentService.Shared.UnitTests;

/// <summary>
/// Serialization tests for Commands and Events.
/// </summary>
public class CommandEventSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void PaymentCommand_RoundTripsJson()
    {
        var original = new PaymentCommand
        {
            IdempotencyKey = "key-123",
            CorrelationId = "corr-123",
            Request = new PaymentRequestDto
            {
                CorrelationId = "corr-123",
                SenderAccount = "ACC1",
                ReceiverAccount = "ACC2",
                Amount = 500m,
                Currency = "EUR"
            }
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PaymentCommand>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.IdempotencyKey.Should().Be("key-123");
        deserialized.CorrelationId.Should().Be("corr-123");
        deserialized.PaymentRequest.Amount.Should().Be(500m);
    }

    [Fact]
    public void PaymentSettled_RoundTripsJson()
    {
        var original = new PaymentSettled
        {
            CorrelationId = "corr-set",
            SettlementId = "SET-ABC",
            SettledAt = new DateTime(2026, 5, 1, 12, 0, 0),
            Status = "Settled"
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PaymentSettled>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.SettlementId.Should().Be("SET-ABC");
        deserialized.Status.Should().Be("Settled");
    }

    [Fact]
    public void PaymentEnriched_RoundTripsJson()
    {
        var original = new PaymentEnriched
        {
            CorrelationId = "corr-enr",
            SenderName = "Alice Corp",
            ReceiverName = "Bob Inc",
            EnrichedAt = new DateTime(2026, 5, 1, 10, 0, 0)
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PaymentEnriched>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.SenderName.Should().Be("Alice Corp");
        deserialized.ReceiverName.Should().Be("Bob Inc");
    }

    [Fact]
    public void PaymentFailed_RoundTripsJson()
    {
        var original = new PaymentFailed
        {
            CorrelationId = "corr-fail",
            FailedStep = "Settle",
            ErrorMessage = "Insufficient funds",
            ErrorCode = "ERR-FUNDS-001",
            RetryCount = 2,
            FailedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PaymentFailed>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.ErrorCode.Should().Be("ERR-FUNDS-001");
        deserialized.RetryCount.Should().Be(2);
    }

    [Fact]
    public void PaymentFailed_ZeroRetries_SerializesCorrectly()
    {
        var original = new PaymentFailed
        {
            CorrelationId = "first-fail",
            FailedStep = "Validate",
            ErrorMessage = "Missing data",
            ErrorCode = "VAL-001",
            RetryCount = 0
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        json.Should().Contain("\"retryCount\":0");
    }

    [Fact]
    public void MarkerInterfaces_AreCorrectlyTyped()
    {
        var command = new PaymentCommand();
        var evt = new PaymentSettled();

        command.Should().BeAssignableTo<ICommand>();
        evt.Should().BeAssignableTo<IEvent>();
    }
}
