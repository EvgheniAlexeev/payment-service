// FILE: src/PaymentService.Api.ReaderService/Features/GetTransactions/GetTransactionsResponse.cs
// VERSION: 2.0.0
// MODULE: M-READER
// PURPOSE: Response DTO for account transaction history
// SEMANTIC_TAG: [RESPONSE_DTO, OUTPUT_MODEL]
// START_MODULE M_READER

using PaymentService.Shared.Dtos;

namespace PaymentService.Api.ReaderService.Features.GetTransactions;

/// <summary>
/// Response model for GetTransactions feature.
/// VSA feature: GetTransactions (ReaderService)
/// </summary>
public class GetTransactionsResponse
{
    /// <summary>
    /// List of transactions for the requested account.
    /// </summary>
    public List<TransactionItem> Transactions { get; set; } = new();

    /// <summary>
    /// Total count of matching transactions (for pagination).
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// Account ID that was queried.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;
}

/// <summary>
/// Single transaction entry in the statement.
/// </summary>
public class TransactionItem
{
    /// <summary>
    /// Payment correlation ID.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Counterparty account (sender if this account is receiver, receiver if this account is sender).
    /// </summary>
    public string CounterpartyAccount { get; set; } = string.Empty;

    /// <summary>
    /// Direction: "outgoing" if this account is sender, "incoming" if this account is receiver.
    /// </summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// Payment amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// ISO 4217 currency code.
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Current payment status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the payment was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the payment was settled (UTC, null if not settled).
    /// </summary>
    public DateTime? SettledAt { get; set; }
}
