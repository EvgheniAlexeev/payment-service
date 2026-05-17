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
/// <remarks>
/// <para><strong>@contract:</strong> M-READER</para>
/// <para><strong>@purpose:</strong> Request DTO for account transaction history query</para>
/// <para><strong>@module-type:</strong> UTILITY (query DTO)</para>
/// <para><strong>@invariant:</strong> AccountId non-empty, max 64 chars</para>
/// <para><strong>@invariant:</strong> Skip ≥ 0, Limit between 1 and 100</para>
/// <para><strong>@verification-ref:</strong> V-M-READER</para>
/// </remarks>
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
