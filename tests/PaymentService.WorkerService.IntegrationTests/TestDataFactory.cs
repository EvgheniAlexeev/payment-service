// FILE: TestDataFactory.cs
// VERSION: 2.0.0
// MODULE: M-INTEGRATION
// PURPOSE: Test specification
// SEMANTIC_TAG: [TEST]
// START_MODULE M_INTEGRATION

// START_MODULE TESTS
// START_BLOCK_TESTS TestDataFactory
// PURPOSE: Centralized test data factory — produces PaymentRequestDto and related models
//          with valid/invalid variants for comprehensive test coverage.
// SEMANTIC_TAG: [BLOCK_TEST_DATA] Test data generation
namespace PaymentService.Workers.IntegrationTests;

using PaymentService.Shared.Dtos;

/// <summary>
/// Factory for creating test payment data with various configurations.
/// </summary>
public static class TestDataFactory
{
    private static int _counter;

    private static string NextId() => $"TEST-{Interlocked.Increment(ref _counter):D6}";

    /// <summary>Create a valid payment request with default values.</summary>
    public static PaymentRequestDto CreateValidRequest(
        string? correlationId = null,
        decimal amount = 100.00m,
        string currency = "USD",
        string senderAccount = "DE89370400440532013000",
        string receiverAccount = "FR1420041010050500013M02606",
        string description = "Test payment")
    {
        return new PaymentRequestDto
        {
            CorrelationId = correlationId ?? NextId(),
            SenderAccount = senderAccount,
            ReceiverAccount = receiverAccount,
            Amount = amount,
            Currency = currency,
            ValueDate = DateTime.UtcNow.Date.AddDays(1),
            Description = description,
        };
    }

    /// <summary>Create a payment request with zero amount (invalid).</summary>
    public static PaymentRequestDto CreateZeroAmountRequest(string? correlationId = null)
    {
        return CreateValidRequest(correlationId, amount: 0m);
    }

    /// <summary>Create a payment request with negative amount (invalid).</summary>
    public static PaymentRequestDto CreateNegativeAmountRequest(string? correlationId = null)
    {
        return CreateValidRequest(correlationId, amount: -50m);
    }

    /// <summary>Create a payment request with very large amount.</summary>
    public static PaymentRequestDto CreateLargeAmountRequest(string? correlationId = null)
    {
        return CreateValidRequest(correlationId, amount: 999_999_999.99m);
    }

    /// <summary>Create a payment request with invalid currency.</summary>
    public static PaymentRequestDto CreateInvalidCurrencyRequest(string? correlationId = null)
    {
        return CreateValidRequest(correlationId, currency: "XXX");
    }

    /// <summary>Create a payment request with empty sender account.</summary>
    public static PaymentRequestDto CreateEmptySenderRequest(string? correlationId = null)
    {
        return CreateValidRequest(correlationId, senderAccount: "");
    }

    /// <summary>Create a payment request with empty receiver account.</summary>
    public static PaymentRequestDto CreateEmptyReceiverRequest(string? correlationId = null)
    {
        return CreateValidRequest(correlationId, receiverAccount: "");
    }

    /// <summary>Create a payment request with past value date (invalid).</summary>
    public static PaymentRequestDto CreatePastValueDateRequest(string? correlationId = null)
    {
        return CreateValidRequest(correlationId) with { ValueDate = DateTime.UtcNow.Date.AddDays(-1) };
    }

    /// <summary>Create a batch of N valid payment requests.</summary>
    public static List<PaymentRequestDto> CreateBatch(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => CreateValidRequest(amount: 10m + i * 5))
            .ToList();
    }

    /// <summary>Create payment requests with various currencies.</summary>
    public static List<PaymentRequestDto> CreateMultiCurrencyRequests()
    {
        var currencies = new[] { "USD", "EUR", "GBP", "CHF", "JPY", "CAD", "AUD", "NZD", "SEK", "NOK" };
        return currencies.Select((c, i) => CreateValidRequest(currency: c, amount: 100m * (i + 1))).ToList();
    }

    /// <summary>Create payment requests at various amounts for boundary testing.</summary>
    public static List<(decimal Amount, bool ShouldBeValid)> CreateBoundaryAmounts()
    {
        return new List<(decimal, bool)>
        {
            (0m, false),           // exactly zero — invalid
            (0.01m, true),         // minimum valid
            (1m, true),            // normal
            (99_999.99m, true),    // normal
            (999_999_999_999.99m, true),  // max valid
            (999_999_999_999.991m, false), // over max — invalid
            (-0.01m, false),       // negative — invalid
        };
    }
}
// END_BLOCK_TESTS
