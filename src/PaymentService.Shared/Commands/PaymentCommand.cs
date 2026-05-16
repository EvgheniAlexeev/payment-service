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
/// <para><strong>@module-type:</strong> UTILITY (pure data contract)</para>
/// <para><strong>@depends:</strong> PaymentService.Shared.Dtos</para>
/// <para><strong>@domain-concept:</strong> PaymentCommand</para>
/// <para><strong>@invariant:</strong> IdempotencyKey and CorrelationId must be non-empty</para>
/// <para><strong>@stability:</strong> STABLE</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </summary>
/// <remarks>
/// <para><strong>Saga Initiation Flow:</strong> API Writer → persists document → publishes PaymentCommand → Worker receives via message bus</para>
/// <para><strong>Idempotency:</strong> {CorrelationId}:{MessageVersion} unique constraint in MongoDB</para>
/// </remarks>
// START_BLOCK_PAYMENT_COMMAND
public record PaymentCommand : ICommand
{
    /// <summary>
    /// <para><strong>@property:</strong> IdempotencyKey</para>
    /// <para><strong>@purpose:</strong> Unique key for deduplication across retries</para>
    /// <para><strong>@constraint:</strong> Non-empty string, format: {CorrelationId}:{step}</para>
    /// <para><strong>@usage:</strong> Checked against MongoDB unique index before processing</para>
    /// </summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>
    /// <para><strong>@property:</strong> CorrelationId</para>
    /// <para><strong>@purpose:</strong> Traces payment through entire saga lifecycle</para>
    /// <para><strong>@constraint:</strong> Non-empty GUID string, immutable</para>
    /// <para><strong>@usage:</strong> Primary key for saga state persistence, partition key for message ordering</para>
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>
    /// <para><strong>@property:</strong> Request</para>
    /// <para><strong>@purpose:</strong> Full payment request payload passed from Writer to saga processor</para>
    /// <para><strong>@constraint:</strong> Must be valid PaymentRequestDto (validated before publication)</para>
    /// <para><strong>@usage:</strong> Saga uses this to validate, reserve, and settle payment</para>
    /// </summary>
    public PaymentRequestDto Request { get; init; } = new();
}
