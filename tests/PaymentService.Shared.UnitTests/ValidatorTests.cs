// FILE: tests/PaymentService.Shared.UnitTests/ValidatorTests.cs
// VERSION: 1.0.0

using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using PaymentService.Shared.Dtos;
using PaymentService.Shared.Validators;

namespace PaymentService.Shared.UnitTests;

/// <summary>
/// BLOCK_VALIDATE tests for PaymentRequestValidator.
/// </summary>
public class ValidatorTests
{
    private readonly ILogger<PaymentRequestValidator> _logger;
    private readonly PaymentRequestValidator _validator;

    public ValidatorTests()
    {
        _logger = A.Fake<ILogger<PaymentRequestValidator>>();
        _validator = new PaymentRequestValidator(_logger);
    }

    private PaymentRequestDto CreateValidRequest() => new()
    {
        CorrelationId = "corr-valid-001",
        SenderAccount = "ACC-SRC",
        ReceiverAccount = "ACC-DST",
        Amount = 100m,
        Currency = "USD",
        ValueDate = DateTime.UtcNow.Date.AddDays(1),
        Description = "Test payment"
    };

    [Fact]
    public async Task ValidRequest_PassesValidation()
    {
        var request = CreateValidRequest();
        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyCorrelationId_FailsValidation()
    {
        var request = CreateValidRequest() with { CorrelationId = "" };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CorrelationId");
    }

    [Fact]
    public async Task CorrelationIdExceedsMaxLength_FailsValidation()
    {
        var request = CreateValidRequest() with
        {
            CorrelationId = new string('x', 101)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CorrelationId");
    }

    [Fact]
    public async Task MissingSenderAccount_FailsValidation()
    {
        var request = CreateValidRequest() with { SenderAccount = "" };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SenderAccount");
    }

    [Fact]
    public async Task MissingReceiverAccount_FailsValidation()
    {
        var request = CreateValidRequest() with { ReceiverAccount = "" };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReceiverAccount");
    }

    [Fact]
    public async Task ZeroAmount_FailsValidation()
    {
        var request = CreateValidRequest() with { Amount = 0 };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount"
            && e.ErrorMessage.Contains("greater than"));
    }

    [Fact]
    public async Task NegativeAmount_FailsValidation()
    {
        var request = CreateValidRequest() with { Amount = -50m };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public async Task AmountExceedsMax_FailsValidation()
    {
        var request = CreateValidRequest() with { Amount = 1_000_000_000_000M };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount"
            && e.ErrorMessage.Contains("maximum"));
    }

    [Fact]
    public async Task InvalidCurrency_FailsValidation()
    {
        var request = CreateValidRequest() with { Currency = "us" };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    [Fact]
    public async Task TooLongCurrency_FailsValidation()
    {
        var request = CreateValidRequest() with { Currency = "USDD" };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    [Fact]
    public async Task TooShortCurrency_FailsValidation()
    {
        var request = CreateValidRequest() with { Currency = "US" };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    [Fact]
    public async Task PastValueDate_FailsValidation()
    {
        var request = CreateValidRequest() with
        {
            ValueDate = DateTime.UtcNow.Date.AddDays(-2)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ValueDate");
    }

    [Fact]
    public async Task TodayValueDate_PassesValidation()
    {
        var request = CreateValidRequest() with
        {
            ValueDate = DateTime.UtcNow.Date
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task FutureValueDate_PassesValidation()
    {
        var request = CreateValidRequest() with
        {
            ValueDate = DateTime.UtcNow.Date.AddDays(30)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidMultipleCurrencies_PassValidation()
    {
        foreach (var currency in new[] { "USD", "EUR", "GBP", "JPY", "CHF" })
        {
            var request = CreateValidRequest() with { Currency = currency };
            var result = await _validator.ValidateAsync(request);
            result.IsValid.Should().BeTrue($"Currency {currency} should be valid");
        }
    }

    [Fact]
    public async Task NullValueDate_PassesValidation()
    {
        var request = CreateValidRequest() with { ValueDate = null };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DescriptionAtMaxLength_PassesValidation()
    {
        var request = CreateValidRequest() with
        {
            Description = new string('a', 500)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DescriptionExceedsMaxLength_FailsValidation()
    {
        var request = CreateValidRequest() with
        {
            Description = new string('a', 501)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }
}
