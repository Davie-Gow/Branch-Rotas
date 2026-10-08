# Branch-Rotas

Workforce scheduling administration and provisioning for branch teams.

Microsoft Teams Shifts remains the scheduling and employee self-service
experience. Power Apps is the administration control plane, and the existing
Data/BI process owns Power BI models and reports.

## Repository status

The repository contains the .NET 10 domain and Azure Functions control-plane
implementation, Graph/Dataverse adapters, a proposed permission manifest,
Terraform infrastructure, an Azure DevOps pipeline, and a source-controlled
Power Platform connector/schema contract. Tenant-owned Power Platform solution
packaging and the production approval gates remain environment setup items;
the Graph roles remain unapproved and are not granted by default.

## Implemented scope

- Authenticated Functions routes for internal user search, idempotent
  membership commands, branch reconciliation, and requester-only operation
  status.
- Server-side branch/franchise scope checks, durable activity workflows,
  per-branch renewable Blob leases, and Graph/Dataverse service adapters.
- Terraform for the Function App, storage, monitoring, API registration, and
  role-based host storage access. Graph application role assignment is
  disabled unless the platform/security approval gate is explicitly enabled.
- Azure DevOps build/test/promotion pipeline with G2 and environment gates.
- Custom connector OpenAPI definition and Dataverse schema contract under
  `power-platform/`.

## Build and test

Requires the .NET 10 SDK.

```powershell
dotnet test tests\WorkforceScheduling.Domain.Tests\WorkforceScheduling.Domain.Tests.csproj
dotnet test tests\WorkforceScheduling.Functions.Tests\WorkforceScheduling.Functions.Tests.csproj
dotnet build src\WorkforceScheduling.Functions\WorkforceScheduling.Functions.csproj
```

The Functions configuration contract uses `Entra__TenantId`,
`Entra__Audience`, `Workforce__DataverseUrl`,
`Workforce__DataverseApiVersion`, `Workforce__GraphScope`, and
`Workforce__LockStorageServiceUri`. The
`Workforce__ScheduleProvisioningTimeoutMinutes` and
`Workforce__SchedulePollingIntervalSeconds` settings must be supplied from
G2-approved values; the repository deliberately defines no operational
threshold defaults. The Terraform `log_retention_days` input also requires a
G2-approved value. Functions host storage settings are also required. Supply
values through local secret settings or Azure environment configuration,
never source control.

See `docs/security/api-permissions.md` for the unapproved Graph proposal and
`power-platform/README.md` for the environment-owned solution packaging steps.
