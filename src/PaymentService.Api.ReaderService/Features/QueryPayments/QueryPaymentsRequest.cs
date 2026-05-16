// FILE: src/PaymentService.Api.ReaderService/Features/QueryPayments/QueryPaymentsRequest.cs
using PaymentService.Shared.Dtos;
// VERSION: 2.0.0
// MODULE: M-READER
// PURPOSE: Query request DTO
// SEMANTIC_TAG: [QUERY_DTO, INPUT_VALIDATION]
// START_MODULE M_READER

// FILE: src/PaymentService.Api.ReaderService/Features/QueryPayments/QueryPaymentsRequest.cs
// VERSION: 1.0.0

using System.ComponentModel.DataAnnotations;

namespace PaymentService.Api.ReaderService.Features.QueryPayments;

/// <summary>
/// Request model for querying payments by status.
/// VSA feature: QueryPayments (ReaderService)
/// </summary>
public class QueryPaymentsRequest
{
    /// <summary>
    /// Payment status to filter by.
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(64)]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size (1-100).
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
