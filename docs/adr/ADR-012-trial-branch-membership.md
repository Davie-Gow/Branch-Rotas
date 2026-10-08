# ADR-012: Trial branch membership and group reuse

- **Status:** Accepted for the trial; platform/security approval remains
  required before Graph integration.
- **Context:** Each branch already has a SharePoint site connected to a
  Microsoft 365 group. The trial requires a manager-facing membership interface
  and must not create a competing staff or organization master.
- **Decision:** Dataverse branch configuration maps a stable `BranchId` and
  `FranchiseId` to the existing Microsoft 365 group ID and site URL. The Power
  App requests ordinary membership changes through a server-authorized
  Functions API. The Functions workload identity applies approved changes to
  that existing group. Branch Managers may manage assigned branches;
  Franchise Managers may manage branches in their assigned franchises; support
  admins are limited to the approved trial scope. Entra security-group
  assignments independently provide privileged role scope. Staff may belong
  to multiple branch groups.
- **Team and Shifts:** Reconcile a Team on the existing group and its native
  Shifts schedule. Assigned Branch Managers receive schedule-management
  rights. Franchise Managers manage membership through the app but do not
  receive schedule-management rights by virtue of that role.
- **Lifecycle:** Deactivation defaults to `Suspended`, retaining the group,
  Team, schedule, and history while blocking new operational changes.
  Decommissioning and archival are separate, approved workflows; physical
  deletion is not the default.
- **Consequences:** The repository must maintain the branch-to-group mapping,
  validate every command server-side, log actor and correlation metadata, and
  reconcile drift. Exact Graph permissions, app-registration details, naming,
  retention, and archival controls require review and approval before
  deployment.
