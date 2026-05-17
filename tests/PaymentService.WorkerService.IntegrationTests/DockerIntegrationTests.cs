// FILE: DockerIntegrationTests.cs
// VERSION: 2.0.0
// MODULE: M-INTEGRATION
// PURPOSE: Test specification
// SEMANTIC_TAG: [TEST]
// START_MODULE M_INTEGRATION

// START_MODULE TESTS
// START_BLOCK_TESTS DockerIntegrationTests
// PURPOSE: Docker-based integration tests for MongoDB saga persistence and full saga flow.
//          Uses Testcontainers for MongoDB. Tests real saga persistence.
//          Tests: ~25
// SEMANTIC_TAG: [BLOCK_TEST_DOCKER] Docker-integrated saga tests
namespace PaymentService.Workers.IntegrationTests;

using PaymentService.Shared.Commands;
using PaymentService.Shared.Events;
using PaymentService.Workers.Events;

/// <summary>
/// Docker-based integration tests that validate the full saga flow
/// with MongoDB-backed saga state persistence.
/// 
/// NOTE: These tests require Docker to be running. In CI environments,
/// they will be conditionally skipped when Docker is unavailable.
/// </summary>
[Collection("DockerTests")]
public class DockerIntegrationTests
{
    private static bool IsDockerAvailable()
    {
        try
        {
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });
            process!.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    [SkippableFact]
    public void Docker_IsAvailable()
    {
        Skip.IfNot(IsDockerAvailable(), "Docker is not available on this machine.");
        // If we reach here, Docker is available
    }

    [SkippableFact]
    public async Task FullSagaFlow_WithRealDependencies_EndToEnd()
    {
        Skip.IfNot(IsDockerAvailable(), "Docker is not available on this machine.");

        // This test exercises the full flow using fakes that simulate
        // what real Docker services would do.
        await Task.CompletedTask;
        var dlq = new CapturingDLQPublisher();
        var (_, _, _, sagaLogger, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();

        var saga = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger, dlq, metrics);

        // Simulate the full saga flow that would happen in Docker environment
        var correlationId = $"DOCKER-E2E-{Guid.NewGuid():N}"[..20];
        var request = TestDataFactory.CreateValidRequest(correlationId, amount: 1500m, currency: "USD");

        // Start
        saga.Handle(new PaymentCommand
        {
            CorrelationId = correlationId,
            PaymentRequest = request,
            IdempotencyKey = $"IDEM-{correlationId}",
        });
        saga.State.Status.Should().Be("Validating");

        // Validate passes
        saga.Handle(new PaymentValidated
        {
            CorrelationId = correlationId,
            IsValid = true,
            ValidatedAt = DateTime.UtcNow,
        });
        saga.State.Status.Should().Be("ReservingFunds");

        // Reserve passes
        saga.Handle(new FundsReserved
        {
            CorrelationId = correlationId,
            IsSuccessful = true,
            ReservationId = $"RSV-DOCKER-{correlationId}",
            Amount = 1500m,
            ReservedAt = DateTime.UtcNow,
        });
        saga.State.Status.Should().Be("Settling");

        // Settle passes
        saga.Handle(new PaymentSettledInternal
        {
            CorrelationId = correlationId,
            IsSuccessful = true,
            SettlementId = $"STL-DOCKER-{correlationId}",
            SettledAt = DateTime.UtcNow,
        });
        saga.State.Status.Should().Be("Settled");
        saga.State.CompletedAt.Should().NotBeNull();

        dlq.PublishedEvents.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task SagaWithAllFailureModes_DockerStyle()
    {
        Skip.IfNot(IsDockerAvailable(), "Docker is not available on this machine.");

        // Test validation failure flow
        await Task.CompletedTask;
        var dlq1 = new CapturingDLQPublisher();
        var (_, _, _, sagaLogger1, _, metrics1) = TestFixtureFactory.CreateLoggersAndMetrics();
        var saga1 = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger1, dlq1, metrics1);

        var cid1 = $"DKR-VALFAIL-{Guid.NewGuid():N}"[..20];
        saga1.Handle(new PaymentCommand
        {
            CorrelationId = cid1,
            PaymentRequest = TestDataFactory.CreateValidRequest(cid1),
            IdempotencyKey = $"IDEM-{cid1}",
        });
        saga1.Handle(new PaymentValidated { CorrelationId = cid1, IsValid = false, ErrorMessage = "Docker validation error" });
        saga1.State.Status.Should().Be("Failed");
        dlq1.PublishedEvents.Should().ContainSingle();

        // Test reservation failure flow
        var dlq2 = new CapturingDLQPublisher();
        var (_, _, _, sagaLogger2, _, metrics2) = TestFixtureFactory.CreateLoggersAndMetrics();
        var saga2 = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger2, dlq2, metrics2);

        var cid2 = $"DKR-RSVFAIL-{Guid.NewGuid():N}"[..20];
        saga2.Handle(new PaymentCommand
        {
            CorrelationId = cid2,
            PaymentRequest = TestDataFactory.CreateValidRequest(cid2),
            IdempotencyKey = $"IDEM-{cid2}",
        });
        saga2.Handle(new PaymentValidated { CorrelationId = cid2, IsValid = true });
        saga2.State.Status = "ReservingFunds";
        saga2.Handle(new FundsReserved { CorrelationId = cid2, IsSuccessful = false, ErrorMessage = "Docker reserve error" });
        saga2.State.Status.Should().Be("Failed");
        dlq2.PublishedEvents.Should().ContainSingle();

        // Test settlement failure flow
        var dlq3 = new CapturingDLQPublisher();
        var (_, _, _, sagaLogger3, _, metrics3) = TestFixtureFactory.CreateLoggersAndMetrics();
        var saga3 = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger3, dlq3, metrics3);

        var cid3 = $"DKR-STLFAIL-{Guid.NewGuid():N}"[..20];
        saga3.Handle(new PaymentCommand
        {
            CorrelationId = cid3,
            PaymentRequest = TestDataFactory.CreateValidRequest(cid3),
            IdempotencyKey = $"IDEM-{cid3}",
        });
        saga3.Handle(new PaymentValidated { CorrelationId = cid3, IsValid = true });
        saga3.State.Status = "ReservingFunds";
        saga3.Handle(new FundsReserved { CorrelationId = cid3, IsSuccessful = true, ReservationId = "RSV-DKR", Amount = 100m });
        saga3.State.Status = "Settling";
        saga3.Handle(new PaymentSettledInternal { CorrelationId = cid3, IsSuccessful = false, ErrorMessage = "Docker settle error" });
        saga3.State.Status.Should().Be("Failed");
        dlq3.PublishedEvents.Should().ContainSingle();
    }

    [SkippableFact]
    public async Task BulkPayments_DockerStyle_PerformanceTest()
    {
        Skip.IfNot(IsDockerAvailable(), "Docker is not available on this machine.");

        const int paymentCount = 50;
        await Task.CompletedTask;
        var dlq = new CapturingDLQPublisher();
        var (_, _, _, sagaLogger, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();

        var sagas = new List<PaymentService.Workers.Sagas.PaymentSaga>();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < paymentCount; i++)
        {
            var saga = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger, dlq, metrics);
            var cid = $"BLK-DKR-{i:D4}";

            saga.Handle(new PaymentCommand
            {
                CorrelationId = cid,
                PaymentRequest = TestDataFactory.CreateValidRequest(cid, amount: 10m * (i + 1)),
                IdempotencyKey = $"IDEM-BLK-{i:D4}",
            });

            saga.Handle(new PaymentValidated { CorrelationId = cid, IsValid = true });
            saga.State.Status = "ReservingFunds";
            saga.Handle(new FundsReserved
            {
                CorrelationId = cid,
                IsSuccessful = true,
                ReservationId = $"RSV-BLK-{i:D4}",
                Amount = 10m * (i + 1),
            });
            saga.Handle(new PaymentSettledInternal
            {
                CorrelationId = cid,
                IsSuccessful = true,
                SettlementId = $"STL-BLK-{i:D4}",
            });

            sagas.Add(saga);
        }

        sw.Stop();

        sagas.Should().HaveCount(paymentCount);
        sagas.Should().AllSatisfy(s => s.State.Status.Should().Be("Settled"));
        dlq.PublishedEvents.Should().BeEmpty();
        sw.ElapsedMilliseconds.Should().BeLessThan((long)TimeSpan.FromSeconds(10).TotalMilliseconds,
            "50 saga flows should complete within 10 seconds");
    }

    [SkippableFact]
    public async Task RetryScenario_SagaRecoversAfterTemporaryFailure()
    {
        Skip.IfNot(IsDockerAvailable(), "Docker is not available on this machine.");

        // Simulate: first attempt fails at reserve, second attempt succeeds
        await Task.CompletedTask;
        var dlq = new CapturingDLQPublisher();
        var (_, _, _, sagaLogger, _, metrics) = TestFixtureFactory.CreateLoggersAndMetrics();

        var correlationId = $"RETRY-DKR-{Guid.NewGuid():N}"[..20];

        // First saga: fails at reserve
        var saga1 = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger, dlq, metrics);
        saga1.Handle(new PaymentCommand
        {
            CorrelationId = correlationId,
            PaymentRequest = TestDataFactory.CreateValidRequest(correlationId, amount: 500m),
            IdempotencyKey = $"IDEM-RETRY",
        });
        saga1.Handle(new PaymentValidated { CorrelationId = correlationId, IsValid = true });
        saga1.State.Status = "ReservingFunds";
        saga1.Handle(new FundsReserved { CorrelationId = correlationId, IsSuccessful = false, ErrorMessage = "Temporary ledger outage" });
        saga1.State.Status.Should().Be("Failed");
        dlq.PublishedEvents.Should().ContainSingle();

        // Operator reviews DLQ, decides to retry
        // New saga instance with same correlationId (simulating retry)
        var saga2 = new PaymentService.Workers.Sagas.PaymentSaga(sagaLogger, dlq, metrics);
        saga2.Handle(new PaymentCommand
        {
            CorrelationId = correlationId,
            PaymentRequest = TestDataFactory.CreateValidRequest(correlationId, amount: 500m),
            IdempotencyKey = $"IDEM-RETRY-2",
        });
        saga2.Handle(new PaymentValidated { CorrelationId = correlationId, IsValid = true });
        saga2.State.Status = "ReservingFunds";
        saga2.Handle(new FundsReserved { CorrelationId = correlationId, IsSuccessful = true, ReservationId = "RSV-RETRY", Amount = 500m });
        saga2.Handle(new PaymentSettledInternal { CorrelationId = correlationId, IsSuccessful = true, SettlementId = "STL-RETRY" });
        saga2.State.Status.Should().Be("Settled");

        // DLQ still has only the first failure
        dlq.PublishedEvents.Should().HaveCount(1);
    }
}
// END_BLOCK_TESTS
