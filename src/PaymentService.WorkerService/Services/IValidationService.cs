// FILE: IValidationService.cs
// VERSION: 2.0.0
// MODULE: M-WORKER
// PURPOSE: External service dependency
// SEMANTIC_TAG: [SERVICE_ABSTRACTION, DEPENDENCY]
// START_MODULE M_WORKER

// START_MODULE M-WORKER
// START_BLOCK_SERVICE IValidationService
// PURPOSE: External payment validation service contract.
//          Validates payment requests against business rules, compliance, and fraud checks.
// SEMANTIC_TAG: [BLOCK_SERVICE_INTERFACE] Export: IValidationService
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

using PaymentService.Shared.Dtos;

public interface IValidationService
{
    /// <summary>
    /// Validate a payment request asynchronously.
    /// Returns true if the payment passes all checks.
    /// </summary>
    /// <param name="request">The payment request to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> ValidatePaymentAsync(PaymentRequestDto request, CancellationToken ct = default);
}
// END_BLOCK_SERVICE
