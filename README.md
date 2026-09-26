# Enterprise Azure Functions Triggers in .NET

This repository contains an enterprise-ready Azure Functions reference solution using the current .NET isolated worker pattern for Azure Functions runtime v4.

The solution demonstrates major Azure Functions trigger families with realistic scenarios, production-oriented configuration, dependency injection, structured logging, health/readiness endpoints, tests, CI/CD workflows, infrastructure templates, and optional containerization.

## What is included

- HTTP APIs for order intake, status, liveness, and readiness.
- Timer jobs for reconciliation and cache warmup.
- Azure Storage Queue and Blob triggers.
- Azure Service Bus queue and topic/subscription triggers.
- Azure Event Hubs and Event Grid triggers.
- Cosmos DB change feed trigger.
- Azure SQL trigger for change tracking.
- Durable Functions orchestration for a claims workflow.
- Central package management with versioned isolated-worker packages.
- `host.json` and `local.settings.sample.json`.
- GitHub Actions CI and deployment workflow.
- Azure DevOps pipeline starter.
- Bicep and Terraform deployment starters.
- Dockerfile for containerized Functions hosting.
- xUnit tests for shared enterprise workflow logic.

## Solution structure

```text
.
|-- src/
|   `-- Enterprise.Functions/
|       |-- Abstractions/
|       |-- Configuration/
|       |-- Functions/
|       |-- Middleware/
|       |-- Models/
|       |-- Services/
|       |-- Enterprise.Functions.csproj
|       |-- host.json
|       `-- local.settings.sample.json
|-- tests/
|   `-- Enterprise.Functions.Tests/
|-- infra/
|   |-- bicep/
|   `-- terraform/
|-- docs/
|   `-- architecture.md
|-- .github/workflows/
|-- Dockerfile
|-- azure-pipelines.yml
|-- Directory.Build.props
|-- Directory.Packages.props
`-- Enterprise.AzureFunctions.sln
```

## Prerequisites

- .NET SDK 9.x installed. The app targets .NET 8 LTS and builds with the SDK pinned in `global.json`.
- Azure Functions Core Tools v4 for local execution.
- Azurite for local storage emulation, or a real Azure Storage connection string.
- Azure subscription for deployment.

## Run locally

1. Copy `src/Enterprise.Functions/local.settings.sample.json` to `src/Enterprise.Functions/local.settings.json`.
2. Replace placeholder connection strings with local emulator or Azure resource values.
3. Start Azurite if using `UseDevelopmentStorage=true`.
4. Restore, build, and run:

```powershell
dotnet restore Enterprise.AzureFunctions.sln
dotnet build Enterprise.AzureFunctions.sln
cd src/Enterprise.Functions
func start
```

Useful local endpoints:

- `GET /api/health/live`
- `GET /api/health/ready`
- `POST /api/orders`
- `POST /api/claims`

## Test

```powershell
dotnet test Enterprise.AzureFunctions.sln
```

## Deploy with GitHub Actions

Configure these GitHub environment variables:

- `AZURE_RESOURCE_GROUP`
- `AZURE_LOCATION`
- `FUNCTION_APP_NAME`

Configure these GitHub environment secrets for federated Azure login:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

Run `.github/workflows/deploy.yml` manually and choose the target environment.

## Infrastructure

`infra/bicep/main.bicep` provisions the core hosting and messaging resources:

- Storage account
- Linux Function App
- Flex Consumption hosting plan
- Application Insights
- Service Bus namespace, queue, topic, and subscription
- Event Hubs namespace and event hub
- Cosmos DB account
- Azure SignalR Service

`infra/terraform/main.tf` is a starter Terraform equivalent for teams standardizing on Terraform.

## Container deployment

Build the optional Functions container image:

```powershell
docker build -t enterprise-functions:local .
```

The container uses the official Azure Functions .NET isolated runtime image for Functions v4 and .NET 8.

## Package compatibility choices

The solution intentionally uses:

- `net8.0` for LTS support.
- Azure Functions runtime `v4`.
- `Azure.Functions.Sdk` `1.0.1`.
- `Microsoft.Azure.Functions.Worker` `2.51.0`.
- Current split binding extensions for Storage, Service Bus, Event Hubs, Event Grid, Cosmos DB, Durable Functions, SQL, SignalR, HTTP, and Timer.

The package versions are centralized in `Directory.Packages.props`.

## Production hardening checklist

- Move secrets to Azure Key Vault references or managed identity based connections.
- Add private endpoints and VNet integration for regulated environments.
- Add Service Bus dead-letter monitoring and replay procedures.
- Add Application Insights alerts for failures, dependency latency, and queue age.
- Add automated Bicep/Terraform validation in CI.
- Add integration tests against ephemeral Azure resources for binding behavior.
