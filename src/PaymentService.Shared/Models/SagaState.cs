// FILE: src/PaymentService.Shared/Models/SagaState.cs
// VERSION: 1.0.0

namespace PaymentService.Shared.Models;

/// <summary>
/// Saga state track record for idempotency and compensation.
/// </summary>
public record SagaState
{
    /// <summary>MongoDB _id.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Correlation ID linking to PaymentDocument.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Current saga step (Validating, Enriching, Settling, Notifying).</summary>
    public string CurrentStep { get; init; } = "None";

    /// <summary>List of completed steps for idempotency checking.</summary>
    public List<string> CompletedSteps { get; init; } = new();

    /// <summary>Error message if saga failed at current step.</summary>
    public string? LastError { get; init; }

    /// <summary>Number of retry attempts.</summary>
    public int RetryCount { get; init; }

    /// <summary>When the saga started.</summary>
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;

    /// <summary>When the saga completed or failed.</summary>
    public DateTime? CompletedAt { get; init; }
}
