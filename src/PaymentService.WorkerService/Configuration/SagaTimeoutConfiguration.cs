// FILE: SagaTimeoutConfiguration.cs
// VERSION: 2.0.0
// MODULE: M-WORKER
// PURPOSE: External service dependency
// SEMANTIC_TAG: [SERVICE_ABSTRACTION, DEPENDENCY]
// START_MODULE M_WORKER

// START_MODULE M-WORKER
// START_BLOCK_CONFIG SagaTimeoutConfiguration
// PURPOSE: Configurable saga timeouts per environment.
//          Development: 5min | Staging: 10min | Production: 15min
// SEMANTIC_TAG: [BLOCK_CONFIG] Wolverine saga timeout configuration
namespace PaymentService.Workers.Configuration;

/// <summary>
/// Configuration for Wolverine saga timeouts, bound from appsettings.json.
/// </summary>
public sealed class SagaTimeoutConfiguration
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Wolverine:SagaTimeout";

    /// <summary>Development environment timeout (default: 5 minutes).</summary>
    public TimeSpan Development { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Staging environment timeout (default: 10 minutes).</summary>
    public TimeSpan Staging { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Production environment timeout (default: 15 minutes).</summary>
    public TimeSpan Production { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Get the appropriate timeout for the current environment.
    /// Falls back to Development if environment is unrecognized.
    /// </summary>
    public TimeSpan GetTimeoutForEnvironment(string environment)
    {
        return environment?.ToUpperInvariant() switch
        {
            "PRODUCTION" => Production,
            "STAGING" => Staging,
            _ => Development,
        };
    }
}
// END_BLOCK_CONFIG
