// FILE: src/PaymentService.Shared/Dtos/GetPaymentsByStatusRequest.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Paginated payment query request DTO
// SEMANTIC_TAG: [QUERY_DTO, PAGINATION]
// START_MODULE M-SHARED-DTOS

namespace PaymentService.Shared.Dtos;

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> Query DTO for listing payments by status with pagination</para>
/// <para><strong>@invariant:</strong> Status must match valid payment states</para>
/// <para><strong>@invariant:</strong> Page > 0 (1-based)</para>
/// <para><strong>@invariant:</strong> PageSize > 0 and ≤ 1000</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </summary>
/// <remarks>
/// <para><strong>Usage:</strong> GET /api/payments/by-status?status=Settled&amp;page=2&amp;pageSize=50</para>
/// <para><strong>Pagination:</strong> Offset = (Page - 1) * PageSize</para>
/// </remarks>
public record GetPaymentsByStatusRequest : IRequest
{
    /// <summary><para><strong>@property:</strong> Status</para><para>Filter criterion: {Pending, Validating, Enriching, Settling, Settled, Failed}</para></summary>
    public string Status { get; init; } = string.Empty;

    /// <summary><para><strong>@property:</strong> Page</para><para>1-based page number for cursor-free pagination</para></summary>
    public int Page { get; init; } = 1;

    /// <summary><para><strong>@property:</strong> PageSize</para><para>Rows per page (max 1000)</para></summary>
    public int PageSize { get; init; } = 20;
}
