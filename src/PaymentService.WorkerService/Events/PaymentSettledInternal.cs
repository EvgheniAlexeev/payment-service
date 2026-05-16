// FILE: PaymentSettledInternal.cs
// VERSION: 2.0.0
// MODULE: M-WORKER
// PURPOSE: Saga internal or external event
// SEMANTIC_TAG: [SAGA_EVENT, MESSAGE]
// START_MODULE M_WORKER

// START_MODULE M-WORKER
// START_BLOCK_EVENT PaymentSettledInternal
// PURPOSE: Wolverine event emitted after settlement completes — consumed by saga to advance state.
//          (Note: distinct from Shared.Events.PaymentSettled which is the external notification event.)
// SEMANTIC_TAG: [BLOCK_EVENT] Wolverine IEvent — settlement internal completed
namespace PaymentService.Workers.Events;

/// <summary>
/// Component of the M-WORKER module
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (component)</para>
/// <para><strong>@purpose:</strong> Component of the M-WORKER module</para>
/// <para><strong>@invariant:</strong> All properties are immutable after construction</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>

public sealed record PaymentSettledInternal
{
    /// <summary>Correlation ID of the settled payment.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Settlement reference ID.</summary>
    public string SettlementId { get; init; } = string.Empty;

    /// <summary>Whether settlement was successful.</summary>
    public bool IsSuccessful { get; init; }

    /// <summary>Error message if settlement failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>When settlement completed (UTC).</summary>
    public DateTime SettledAt { get; init; } = DateTime.UtcNow;
}
// END_BLOCK_EVENT
