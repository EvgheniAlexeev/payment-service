// FILE: tests/PaymentService.Shared.UnitTests/DtoSerializationTests.cs
// VERSION: 2.0.0
// MODULE: M-TEST
// PURPOSE: Test specification
// SEMANTIC_TAG: [TEST]
// START_MODULE M_TEST

// FILE: tests/PaymentService.Shared.UnitTests/DtoSerializationTests.cs
// VERSION: 1.0.0

using System.Text.Json;
using FluentAssertions;
using PaymentService.Shared.Dtos;

namespace PaymentService.Shared.UnitTests;

/// <summary>
/// JSON serialization round-trip tests for all DTOs.
/// </summary>
public class DtoSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void PaymentRequestDto_RoundTripsJson()
    {
        var original = new PaymentRequestDto
        {
            CorrelationId = "corr-123",
            SenderAccount = "ACC001",
            ReceiverAccount = "ACC002",
            Amount = 1000.50m,
            Currency = "USD",
            ValueDate = new DateTime(2026, 6, 1),
            Description = "Invoice payment"
        };

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PaymentRequestDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.CorrelationId.Should().Be("corr-123");
        deserialized.SenderAccount.Should().Be("ACC001");
        deserialized.ReceiverAccount.Should().Be("ACC002");
        deserialized.Amount.Should().Be(1000.50m);
        deserialized.Currency.Should().Be("USD");
        deserialized.ValueDate.Should().Be(new DateTime(2026, 6, 1));
        deserialized.Description.Should().Be("Invoice payment");
    }

    [Fact]
    public void PaymentRequestDto_DefaultsCorrectly()
    {
        var dto = new PaymentRequestDto();

        dto.CorrelationId.Should().BeEmpty();
        dto.SenderAccount.Should().BeEmpty();
        dto.ReceiverAccount.Should().BeEmpty();
        dto.Amount.Should().Be(0);
        dto.Currency.Should().Be("USD");
        dto.ValueDate.Should().BeNull();
        dto.Description.Should().BeEmpty();
    }

    [Fact]
    public void PaymentStatusDto_SerializesAllFields()
    {
        var dto = new PaymentStatusDto
        {
            CorrelationId = "corr-456",
            Status = "Settled",
            SagaState = "Completed",
            Amount = 500m,
            Currency = "EUR",
            CreatedAt = DateTime.UtcNow,
            SettledAt = DateTime.UtcNow.AddHours(1),
            SenderAccount = "FROM-ACC",
            ReceiverAccount = "TO-ACC"
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PaymentStatusDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.CorrelationId.Should().Be("corr-456");
        deserialized.Status.Should().Be("Settled");
        deserialized.Amount.Should().Be(500m);
    }

    [Fact]
    public void CreatePaymentResponse_DefaultsHaveCorrectMessage()
    {
        var response = new Shared.Dtos.CreatePaymentResponse 
        { 
            CorrelationId = "test-123"
        };

        response.CorrelationId.Should().Be("test-123");
        response.Message.Should().BeNull();
    }

    [Fact]
    public void GetPaymentsByStatusRequest_DefaultsCorrectly()
    {
        var request = new Shared.Dtos.GetPaymentsByStatusRequest();

        request.Status.Should().BeEmpty();
        request.Page.Should().Be(1);
        request.PageSize.Should().Be(20);
    }
}
