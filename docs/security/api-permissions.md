# Microsoft Graph and connector permission proposal

**Status: pending platform/security approval.** This document and
`graph-permission-proposal.json` are a proposal only. No permission is approved,
consented, or granted by this repository or by the implementation author.
Only the designated platform/security approver may approve the proposal and
the tenant administrator may grant application consent.

| Component | Permission | Type | Operations | Justification | Approval | Review owner |
|---|---|---|---|---|---|---|
| Microsoft Graph | `User.Read.All` | Application | Search and revalidate enabled internal users | Minimal user picker profile for a selected branch membership command | Pending | Platform/security |
| Microsoft Graph | `GroupMember.ReadWrite.All` | Application | Read/add/remove group membership | Apply ordinary membership changes to the existing branch Microsoft 365 group | Pending | Platform/security |
| Microsoft Graph | `Group.ReadWrite.All` | Application | Create a Team on an existing group | Create the Team only when absent for the pre-existing branch group | Pending | Platform/security |
| Microsoft Graph | `Team.ReadBasic.All` | Application | Read Team ID for reconciliation | Avoid duplicate Team creation while only reading basic Team information | Pending | Platform/security |
| Microsoft Graph | `Schedule.ReadWrite.All` | Application | Read/create the native Shifts schedule | Initialize and reconcile the branch's native schedule | Pending | Platform/security |
| Power Apps custom connector | API delegated scope `access_as_user` | Delegated | Call the Functions API | Preserve the signed-in user identity for server-side authorization | Pending | Entra/Power Platform owners |

API references and permission-specific justifications are recorded in the
machine-readable proposal. `Group.ReadWrite.All` is broad; it is included only
because the documented Teams-on-existing-group create operation requires it.
The platform reviewer must verify whether a narrower approved mechanism is
available before granting it.

The Terraform input `graph_permission_approval` defaults to `false`. Keep it
false until the reviewer records their decision through the organization's
approval process. Entra application-role assignment requires privileged
consent and must not be inferred from a pipeline run or a successful build.
The Function API still validates caller signature, tenant, audience, lifetime,
principal, group-overage state, and branch scope before it can use workload
identity permissions.
