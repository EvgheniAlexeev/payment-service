// FILE: src/PaymentService.Shared/Dtos/CreatePaymentResponse.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Response DTO for async payment creation
// SEMANTIC_TAG: [RESPONSE_DTO, ASYNC_PATTERN]
// START_MODULE M-SHARED-DTOS

namespace PaymentService.Shared.Dtos;

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@purpose:</strong> Response DTO for 202 Accepted pattern on payment creation</para>
/// <para><strong>@module-type:</strong> UTILITY (pure data contract)</para>
/// <para><strong>@depends:</strong> IResponse interface</para>
/// <para><strong>@domain-concept:</strong> CreatePaymentResponse (value object)</para>
/// <para><strong>@invariant:</strong> CorrelationId must be non-empty</para>
/// <para><strong>@invariant:</strong> Message must describe async processing status</para>
/// <para><strong>@stability:</strong> STABLE</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </summary>
/// <remarks>
/// <para><strong>Usage:</strong> Returned to client immediately upon acceptance (status 202)</para>
/// <para><strong>Async Pattern:</strong> Client polls GET /api/payments/{correlationId} for status updates</para>
/// </remarks>
public record CreatePaymentResponse : IResponse
{
    /// <summary>
    /// <para><strong>@property:</strong> CorrelationId</para>
    /// <para><strong>@purpose:</strong> Token for client to track payment processing status</para>
    /// <para><strong>@constraint:</strong> Non-empty GUID string</para>
    /// <para><strong>@usage:</strong> Used in subsequent GET /api/payments/{correlationId} queries</para>
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>
    /// <para><strong>@property:</strong> Message</para>
    /// <para><strong>@purpose:</strong> Human-readable status indication</para>
    /// <para><strong>@constraint:</strong> Non-empty string, fixed values per outcome</para>
    /// <para><strong>@usage:</strong> Client displays to user</para>
    /// </summary>
    public string Message { get; init; } = "Payment accepted for processing";
}
