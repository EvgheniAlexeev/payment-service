// START_MODULE M-WORKER
// START_BLOCK_SERVICE IDLQPublisher
// PURPOSE: Dead Letter Queue publisher contract for failed saga events.
//          Operators review DLQ events and decide: retry, manual intervention, or contact customer.
// SEMANTIC_TAG: [BLOCK_SERVICE_INTERFACE] Export: IDLQPublisher
namespace PaymentService.Workers.Services;

using PaymentService.Shared.Events;

/// <summary>
/// Dead Letter Queue publisher for failed payment events.
/// Enables manual operator review of failures (no automatic compensation).
/// </summary>
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
