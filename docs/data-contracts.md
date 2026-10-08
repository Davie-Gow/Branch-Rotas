# Data and integration contracts

## Stable identifiers

Use immutable identifiers for joins and authorization: `BranchId`,
`FranchiseId`, and Entra object IDs. Names and labels are display-only.
Identifier formats remain source-defined; domain validation rejects blank or
whitespace-padded values but does not invent a format.

## Branch configuration

The trusted branch configuration required by the initial domain policy
contains:

| Field | Purpose |
|---|---|
| `BranchId` | Stable branch key |
| `FranchiseId` | Stable franchise authorization scope |
| Trial-scope flag | Explicit allow-list for this trial |
| Lifecycle state | `NotEnabled`, `Provisioning`, `Active`, `Suspended`, `DecommissionPending`, or `Archived` |
| Microsoft 365 group ID | Mapping to the existing SharePoint-connected group |
| SharePoint site URL | Branch reference/navigation |

The implemented adapter uses the proposed logical names documented in
`../power-platform/dataverse/schema.md`. Verify them against the tenant
solution before deployment; they remain a repository-owned contract, not
evidence that tenant Dataverse tables or permissions have been provisioned.

## Membership and privileged scope

- Power App commands request add/remove operations for enabled internal Entra
  accounts on an explicitly selected branch group.
- Staff may be members of multiple branch groups.
- Entra security-group assignments map to branch/franchise manager scope.
- Branch Managers manage assigned branches; Franchise Managers manage their
  authorised franchise branches; Platform/support admins operate within the
  explicitly approved trial scope.
- Membership in a branch group does not imply any privileged role.
- Only approved, versioned classification codes may be promoted to production.

## Error contract

Errors use the controlled categories `Validation`, `Authorisation`,
`MasterData`, `Configuration`, `Dependency`, `Throttling`, `Transient`,
`Conflict`, `Permanent`, and `Unknown`. Each result includes retryability, a
safe user message, diagnostic reference, correlation ID, and originating
component. Raw Graph or internal exception details must not be exposed to
users.
