// FILE: src/PaymentService.Shared/Commands/PaymentCommand.cs
// VERSION: 1.0.0

using PaymentService.Shared.Dtos;

namespace PaymentService.Shared.Commands;

/// <summary>
/// BLOCK_PAYMENT_COMMAND — Wolverine command to initiate payment saga.
/// Published by API Writer after persisting the initial payment document.
/// </summary>
public record PaymentCommand : ICommand
{
    /// <summary>Idempotency key for deduplication.</summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>Correlation ID of the payment.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Full payment request payload.</summary>
    public PaymentRequestDto Request { get; init; } = new();
}
