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
/// <para><strong>@module-type:</strong> UTILITY (pure data contract)</para>
/// </summary>
public class CreatePaymentResponse
{
    /// <summary><para><strong>@property:</strong> CorrelationId</para><para>Idempotency key for tracking async saga</para></summary>
    public required string CorrelationId { get; init; }

    /// <summary><para><strong>@property:</strong> Message</para><para>Status message (e.g., "Payment accepted for processing")</para></summary>
    public string? Message { get; init; }

    /// <summary><para><strong>@property:</strong> AcceptedAt</para><para>Timestamp when payment was accepted (UTC)</para></summary>
    public DateTime AcceptedAt { get; init; }
}
