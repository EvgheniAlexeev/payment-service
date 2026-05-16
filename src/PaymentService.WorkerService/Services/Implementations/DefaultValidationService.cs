// FILE: DefaultValidationService.cs
// VERSION: 2.0.0
// MODULE: M-WORKER
// PURPOSE: External service dependency
// SEMANTIC_TAG: [SERVICE_ABSTRACTION, DEPENDENCY]
// START_MODULE M_WORKER

// START_MODULE M-WORKER
// START_BLOCK_SERVICE DefaultValidationService
// PURPOSE: Default implementation of IValidationService using HTTP client.
//          Validates payment requests against the external validation service.
//          Uses Polly retry policy via the registered HttpClient.
// SEMANTIC_TAG: [BLOCK_SERVICE_IMPL] Default IValidationService (HTTP)
namespace PaymentService.Workers.Services.Implementations;

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using PaymentService.Shared.Dtos;

/// <summary>
/// Default HTTP-based validation service implementation.
/// Calls external validation service with Polly retry policy.
/// </summary>
public class DefaultValidationService : IValidationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DefaultValidationService> _logger;

    public DefaultValidationService(
        IHttpClientFactory httpClientFactory,
        ILogger<DefaultValidationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> ValidatePaymentAsync(PaymentRequestDto request, CancellationToken ct = default)
    {
        _logger.LogDebug(
            "[PaymentService.Workers][DefaultValidationService][BLOCK_VALIDATE_HTTP] " +
            "Calling external validation service for {correlationId}",
            request.CorrelationId);

        var client = _httpClientFactory.CreateClient("ValidationService");

        try
        {
            var response = await client.PostAsJsonAsync("/api/validate", request, ct);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ValidationResult>(cancellationToken: ct);
                return result?.IsValid ?? false;
            }

            _logger.LogWarning(
                "[PaymentService.Workers][DefaultValidationService][BLOCK_VALIDATE_HTTP_FAIL] " +
                "Validation service returned status {statusCode} for {correlationId}",
                (int)response.StatusCode, request.CorrelationId);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PaymentService.Workers][DefaultValidationService][BLOCK_VALIDATE_HTTP_ERROR] " +
                "Validation service call failed for {correlationId}",
                request.CorrelationId);
            return false;
        }
    }

    /// <summary>Validation service response DTO.</summary>
    private sealed record ValidationResult
    {
        public bool IsValid { get; init; }
    }
}
// END_BLOCK_SERVICE
