// FILE: src/PaymentService.Shared/Models/SagaState.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Saga state tracking model for idempotency and compensation
// SEMANTIC_TAG: [SAGA_STATE, IDEMPOTENCY_MODEL]
// START_MODULE M-SHARED-MODELS

namespace PaymentService.Shared.Models;

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@purpose:</strong> Captures saga execution state for idempotency and error recovery</para>
/// <para><strong>@module-type:</strong> UTILITY (persistence model)</para>
/// <para><strong>@domain-concept:</strong> SagaState (execution track record)</para>
/// <para><strong>@invariant:</strong> CorrelationId links to PaymentDocument</para>
/// <para><strong>@invariant:</strong> CurrentStep consistent with CompletedSteps list</para>
/// <para><strong>@invariant:</strong> CompletedAt only set when saga finishes</para>
/// <para><strong>@stability:</strong> STABLE</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </summary>
/// <remarks>
/// <para><strong>Idempotency Check:</strong> Check CompletedSteps before each step execution</para>
/// <para><strong>Compensation:</strong> Reverse steps from CompletedSteps on failure</para>
/// <para><strong>Retry Logic:</strong> RetryCount incremented on transient failures</para>
/// </remarks>
public record SagaState
{
    /// <summary><para><strong>@property:</strong> Id</para><para>MongoDB _id</para></summary>
    public string Id { get; init; } = string.Empty;

    /// <summary><para><strong>@property:</strong> CorrelationId</para><para>Link to PaymentDocument</para></summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary><para><strong>@property:</strong> CurrentStep</para><para>Active saga step name</para></summary>
    public string CurrentStep { get; init; } = "None";

    /// <summary><para><strong>@property:</strong> CompletedSteps</para><para>Steps already executed (for idempotency)</para></summary>
    public List<string> CompletedSteps { get; init; } = new();

    /// <summary><para><strong>@property:</strong> LastError</para><para>Error message from last failed attempt</para></summary>
    public string? LastError { get; init; }

    /// <summary><para><strong>@property:</strong> RetryCount</para><para>Number of retry attempts so far</para></summary>
    public int RetryCount { get; init; }

    /// <summary><para><strong>@property:</strong> StartedAt</para><para>Saga start timestamp (UTC)</para></summary>
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;

    /// <summary><para><strong>@property:</strong> CompletedAt</para><para>Saga completion or null</para></summary>
    public DateTime? CompletedAt { get; init; }
}
