// FILE: src/PaymentService.Shared/Dtos/CreatePaymentResponse.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Response DTO for payment creation
// SEMANTIC_TAG: [RESPONSE_DTO]
// START_MODULE M_SHARED

namespace PaymentService.Shared.Dtos;

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@purpose:</strong> Response model for successful payment creation (202 Accepted)</para>
/// </summary>
public class CreatePaymentResponse
{
    /// <summary>Idempotency key for tracking async saga</summary>
    public required string CorrelationId { get; init; }

    public string? Message { get; init; }

    public DateTime AcceptedAt { get; init; }
}
