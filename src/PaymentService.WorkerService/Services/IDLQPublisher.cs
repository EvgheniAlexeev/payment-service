// FILE: IDLQPublisher.cs
// VERSION: 2.0.0
// MODULE: M-WORKER
// PURPOSE: External service dependency
// SEMANTIC_TAG: [SERVICE_ABSTRACTION, DEPENDENCY]
// START_MODULE M_WORKER

// START_MODULE M-WORKER
// START_BLOCK_SERVICE IDLQPublisher
// PURPOSE: Dead Letter Queue publisher contract for failed saga events.
//          Operators review DLQ events and decide: retry, manual intervention, or contact customer.
// SEMANTIC_TAG: [BLOCK_SERVICE_INTERFACE] Export: IDLQPublisher
namespace PaymentService.Workers.Services;

/// <summary>
/// Service abstraction contract for the M-WORKER module
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (service abstraction, dependency injection contract)</para>
/// <para><strong>@purpose:</strong> Service abstraction contract for the M-WORKER module</para>
/// <para><strong>@invariant:</strong> All implementations must be thread-safe and respect cancellation tokens</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>

using PaymentService.Shared.Events;

public interface IDLQPublisher
{
    /// <summary>
    /// Publish a failed payment event to the dead letter queue.
    /// </summary>
    /// <param name="failedEvent">The failure event with full context.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PublishFailedPaymentAsync(PaymentFailed failedEvent, CancellationToken ct = default);
}
// END_BLOCK_SERVICE
