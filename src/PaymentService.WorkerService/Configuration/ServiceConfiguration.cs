// FILE: ServiceConfiguration.cs
// VERSION: 2.0.0
// MODULE: M-WORKER
// PURPOSE: External service dependency
// SEMANTIC_TAG: [SERVICE_ABSTRACTION, DEPENDENCY]
// START_MODULE M_WORKER

// START_MODULE M-WORKER
// START_BLOCK_CONFIG ServiceConfiguration
// PURPOSE: Configuration for external service endpoints (validation, ledger, settlement).
//          Bound from appsettings.json.
// SEMANTIC_TAG: [BLOCK_CONFIG] External service configuration
namespace PaymentService.Workers.Configuration;

/// <summary>
/// Configuration model for the M-WORKER module
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-WORKER (configuration model)</para>
/// <para><strong>@purpose:</strong> Configuration model for the M-WORKER module</para>
/// <para><strong>@invariant:</strong> Configuration values have sensible defaults; validated at startup</para>
/// <para><strong>@verification-ref:</strong> V-M-WORKER</para>
/// </remarks>

public sealed class ServiceConfiguration
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Services";

    /// <summary>Base URL for the payment validation service.</summary>
    public string ValidationServiceUrl { get; set; } = "http://localhost:5001";

    /// <summary>Base URL for the ledger service.</summary>
    public string LedgerServiceUrl { get; set; } = "http://localhost:5002";

    /// <summary>Base URL for the settlement service.</summary>
    public string SettlementServiceUrl { get; set; } = "http://localhost:5003";

    /// <summary>HTTP timeout for external service calls.</summary>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Number of retries on transient failures.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Base delay for exponential backoff (milliseconds).</summary>
    public int RetryBaseDelayMs { get; set; } = 100;

    /// <summary>Maximum delay for exponential backoff (milliseconds).</summary>
    public int RetryMaxDelayMs { get; set; } = 2000;
}
// END_BLOCK_CONFIG
