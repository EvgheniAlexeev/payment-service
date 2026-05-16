// FILE: LoggingDLQPublisher.cs
// VERSION: 2.0.0
// MODULE: M-WORKER
// PURPOSE: External service dependency
// SEMANTIC_TAG: [SERVICE_ABSTRACTION, DEPENDENCY]
// START_MODULE M_WORKER

// START_MODULE M-WORKER
// START_BLOCK_SERVICE LoggingDLQPublisher
// PURPOSE: Default DLQ publisher — logs failed payment events for operator review.
//          In production, this would publish to a message broker DLQ topic.
//          For Phase-3: logging-based monitoring with structured log output.
// SEMANTIC_TAG: [BLOCK_SERVICE_IMPL] Default IDLQPublisher (logging-based)
// SEMANTIC_TAG: [BLOCK_DLQ] Manual DLQ compensation — operator reviews log output
namespace PaymentService.Workers.Services.Implementations;

/// <summary>
/// Component of the M-WORKER module
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (component)</para>
/// <para><strong>@purpose:</strong> Component of the M-WORKER module</para>
/// <para><strong>@invariant:</strong> All properties are immutable after construction</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>

using Microsoft.Extensions.Logging;
using PaymentService.Shared.Events;
using System.Text.Json;

public class LoggingDLQPublisher : IDLQPublisher
{
    private readonly ILogger<LoggingDLQPublisher> _logger;

    public LoggingDLQPublisher(ILogger<LoggingDLQPublisher> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    // START_BLOCK_DLQ_PUBLISH
    public Task PublishFailedPaymentAsync(PaymentFailed failedEvent, CancellationToken ct = default)
    {
        // Structured log for operator review
        var dlqPayload = JsonSerializer.Serialize(new
        {
            type = "DLQ_PAYMENT_FAILED",
            failedEvent.CorrelationId,
            failedEvent.FailedStep,
            failedEvent.ErrorCode,
            failedEvent.ErrorMessage,
            failedEvent.RetryCount,
            failedEvent.FailedAt,
            originalRequest = new
            {
                failedEvent.OriginalRequest?.CorrelationId,
                failedEvent.OriginalRequest?.SenderAccount,
                failedEvent.OriginalRequest?.ReceiverAccount,
                failedEvent.OriginalRequest?.Amount,
                failedEvent.OriginalRequest?.Currency,
            },
            reviewInstructions = new[]
            {
                "1. Review the failure reason and step.",
                "2. Check original payment request details.",
                "3. Decide: retry via API / manual intervention / contact customer.",
                "4. DO NOT auto-compensate — operator decision required.",
            },
        });

        _logger.LogWarning(
            "[PaymentService.Workers][LoggingDLQPublisher][BLOCK_DLQ_PUBLISH] " +
            "DLQ payment failed event: {dlqPayload}", dlqPayload);

        return Task.CompletedTask;
    }
    // END_BLOCK_DLQ_PUBLISH
}
// END_BLOCK_SERVICE
