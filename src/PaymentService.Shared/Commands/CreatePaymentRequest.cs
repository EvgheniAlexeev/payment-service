// FILE: src/PaymentService.Shared/Commands/CreatePaymentRequest.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Payment creation request command
// SEMANTIC_TAG: [COMMAND_DTO]
// START_MODULE M_SHARED

namespace PaymentService.Shared.Commands;

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> Request model for payment creation via Writer API</para>
/// </summary>
public class CreatePaymentRequest
{
    /// <summary><para><strong>@property:</strong> CorrelationId</para><para>Unique idempotency key</para></summary>
    public required string CorrelationId { get; init; }

    /// <summary><para><strong>@property:</strong> SenderAccount</para><para>Source account identifier</para></summary>
    public required string SenderAccount { get; init; }

    /// <summary><para><strong>@property:</strong> ReceiverAccount</para><para>Destination account identifier</para></summary>
    public required string ReceiverAccount { get; init; }

    /// <summary><para><strong>@property:</strong> Amount</para><para>Transfer amount in base currency units</para></summary>
    public required decimal Amount { get; init; }

    /// <summary><para><strong>@property:</strong> Currency</para><para>ISO 4217 currency code</para></summary>
    public required string Currency { get; init; }

    /// <summary><para><strong>@property:</strong> ValueDate</para><para>Settlement date (UTC)</para></summary>
    public required DateTime ValueDate { get; init; }

    /// <summary><para><strong>@property:</strong> Description</para><para>Optional transaction description</para></summary>
    public string? Description { get; init; }
}
