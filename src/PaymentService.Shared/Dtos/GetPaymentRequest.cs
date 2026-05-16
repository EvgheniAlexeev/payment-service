// FILE: src/PaymentService.Shared/Dtos/GetPaymentRequest.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Payment retrieval query DTO
// SEMANTIC_TAG: [QUERY_DTO]
// START_MODULE M_SHARED

namespace PaymentService.Shared.Dtos;

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@purpose:</strong> Query DTO for retrieving a single payment by ID</para>
/// <para><strong>@module-type:</strong> UTILITY (pure data contract)</para>
/// </summary>
public class GetPaymentRequest
{
    /// <summary><para><strong>@property:</strong> PaymentId</para><para>Unique payment identifier</para></summary>
    public required string PaymentId { get; init; }
}
