# Architecture

## Approved boundaries

- Power Apps is the administration and provisioning control plane.
- Dataverse stores branch configuration, resource mappings, operation state,
  approved access-scope mappings, taxonomy versions, and exceptions. It is not
  an employee or organization master.
- Azure Functions with Durable Functions runs asynchronous provisioning and
  reconciliation. Terraform is the approved Azure infrastructure-as-code
  standard; the Functions implementation uses the .NET 10 isolated worker.
  Domain and test projects target `net10.0` so shared contracts, policy
  implementations, and test dependencies use the same runtime generation.
- Each trial branch maps to its existing SharePoint-connected Microsoft 365
  group. The solution reconciles a Team on that group and its native Shifts
  schedule rather than creating a second branch membership group.
- The existing Data/BI ALM process owns semantic models, reports, RLS, and
  reporting deployment. This repository owns the published-Shifts extraction
  and governed data contract.

## Request and identity flow

1. A user authenticates to Power Apps with Entra ID.
2. The app sends a user-authenticated request to the Functions API.
3. The API resolves the requested branch from trusted configuration and
   validates the caller's Entra-backed role/scope assignment.
4. Durable Functions serializes state-changing work per branch and uses its
   workload identity for approved Microsoft Graph operations.
5. Dataverse records progress, audit references, resource mappings, and safe
   exceptions. Telemetry carries correlation IDs and avoids unnecessary
   employee data.

The HTTP surface is `/api/branches/{branchId}/users`,
`/api/branches/{branchId}/membership-operations`,
`/api/branches/{branchId}/provisioning-operations`, and
`/api/operations/{operationId}`. State-changing requests require a GUID
`Idempotency-Key`; authorization is repeated by the durable activity before
Graph mutation. Each operation ID is also the Durable Functions instance ID.
Shifts schedule readiness is polled using durable timers, not a custom rota UI.

The Power Apps client is not an authorization boundary. Ordinary Microsoft 365
group membership never grants managerial or support privileges.
Franchise Managers may administer branch membership within their franchise;
that role alone does not grant Team or Shifts reconciliation, which is
restricted to the assigned Branch Manager or an explicitly scoped trial
support administrator.

## Decisions and gates

- ADR-010: Durable Functions is the selected orchestration platform.
- ADR-011: Power BI reports and semantic models remain with the existing
  Data/BI ALM process.
- ADR-012: Trial branch membership uses existing Microsoft 365 groups and
  Power App commands, as recorded in `adr/ADR-012-trial-branch-membership.md`.
- The Graph adapters are implemented against v1.0, but all application roles
  in `security/graph-permission-proposal.json` remain proposed and pending
  platform/security approval. Terraform defaults to granting none.
- G2 approval is required for taxonomy, retention, permissions, lifecycle
  details, reconciliation frequency, performance targets, and quality
  thresholds.

See [the project specification](../README.md) for the implementation status.
