// FILE: src/PaymentService.Shared/Dtos/PaymentStatusDto.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Payment status query response DTO
// SEMANTIC_TAG: [QUERY_DTO, STATUS_VO]
// START_MODULE M-SHARED-DTOS

namespace PaymentService.Shared.Dtos;

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@purpose:</strong> Value object returned by Reader API for payment status queries</para>
/// <para><strong>@module-type:</strong> UTILITY (pure data contract)</para>
/// <para><strong>@depends:</strong> IQueryResponse interface (implicit)</para>
/// <para><strong>@domain-concept:</strong> PaymentStatusDto (aggregate snapshot)</para>
/// <para><strong>@invariant:</strong> CorrelationId maps to exactly one payment document</para>
/// <para><strong>@invariant:</strong> Status in {Pending, Validating, Enriching, Settling, Settled, Failed}</para>
/// <para><strong>@invariant:</strong> SagaState consistent with Status transition</para>
/// <para><strong>@stability:</strong> STABLE</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </summary>
/// <remarks>
/// <para><strong>Query Response:</strong> GET /api/payments/{correlationId} returns PaymentStatusDto</para>
/// <para><strong>Saga State Mapping:</strong> SettledAt populated only when Status=Settled</para>
/// <para><strong>Timeline:</strong> Tracks CreatedAt → SettledAt with all intermediate saga steps</para>
/// </remarks>
public record PaymentStatusDto
{
    /// <summary><para><strong>@property:</strong> CorrelationId</para><para>Payment tracking token</para></summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary><para><strong>@property:</strong> Status</para><para>Current payment state in saga lifecycle</para></summary>
    public string Status { get; init; } = string.Empty;

    /// <summary><para><strong>@property:</strong> SagaState</para><para>Detailed saga step for observability</para></summary>
    public string SagaState { get; init; } = "None";

    /// <summary><para><strong>@property:</strong> Amount</para><para>Payment amount in specified currency</para></summary>
    public decimal Amount { get; init; }

    /// <summary><para><strong>@property:</strong> Currency</para><para>ISO 4217 currency code</para></summary>
    public string Currency { get; init; } = "USD";

    /// <summary><para><strong>@property:</strong> CreatedAt</para><para>Payment submission timestamp (UTC)</para></summary>
    public DateTime CreatedAt { get; init; }

    /// <summary><para><strong>@property:</strong> SettledAt</para><para>Settlement completion timestamp or null</para></summary>
    public DateTime? SettledAt { get; init; }

    /// <summary><para><strong>@property:</strong> SenderAccount</para><para>Source account identifier</para></summary>
    public string SenderAccount { get; init; } = string.Empty;

    /// <summary><para><strong>@property:</strong> ReceiverAccount</para><para>Destination account identifier</para></summary>
    public string ReceiverAccount { get; init; } = string.Empty;
}
