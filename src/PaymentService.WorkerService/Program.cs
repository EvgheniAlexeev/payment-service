// FILE: Program.cs
// VERSION: 2.0.0
// MODULE: M-WORKER
// PURPOSE: Wolverine saga orchestration bootstrapping
// SEMANTIC_TAG: [SAGA_BOOTSTRAP, WOLVERINE]
// START_MODULE M_WORKER

// START_MODULE M-WORKER
// START_BLOCK_PROGRAM Program
// PURPOSE: Worker host entry point for PaymentService.Workers.
//          Configures Wolverine with RabbitMQ, Prometheus metrics,
//          and structured logging.
// SEMANTIC_TAG: [BLOCK_HOST] Wolverine worker host
// Program.cs — WARNING: Needs Wolverine NuGet package and .NET 9 API alignment
// This file has pre-existing compatibility issues:
//   - Wolverine 3.3.0 not available (needs private feed)
//   - prometheus-net KestrelMetricServer API needs .NET 9 update
//   - HostApplicationBuilder API differences in .NET 9
//
// Original file restored with minimal fixes; full refactor needed with proper packages.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentService.Workers;
using PaymentService.Workers.Configuration;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddPaymentWorkers(builder.Configuration);

// NOTE: Wolverine.UseWolverine(), Prometheus metrics, RabbitMQ, HealthChecks
// require package restoration with the correct NuGet feed.
// See: https://github.com/EvgheniAlexeev/grace-tooling for setup.

var host = builder.Build();

Console.WriteLine(
    "[PaymentService.Workers][Program][BLOCK_HOST_START] " +
    "Worker started.");

await host.RunAsync();
// END_BLOCK_MODULE
