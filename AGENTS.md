# GRACE Framework - Project Engineering Protocol

## Keywords
mif, payment-processing, swift, cqrs, dapr, wolverine, mongodb, k8s, reader, writer, worker, saga

## Annotation
PaymentService microservice in MIF (Micro Integrator for Finances). Solution file: **PaymentServices.sln**. .NET 9 CQRS architecture: Reader (query-only) and Writer (command gateway) APIs; Worker saga processor; MongoDB persistence.

## Core Principles
1. **Never Write Code Without a Contract** — MODULE_CONTRACT defines PURPOSE, SCOPE, INPUTS, OUTPUTS
2. **Semantic Markup Is Load-Bearing** — `// START_BLOCK_<NAME>` and `// END_BLOCK_<NAME>` are navigation anchors
3. **Knowledge Graph Is Always Current** — `docs/knowledge-graph.xml` is the project map
4. **Verification Is First-Class** — Testing, traces, log anchors designed before execution
5. **Top-Down Synthesis** — Requirements → Technology → Development → Verification → Code
6. **Governed Autonomy** — Freedom in HOW, not WHAT

## Semantic Markup Reference (C# .NET)

### Module Level
```csharp
// FILE: src/PaymentService.Reader/PaymentQueryHandler.cs
// VERSION: 1.0.0
// START_MODULE_CONTRACT
//   PURPOSE: Query payment status from MongoDB
//   SCOPE: GET /api/payments/{correlationId}, GET /api/payments/batch
//   DEPENDS: M-MONGO, M-SHARED
//   ROLE: RUNTIME
//   MAP_MODE: EXPORTS
// END_MODULE_CONTRACT
//
// START_MODULE_MAP
//   GetPaymentByCorrelationId - Query single payment by correlationId
//   GetPaymentsBatch - Query multiple payments with pagination
// END_MODULE_MAP
```

### Function Level
```csharp
// START_CONTRACT: GetPaymentByCorrelationId
//   PURPOSE: Retrieve payment document by correlationId from MongoDB
//   INPUTS: { correlationId: string - unique payment identifier }
//   OUTPUTS: { PaymentDocument - complete payment record or null }
//   SIDE_EFFECTS: MongoDB read-only query, no mutations
//   LINKS: M-MONGO / IPaymentDocumentRepository
// END_CONTRACT: GetPaymentByCorrelationId
```

### Code Block Level
```csharp
// START_BLOCK_QUERY_MONGODB
var payment = await paymentRepository.GetByCorrelationIdAsync(correlationId);
// END_BLOCK_QUERY_MONGODB
```

## Logging Convention

```csharp
_logger.Information("[Reader][GetPaymentByCorrelationId][BLOCK_QUERY_MONGODB] Querying payment {correlationId}", correlationId);
```

Rules:
- Prefix: `[ModuleName][functionName][BLOCK_NAME]`
- Structured fields only; no prose-heavy strings
- Never log account numbers, amounts, sensitive data
- Redact at Serilog enricher level

## Verification Conventions

`docs/verification-plan.xml` is the project verification contract. Testing rules:
- Deterministic assertions first (xUnit + FakeItEasy)
- Log/trace assertions for saga state transitions (Writer → MQ → Worker)
- Bottom-up integration: MongoDB → Reader → Writer → Worker → saga
- Module-local tests close to modules
- Wave and phase checks explicit

## File Structure
```
PaymentServices.sln
├── src/
│   ├── PaymentService.Shared/          - DTOs, events, commands
│   ├── PaymentService.Api.ReaderService/    - Query API (GET endpoints)
│   ├── PaymentService.Api.WriterService/    - Command API (POST → MQ)
│   ├── PaymentService.WorkerService/   - Saga processor (MQ subscriber)
│   └── PaymentService.Persistence/     - MongoDB repositories
├── tests/
│   ├── PaymentService.Shared.UnitTests/
│   ├── PaymentService.Api.ReaderService.UnitTests/
│   ├── PaymentService.Api.WriterService.UnitTests/
│   ├── PaymentService.WorkerService.UnitTests/
│   ├── PaymentService.Persistence.UnitTests/
│   ├── PaymentService.IntegrationTests/
│   └── PaymentService.LoadTests/
├── deploy/
│   └── k8s/                    - Kubernetes + Dapr manifests
└── docs/
    ├── requirements.xml
    ├── technology.xml
    ├── development-plan.xml
    ├── verification-plan.xml
    ├── knowledge-graph.xml
    └── operational-packets.xml
```

## Rules for Modifications

1. Read MODULE_CONTRACT before editing any file
2. After editing, update MODULE_MAP
3. After adding/removing modules, update docs/knowledge-graph.xml
4. After changing tests/commands/log markers, update docs/verification-plan.xml
5. After fixes, add CHANGE_SUMMARY and strengthen nearby verification
6. Never remove semantic markup anchors unless intentionally replacing
