// FILE: DefaultSettlementService.cs
// VERSION: 2.0.0
// MODULE: M-WORKER
// PURPOSE: External service dependency
// SEMANTIC_TAG: [SERVICE_ABSTRACTION, DEPENDENCY]
// START_MODULE M_WORKER

// START_MODULE M-WORKER
// START_BLOCK_SERVICE DefaultSettlementService
// PURPOSE: Default implementation of ISettlementService using HTTP client.
//          Finalizes payments via the external settlement service.
//          Uses Polly retry policy via the registered HttpClient.
// SEMANTIC_TAG: [BLOCK_SERVICE_IMPL] Default ISettlementService (HTTP)
namespace PaymentService.Workers.Services.Implementations;

/// <summary>
/// Service implementation for the M-WORKER module
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (service implementation)</para>
/// <para><strong>@purpose:</strong> Service implementation for the M-WORKER module</para>
/// <para><strong>@invariant:</strong> All operations logged with correlation IDs for traceability</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

public class DefaultSettlementService : ISettlementService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DefaultSettlementService> _logger;

    public DefaultSettlementService(
        IHttpClientFactory httpClientFactory,
        ILogger<DefaultSettlementService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> SettleAsync(
        string correlationId,
        string reservationId,
        decimal amount,
        string receiverAccount,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "[PaymentService.Workers][DefaultSettlementService][BLOCK_SETTLE_HTTP] " +
            "Calling settlement service for {correlationId}, reservationId={reservationId}",
            correlationId, reservationId);

        var client = _httpClientFactory.CreateClient("SettlementService");

        try
        {
            var payload = new { correlationId, reservationId, amount, receiverAccount };
            var response = await client.PostAsJsonAsync("/api/settle", payload, ct);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SettlementResult>(cancellationToken: ct);
                return result?.SettlementId;
            }

            _logger.LogWarning(
                "[PaymentService.Workers][DefaultSettlementService][BLOCK_SETTLE_HTTP_FAIL] " +
                "Settlement service returned status {statusCode} for {correlationId}",
                (int)response.StatusCode, correlationId);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PaymentService.Workers][DefaultSettlementService][BLOCK_SETTLE_HTTP_ERROR] " +
                "Settlement service call failed for {correlationId}",
                correlationId);
            return null;
        }
    }

    private sealed record SettlementResult
    {
        public string SettlementId { get; init; } = string.Empty;
    }
}
// END_BLOCK_SERVICE
