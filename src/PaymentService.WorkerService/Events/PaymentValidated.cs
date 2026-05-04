// START_MODULE M-WORKER
// START_BLOCK_EVENT PaymentValidated
// PURPOSE: Wolverine event emitted after ValidatePayment step completes.
//          Carries validation result plus error info for DLQ routing.
// SEMANTIC_TAG: [BLOCK_EVENT] Wolverine IEvent — validation completed
namespace PaymentService.Workers.Events;

using PaymentService.Shared.Dtos;

/// <summary>
/// Wolverine event published after the ValidatePayment step handler completes.
/// </summary>
public sealed record PaymentValidated
{
    /// <summary>Correlation ID of the payment being validated.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Whether the payment passed validation.</summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Validation error message (null if valid).
    /// Populated with exception message if validation service throws.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>When validation completed (UTC).</summary>
    public DateTime ValidatedAt { get; init; } = DateTime.UtcNow;
}
// END_BLOCK_EVENT
