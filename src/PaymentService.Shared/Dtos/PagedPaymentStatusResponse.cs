// FILE: src/PaymentService.Shared/Dtos/PagedPaymentStatusResponse.cs
using PaymentService.Shared.Dtos;
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Paginated query response for payments
// SEMANTIC_TAG: [RESPONSE_DTO, PAGINATION]
// START_MODULE M_SHARED_DTOS

namespace PaymentService.Shared.Dtos;

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> Paginated response wrapper for payment status queries</para>
/// </summary>
public record PagedPaymentStatusResponse
{
    /// <summary><para><strong>@property:</strong> Items</para><para>Current page items</para></summary>
    public List<PaymentStatusDto> Items { get; init; } = new();

    /// <summary><para><strong>@property:</strong> Page</para><para>Current page number (1-based)</para></summary>
    public int Page { get; init; }

    /// <summary><para><strong>@property:</strong> PageSize</para><para>Items per page</para></summary>
    public int PageSize { get; init; }

    /// <summary><para><strong>@property:</strong> Total</para><para>Total item count across all pages</para></summary>
    public long Total { get; init; }

    /// <summary><para><strong>@property:</strong> Pages</para><para>Total number of pages</para></summary>
    public int Pages => (int)Math.Ceiling((double)Total / PageSize);
}
