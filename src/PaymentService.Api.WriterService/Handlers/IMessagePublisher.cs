// FILE: src/PaymentService.Api.WriterService/Handlers/IMessagePublisher.cs
// VERSION: 1.0.0

using PaymentService.Shared;

namespace PaymentService.Api.WriterService.Handlers;

/// <summary>
/// Abstraction for message publishing (Wolverine/Dapr pub-sub).
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publish a command/message asynchronously.
    /// </summary>
    Task PublishAsync<T>(T message, CancellationToken ct = default);
}
