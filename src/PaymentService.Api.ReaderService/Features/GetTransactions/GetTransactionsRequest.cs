// FILE: src/PaymentService.Api.ReaderService/Features/GetTransactions/GetTransactionsRequest.cs
// VERSION: 2.0.0
// MODULE: M-READER
// PURPOSE: Query request DTO for account transaction history
// SEMANTIC_TAG: [QUERY_DTO, INPUT_VALIDATION]
// START_MODULE M_READER

namespace PaymentService.Api.ReaderService.Features.GetTransactions;

/// <summary>
/// Request model for GetTransactions feature.
/// VSA feature: GetTransactions (ReaderService)
/// </summary>
public class GetTransactionsRequest
{
    /// <summary>
    /// Account ID to query transaction history for.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Number of records to skip (pagination offset).
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    /// Maximum number of records to return.
    /// </summary>
    public int Limit { get; set; } = 20;
}
