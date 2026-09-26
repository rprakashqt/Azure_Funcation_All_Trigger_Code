# Architecture

This repository is a reference-grade Azure Functions solution using the .NET isolated worker model on Azure Functions runtime v4.

## Trigger coverage

| Trigger | Function | Enterprise scenario |
| --- | --- | --- |
| HTTP | `SubmitOrderHttp`, `OrderStatusHttp` | Order intake and status lookup APIs |
| HTTP | `HealthLiveHttp`, `HealthReadyHttp` | Liveness and readiness endpoints for platform probes |
| Timer | `NightlyOrderReconciliationTimer`, `CacheWarmupTimer` | Finance reconciliation and operational cache refresh |
| Storage Queue | `FulfillOrderQueue`, `DeadLetterAuditQueue` | Fulfillment command processing and poison message audit |
| Blob Storage | `InboundDocumentBlob` | Inbound invoice and document ingestion |
| Service Bus Queue | `PaymentCapturedServiceBusQueue` | Payment posting into finance systems |
| Service Bus Topic | `OrderEventTopicForLoyalty` | Loyalty projection from order domain events |
| Event Hubs | `DeviceTelemetryEventHub` | High-volume device telemetry ingestion |
| Event Grid | `StorageLifecycleEventGrid` | Cloud event notification handling |
| Cosmos DB | `CustomerProfileCosmosChangeFeed` | Customer profile projection from change feed |
| Azure SQL | `ShipmentSqlChangeTracking` | Shipment table change tracking |
| Durable Functions | `StartClaimsWorkflowHttp`, `ClaimsWorkflowOrchestrator` | Long-running insurance claim workflow |

## Runtime pattern

- `.NET 8` target framework for LTS runtime support.
- Azure Functions runtime `v4`.
- `Azure.Functions.Sdk` with isolated worker packages, not in-process `Microsoft.NET.Sdk.Functions`.
- Central package management in `Directory.Packages.props`.
- `host.json` configures durable, event hub, queue, service bus, logging, concurrency, and health monitor settings.
- `local.settings.sample.json` documents local-only settings while the real `local.settings.json` remains ignored.
- Dependency injection is configured in `Program.cs`.
- `ExceptionHandlingMiddleware` provides consistent structured error logging across all triggers.
- App Insights worker telemetry is enabled for production observability.

## Deployment options

- GitHub Actions CI in `.github/workflows/ci.yml`.
- GitHub Actions infrastructure and application deployment in `.github/workflows/deploy.yml`.
- Azure DevOps starter pipeline in `azure-pipelines.yml`.
- Bicep template in `infra/bicep/main.bicep`.
- Terraform starter in `infra/terraform/main.tf`.
- Optional container deployment through `Dockerfile`.

## Production notes

- Prefer managed identity and Key Vault references for production connection settings.
- Use deployment slots for App Service plan deployments where available.
- Use Application Insights availability tests against `/api/health/live` and `/api/health/ready`.
- Tune concurrency values per workload and downstream system limits.
- Keep queue and Service Bus dead-letter processes wired into incident management.
