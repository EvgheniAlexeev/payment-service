// FILE: src/PaymentService.Shared/Models/PaymentDocument.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Payment aggregate document for MongoDB persistence
// SEMANTIC_TAG: [AGGREGATE_ROOT, PERSISTENCE_MODEL]
// START_MODULE M-SHARED-MODELS

using PaymentService.Shared.Dtos;

namespace PaymentService.Shared.Models;

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@purpose:</strong> MongoDB document storing payment state and timeline</para>
/// <para><strong>@module-type:</strong> UTILITY (persistence model)</para>
/// <para><strong>@depends:</strong> PaymentService.Shared.Dtos.PaymentRequestDto</para>
/// <para><strong>@domain-concept:</strong> PaymentDocument (aggregate root)</para>
/// <para><strong>@invariant:</strong> CorrelationId is unique (MongoDB unique index)</para>
/// <para><strong>@invariant:</strong> Status in {Pending, Validating, Enriching, Settling, Settled, Failed, Compensated}</para>
/// <para><strong>@invariant:</strong> ModifiedAt ≥ CreatedAt when populated</para>
/// <para><strong>@invariant:</strong> SettledAt only non-null when Status=Settled</para>
/// <para><strong>@stability:</strong> STABLE</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </summary>
/// <remarks>
/// <para><strong>Lifecycle:</strong> Created on payment submission, mutated by saga steps, indexed by CorrelationId</para>
/// <para><strong>TTL:</strong> MongoDB TTL index on CreatedAt (7 days) for auto-cleanup</para>
/// <para><strong>Idempotency:</strong> SagaState + ModifiedAt ensure no duplicate step execution</para>
/// </remarks>
public record PaymentDocument
{
    /// <summary><para><strong>@property:</strong> Id</para><para>MongoDB internal identifier (_id)</para></summary>
    public string Id { get; init; } = string.Empty;

    /// <summary><para><strong>@property:</strong> CorrelationId</para><para>Business key, unique index for saga tracing</para></summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary><para><strong>@property:</strong> Request</para><para>Full payment request snapshot at submission time</para></summary>
    public PaymentRequestDto Request { get; init; } = new();

    /// <summary><para><strong>@property:</strong> Status</para><para>Current payment state in saga lifecycle</para></summary>
    public string Status { get; init; } = "Pending";

    /// <summary><para><strong>@property:</strong> SagaState</para><para>Detailed saga step for observability and idempotency</para></summary>
    public string SagaState { get; init; } = "None";

    /// <summary><para><strong>@property:</strong> CreatedAt</para><para>Document creation timestamp (UTC), used for TTL index</para></summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary><para><strong>@property:</strong> SettledAt</para><para>Settlement completion or null</para></summary>
    public DateTime? SettledAt { get; init; }

    /// <summary><para><strong>@property:</strong> SettlementId</para><para>Ledger settlement reference after successful settlement</para></summary>
    public string? SettlementId { get; init; }

    /// <summary><para><strong>@property:</strong> ModifiedAt</para><para>Last mutation timestamp, null until first update</para></summary>
    public DateTime? ModifiedAt { get; init; }
}
