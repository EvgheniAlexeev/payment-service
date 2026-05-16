// FILE: src/PaymentService.Shared/Commands/PaymentCommand.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: Payment command contract for saga orchestration
// SEMANTIC_TAG: [PAYMENT_COMMAND, WOLVERINE_MESSAGE]
// START_MODULE M-SHARED-COMMANDS
// SEMANTIC_PURPOSE: PaymentCommand contract (Wolverine message) for initiating payment saga

using PaymentService.Shared.Dtos;

namespace PaymentService.Shared.Commands;

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@purpose:</strong> Wolverine command to initiate payment processing saga</para>
/// <para><strong>@invariant:</strong> IdempotencyKey and CorrelationId must be non-empty</para>
/// <para><strong>@invariant:</strong> {CorrelationId}:{MessageVersion} unique constraint in MongoDB</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </summary>
// START_BLOCK_PAYMENT_COMMAND
public record PaymentCommand : ICommand
{
    public string IdempotencyKey { get; init; } = string.Empty;

    public string CorrelationId { get; init; } = string.Empty;

    public int MessageVersion { get; init; } = 1;

    public PaymentRequestDto PaymentRequest { get; init; } = new();

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
