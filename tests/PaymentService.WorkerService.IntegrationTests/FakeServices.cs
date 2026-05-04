// START_MODULE TESTS
// START_BLOCK_TESTS FakeServices
// PURPOSE: Fake/mock implementations of all service interfaces for integration testing.
//          Enables deterministic, fast saga testing without Docker dependencies.
// SEMANTIC_TAG: [BLOCK_TEST_FAKES] Test fakes for Payments saga services
namespace PaymentService.Workers.IntegrationTests;

using FakeItEasy;
using Microsoft.Extensions.Logging;
using PaymentService.Shared.Dtos;
using PaymentService.Shared.Events;
using PaymentService.Workers.Metrics;
using PaymentService.Workers.Services;
using PaymentService.Workers.Services.Implementations;
using PaymentService.Workers.Steps;

/// <summary>
/// Configurable fake for IValidationService — returns true/false based on setup.
/// </summary>
public class FakeValidationService : IValidationService
{
    private bool _shouldPass;

    public FakeValidationService(bool shouldPass = true)
    {
        _shouldPass = shouldPass;
    }

    public void SetResult(bool shouldPass) => _shouldPass = shouldPass;

    public Task<bool> ValidatePaymentAsync(PaymentRequestDto request, CancellationToken ct = default)
    {
        return Task.FromResult(_shouldPass);
    }
}

/// <summary>
/// Configurable fake for ILedgerService — controls reserve and settle outcomes.
/// </summary>
public class FakeLedgerService : ILedgerService
{
    private bool _reserveSucceeds;
    private bool _settleSucceeds;
    private readonly List<string> _releasedReservations = new();

    public FakeLedgerService(bool reserveSucceeds = true, bool settleSucceeds = true)
    {
        _reserveSucceeds = reserveSucceeds;
        _settleSucceeds = settleSucceeds;
    }

    public void SetReserveResult(bool succeeds) => _reserveSucceeds = succeeds;
    public void SetSettleResult(bool succeeds) => _settleSucceeds = succeeds;
    public IReadOnlyList<string> ReleasedReservations => _releasedReservations;

    public Task<string?> ReserveFundsAsync(string correlationId, decimal amount, string senderAccount, CancellationToken ct = default)
    {
        return Task.FromResult(_reserveSucceeds ? $"RSV-{correlationId}-{Guid.NewGuid():N}"[..20] : null);
    }

    public Task<bool> SettleFundsAsync(string correlationId, string reservationId, decimal amount, string receiverAccount, CancellationToken ct = default)
    {
        return Task.FromResult(_settleSucceeds);
    }

    public Task ReleaseReservationAsync(string correlationId, string reservationId, CancellationToken ct = default)
    {
        _releasedReservations.Add(reservationId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Configurable fake for ISettlementService.
/// </summary>
public class FakeSettlementService : ISettlementService
{
    private bool _succeeds;

    public FakeSettlementService(bool succeeds = true)
    {
        _succeeds = succeeds;
    }

    public void SetResult(bool succeeds) => _succeeds = succeeds;

    public Task<string?> SettleAsync(string correlationId, string reservationId, decimal amount, string receiverAccount, CancellationToken ct = default)
    {
        return Task.FromResult(_succeeds ? $"STL-{correlationId}-{DateTime.UtcNow:yyyyMMddHHmmss}" : null);
    }
}

/// <summary>
/// Capturing fake for IDLQPublisher — records published events for assertions.
/// </summary>
public class CapturingDLQPublisher : IDLQPublisher
{
    private readonly List<PaymentFailed> _publishedEvents = new();

    public IReadOnlyList<PaymentFailed> PublishedEvents => _publishedEvents;

    public Task PublishFailedPaymentAsync(PaymentFailed failedEvent, CancellationToken ct = default)
    {
        _publishedEvents.Add(failedEvent);
        return Task.CompletedTask;
    }

    public void Clear() => _publishedEvents.Clear();
}

/// <summary>
/// Creates test fixtures with pre-configured fakes.
/// </summary>
public static class TestFixtureFactory
{
    public static (
        ILogger<ValidatePaymentHandler> validateLogger,
        ILogger<ReserveFundsHandler> reserveLogger,
        ILogger<SettlePaymentHandler> settleLogger,
        ILogger<PaymentService.Workers.Sagas.PaymentSaga> sagaLogger,
        ILogger<LoggingDLQPublisher> dlqLogger,
        PaymentSagaMetrics metrics
    ) CreateLoggersAndMetrics()
    {
        var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Debug).AddConsole());

        return (
            loggerFactory.CreateLogger<ValidatePaymentHandler>(),
            loggerFactory.CreateLogger<ReserveFundsHandler>(),
            loggerFactory.CreateLogger<SettlePaymentHandler>(),
            loggerFactory.CreateLogger<PaymentService.Workers.Sagas.PaymentSaga>(),
            loggerFactory.CreateLogger<LoggingDLQPublisher>(),
            new PaymentSagaMetrics()
        );
    }
}
// END_BLOCK_TESTS
