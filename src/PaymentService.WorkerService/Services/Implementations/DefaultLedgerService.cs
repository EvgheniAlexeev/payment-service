// START_MODULE M-WORKER
// START_BLOCK_SERVICE DefaultLedgerService
// PURPOSE: Default implementation of ILedgerService using HTTP client.
//          Reserves, settles, and releases funds via external ledger service.
//          Uses Polly retry policy via the registered HttpClient.
// SEMANTIC_TAG: [BLOCK_SERVICE_IMPL] Default ILedgerService (HTTP)
namespace PaymentService.Workers.Services.Implementations;

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

/// <summary>
/// Default HTTP-based ledger service implementation.
/// Manages fund reservation, settlement, and release via external service.
/// </summary>
public class DefaultLedgerService : ILedgerService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DefaultLedgerService> _logger;

    public DefaultLedgerService(
        IHttpClientFactory httpClientFactory,
        ILogger<DefaultLedgerService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> ReserveFundsAsync(
        string correlationId,
        decimal amount,
        string senderAccount,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "[PaymentService.Workers][DefaultLedgerService][BLOCK_RESERVE_HTTP] " +
            "Calling ledger service to reserve {amount} for {correlationId}",
            amount, correlationId);

        var client = _httpClientFactory.CreateClient("LedgerService");

        try
        {
            var payload = new { correlationId, amount, senderAccount };
            var response = await client.PostAsJsonAsync("/api/reserve", payload, ct);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ReservationResult>(cancellationToken: ct);
                return result?.ReservationId;
            }

            _logger.LogWarning(
                "[PaymentService.Workers][DefaultLedgerService][BLOCK_RESERVE_HTTP_FAIL] " +
                "Ledger reserve returned status {statusCode} for {correlationId}",
                (int)response.StatusCode, correlationId);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PaymentService.Workers][DefaultLedgerService][BLOCK_RESERVE_HTTP_ERROR] " +
                "Ledger reserve call failed for {correlationId}",
                correlationId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SettleFundsAsync(
        string correlationId,
        string reservationId,
        decimal amount,
        string receiverAccount,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "[PaymentService.Workers][DefaultLedgerService][BLOCK_SETTLE_HTTP] " +
            "Calling ledger service to settle {amount} for {correlationId}, reservationId={reservationId}",
            amount, correlationId, reservationId);

        var client = _httpClientFactory.CreateClient("LedgerService");

        try
        {
            var payload = new { correlationId, reservationId, amount, receiverAccount };
            var response = await client.PostAsJsonAsync("/api/settle", payload, ct);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PaymentService.Workers][DefaultLedgerService][BLOCK_SETTLE_HTTP_ERROR] " +
                "Ledger settle call failed for {correlationId}",
                correlationId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task ReleaseReservationAsync(
        string correlationId,
        string reservationId,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "[PaymentService.Workers][DefaultLedgerService][BLOCK_RELEASE_HTTP] " +
            "Calling ledger service to release reservation {reservationId} for {correlationId}",
            reservationId, correlationId);

        var client = _httpClientFactory.CreateClient("LedgerService");

        try
        {
            var payload = new { correlationId, reservationId };
            await client.PostAsJsonAsync("/api/release", payload, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PaymentService.Workers][DefaultLedgerService][BLOCK_RELEASE_HTTP_ERROR] " +
                "Ledger release call failed for {correlationId}",
                correlationId);
        }
    }

    private sealed record ReservationResult
    {
        public string ReservationId { get; init; } = string.Empty;
    }
}
// END_BLOCK_SERVICE
