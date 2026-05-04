// START_MODULE M-WORKER
// START_BLOCK_COMMAND ValidatePaymentCommand
// PURPOSE: Wolverine command dispatched from saga to the ValidatePaymentHandler.
//          Carries the payment request for validation.
// SEMANTIC_TAG: [BLOCK_COMMAND] Wolverine ICommand
namespace PaymentService.Workers.Commands;

using PaymentService.Shared.Dtos;

/// <summary>
/// Command dispatched from PaymentSaga to the ValidatePayment step handler.
/// </summary>
public sealed record ValidatePaymentCommand
{
    /// <summary>Correlation ID of the payment.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>The full payment request to validate.</summary>
    public PaymentRequestDto PaymentRequest { get; init; } = null!;

    /// <summary>When the command was created (UTC).</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
// END_BLOCK_COMMAND
