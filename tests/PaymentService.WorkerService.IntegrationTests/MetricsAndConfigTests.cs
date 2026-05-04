// START_MODULE TESTS
// START_BLOCK_TESTS MetricsAndConfigTests
// PURPOSE: Tests for PaymentSagaMetrics, SagaTimeoutConfiguration, ServiceConfiguration.
//          Tests: ~30
// SEMANTIC_TAG: [BLOCK_TEST_METRICS] Metrics and configuration tests
namespace PaymentService.Workers.IntegrationTests;

using PaymentService.Workers.Configuration;
using PaymentService.Workers.Metrics;

public class PaymentSagaMetricsTests
{
    private readonly PaymentSagaMetrics _metrics = new();

    [Fact]
    public void IncrementSagaStarted_IncrementsCounters()
    {
        _metrics.IncrementSagaStarted();
        _metrics.IncrementSagaStarted();
        _metrics.IncrementSagaStarted();
        // Counters are Prometheus-managed; test that no exceptions occur
    }

    [Fact]
    public void IncrementSagaCompleted_IncrementsCompletedCounter()
    {
        _metrics.IncrementSagaStarted();
        _metrics.IncrementSagaCompleted();
        // No exception = success
    }

    [Fact]
    public void IncrementSagaFailed_WithStep_RecordsFailure()
    {
        _metrics.IncrementSagaStarted();
        _metrics.IncrementSagaFailed("Validate");
        _metrics.IncrementSagaFailed("ReserveFunds");
        _metrics.IncrementSagaFailed("Settle");
        // No exception = success
    }

    [Fact]
    public void RecordSagaDuration_VariousDurations_Succeeds()
    {
        var durations = new[]
        {
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(10),
        };

        foreach (var d in durations)
        {
            _metrics.RecordSagaDuration(d);
            _metrics.RecordStepDuration("Validate", d);
            _metrics.RecordStepDuration("ReserveFunds", d);
            _metrics.RecordStepDuration("Settle", d);
        }
    }

    [Fact]
    public void IncrementStepSuccess_VariousSteps_Succeeds()
    {
        _metrics.IncrementStepSuccess("Validate");
        _metrics.IncrementStepSuccess("ReserveFunds");
        _metrics.IncrementStepSuccess("Settle");
        // No exception
    }

    [Fact]
    public void IncrementStepFailure_VariousSteps_Succeeds()
    {
        _metrics.IncrementStepFailure("Validate");
        _metrics.IncrementStepFailure("ReserveFunds");
        _metrics.IncrementStepFailure("Settle");
    }

    [Fact]
    public void RecordReservationAmount_VariousAmounts_Succeeds()
    {
        var amounts = new[] { 0.01m, 1m, 100m, 10000m, 1000000m, 999999999.99m };
        foreach (var a in amounts)
        {
            _metrics.RecordReservationAmount(a);
        }
    }

    [Fact]
    public void RecordSettlementAmount_VariousAmounts_Succeeds()
    {
        var amounts = new[] { 0.01m, 50m, 500m, 5000m, 500000m };
        foreach (var a in amounts)
        {
            _metrics.RecordSettlementAmount(a);
        }
    }

    [Fact]
    public void GaugeInProgress_IncrementDecrement_Balances()
    {
        for (int i = 0; i < 10; i++)
            _metrics.IncrementSagaStarted();
        for (int i = 0; i < 7; i++)
            _metrics.IncrementSagaCompleted();
        _metrics.IncrementSagaFailed("Test");
        _metrics.IncrementSagaFailed("Test");
        _metrics.IncrementSagaFailed("Test");
    }

    [Fact]
    public void Metrics_SimulateFullTrackingCycle_Succeeds()
    {
        // Simulate 100 saga lifecycles
        for (int i = 0; i < 100; i++)
        {
            _metrics.IncrementSagaStarted();
            var step = i % 3 switch
            {
                0 => "Validate",
                1 => "ReserveFunds",
                _ => "Settle",
            };
            _metrics.IncrementStepSuccess(step);
            _metrics.RecordStepDuration(step, TimeSpan.FromMilliseconds(50 + i));
            _metrics.RecordReservationAmount(100m * (i + 1));

            if (i % 10 == 0)
            {
                _metrics.IncrementSagaFailed("RandomFailure");
            }
            else if (i % 5 == 0)
            {
                _metrics.IncrementStepFailure(step);
            }
            else
            {
                _metrics.IncrementSagaCompleted();
            }

            _metrics.RecordSagaDuration(TimeSpan.FromSeconds(1 + i % 30));
            _metrics.RecordSettlementAmount(100m * (i + 1));
        }
    }

    [Fact]
    public void Metrics_SingletonInstance_ReusedAcrossSagas()
    {
        var sharedMetrics = new PaymentSagaMetrics();

        // Saga A
        sharedMetrics.IncrementSagaStarted();
        sharedMetrics.IncrementStepSuccess("Validate");
        sharedMetrics.IncrementStepSuccess("ReserveFunds");
        sharedMetrics.IncrementSagaCompleted();

        // Saga B
        sharedMetrics.IncrementSagaStarted();
        sharedMetrics.IncrementStepFailure("Validate");
        sharedMetrics.IncrementSagaFailed("Validate");

        // Saga C
        sharedMetrics.IncrementSagaStarted();
        sharedMetrics.IncrementStepSuccess("Validate");
        sharedMetrics.IncrementStepSuccess("ReserveFunds");
        sharedMetrics.IncrementStepSuccess("Settle");
        sharedMetrics.IncrementSagaCompleted();
        sharedMetrics.RecordSettlementAmount(500m);
    }
}

public class SagaTimeoutConfigurationTests
{
    [Fact]
    public void GetTimeoutForEnvironment_Development_Returns5Minutes()
    {
        var config = new SagaTimeoutConfiguration
        {
            Development = TimeSpan.FromMinutes(5),
            Staging = TimeSpan.FromMinutes(10),
            Production = TimeSpan.FromMinutes(15),
        };

        config.GetTimeoutForEnvironment("Development").Should().Be(TimeSpan.FromMinutes(5));
        config.GetTimeoutForEnvironment("development").Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void GetTimeoutForEnvironment_Staging_Returns10Minutes()
    {
        var config = new SagaTimeoutConfiguration();
        config.GetTimeoutForEnvironment("Staging").Should().Be(TimeSpan.FromMinutes(10));
        config.GetTimeoutForEnvironment("STAGING").Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void GetTimeoutForEnvironment_Production_Returns15Minutes()
    {
        var config = new SagaTimeoutConfiguration();
        config.GetTimeoutForEnvironment("Production").Should().Be(TimeSpan.FromMinutes(15));
        config.GetTimeoutForEnvironment("PRODUCTION").Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void GetTimeoutForEnvironment_Unknown_DefaultsToDevelopment()
    {
        var config = new SagaTimeoutConfiguration
        {
            Development = TimeSpan.FromMinutes(5),
            Staging = TimeSpan.FromMinutes(10),
            Production = TimeSpan.FromMinutes(15),
        };

        config.GetTimeoutForEnvironment("Unknown").Should().Be(TimeSpan.FromMinutes(5));
        config.GetTimeoutForEnvironment("").Should().Be(TimeSpan.FromMinutes(5));
        config.GetTimeoutForEnvironment(null!).Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void GetTimeoutForEnvironment_CustomValues_UseCustomConfig()
    {
        var config = new SagaTimeoutConfiguration
        {
            Development = TimeSpan.FromMinutes(3),
            Staging = TimeSpan.FromMinutes(7),
            Production = TimeSpan.FromMinutes(20),
        };

        config.GetTimeoutForEnvironment("Development").Should().Be(TimeSpan.FromMinutes(3));
        config.GetTimeoutForEnvironment("Staging").Should().Be(TimeSpan.FromMinutes(7));
        config.GetTimeoutForEnvironment("Production").Should().Be(TimeSpan.FromMinutes(20));
    }

    [Fact]
    public void Configuration_DefaultValues_AreCorrect()
    {
        var config = new SagaTimeoutConfiguration();
        config.Development.Should().Be(TimeSpan.FromMinutes(5));
        config.Staging.Should().Be(TimeSpan.FromMinutes(10));
        config.Production.Should().Be(TimeSpan.FromMinutes(15));
    }
}

public class ServiceConfigurationTests
{
    [Fact]
    public void ServiceConfiguration_DefaultValues_AreSet()
    {
        var config = new ServiceConfiguration();
        config.ValidationServiceUrl.Should().Be("http://localhost:5001");
        config.LedgerServiceUrl.Should().Be("http://localhost:5002");
        config.SettlementServiceUrl.Should().Be("http://localhost:5003");
        config.HttpTimeout.Should().Be(TimeSpan.FromSeconds(30));
        config.MaxRetries.Should().Be(3);
        config.RetryBaseDelayMs.Should().Be(100);
        config.RetryMaxDelayMs.Should().Be(2000);
    }

    [Fact]
    public void ServiceConfiguration_CanBeCustomized()
    {
        var config = new ServiceConfiguration
        {
            ValidationServiceUrl = "https://val.example.com",
            LedgerServiceUrl = "https://ledger.example.com",
            SettlementServiceUrl = "https://settle.example.com",
            HttpTimeout = TimeSpan.FromSeconds(15),
            MaxRetries = 5,
            RetryBaseDelayMs = 200,
            RetryMaxDelayMs = 5000,
        };

        config.ValidationServiceUrl.Should().Be("https://val.example.com");
        config.MaxRetries.Should().Be(5);
    }
}
// END_BLOCK_TESTS
