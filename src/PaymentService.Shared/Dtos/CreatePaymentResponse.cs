// FILE: src/PaymentService.Shared/Dtos/CreatePaymentResponse.cs
// VERSION: 1.0.0

namespace PaymentService.Shared.Dtos;

/// <summary>
/// Response DTO for payment creation (202 Accepted pattern).
/// </summary>
public record CreatePaymentResponse : IResponse
{
    /// <summary>The correlationId of the created payment.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Human-readable status message.</summary>
    public string Message { get; init; } = "Payment accepted for processing";
}
