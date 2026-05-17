// FILE: src/PaymentService.Shared/Dtos/PaymentRequestDto.cs
// VERSION: 1.0.0

namespace PaymentService.Shared.Dtos;

/// <summary>
/// BLOCK_CREATE_PAYMENT DTO for payment creation requests.
/// Contains PII (Description) — must be redacted in logs.
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> DTO carrying payment creation request with PII redaction requirements</para>
/// <para><strong>@invariant:</strong> Amount > 0</para>
/// <para><strong>@invariant:</strong> Currency is valid ISO 4217 code</para>
/// <para><strong>@invariant:</strong> SenderAccount and ReceiverAccount different</para>
/// <para><strong>@invariant:</strong> Description must be redacted in log output</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </remarks>
public record PaymentRequestDto
{
    /// <summary>Unique payment correlation identifier.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Source account identifier.</summary>
    public string SenderAccount { get; init; } = string.Empty;

    /// <summary>Destination account identifier.</summary>
    public string ReceiverAccount { get; init; } = string.Empty;

    /// <summary>Payment amount in the specified currency.</summary>
    public decimal Amount { get; init; }

    /// <summary>ISO 4217 currency code (e.g., USD, EUR).</summary>
    public string Currency { get; init; } = "USD";

    /// <summary>Settlement value date.</summary>
    public DateTime? ValueDate { get; init; }

    /// <summary>Payment purpose/description (PII — redact in logs).</summary>
    public string Description { get; init; } = string.Empty;
}
