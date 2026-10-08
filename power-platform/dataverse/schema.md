# Dataverse schema contract

The logical names below are consumed by the Functions adapter. Create the
tables in an unmanaged solution first, then promote managed solutions through
DEV, TEST, and PROD. Values shown as text are deliberately text columns: the
adapter reads their literal string values, not Dataverse choice labels or
formatted values.

| Table logical name (entity set) | Column logical name | Type / constraint | Purpose |
|---|---|---|---|
| `ws_branchconfiguration` (`ws_branchconfigurations`) | `ws_branchid` | Text, required, alternate key | Stable branch identifier |
|  | `ws_franchiseid` | Text, required | Stable franchise scope identifier |
|  | `ws_intrialscope` | Yes/No, required | Explicit trial allow-list |
|  | `ws_lifecyclestate` | Text, required | `NotEnabled`, `Provisioning`, `Active`, `Suspended`, `DecommissionPending`, or `Archived` |
|  | `ws_m365groupid` | Text, required | Existing SharePoint-connected M365 group GUID |
|  | `ws_teamid` | Text, optional | Reconciled Team GUID |
|  | `ws_timezone` | Text, required | Approved IANA timezone for Shifts |
|  | `ws_sharepointsiteurl` | URL/Text, required | Existing branch group's SharePoint site URL |
|  | `ws_lastoperationid` | Text, optional | Most recent lifecycle operation GUID |
|  | `ws_lasterrorcategory` | Text, optional | Sanitized last failure category |
| `ws_accessassignment` (`ws_accessassignments`) | `ws_entraagroupid` | Unique identifier, required | Entra group ID mapped to an operational role |
|  | `ws_role` | Text, required | `BranchManager`, `FranchiseManager`, or `PlatformSupportAdmin` |
|  | `ws_scopetype` | Text, required | `Branch`, `Franchise`, or `Trial` |
|  | `ws_scopeid` | Text, required | Stable ID or reserved `trial` scope |
|  | `ws_effectivefrom`, `ws_effectiveto` | Date/time, optional | Effective assignment window |
| `ws_provisioningoperation` (`ws_provisioningoperations`) | `ws_operationid` | Text, required, alternate key | Caller-supplied GUID idempotency key |
|  | `ws_branchid` | Text, required | Stable branch key |
|  | `ws_operationtype`, `ws_state`, `ws_step` | Text, required | Operation type, state, and sanitized progress |
|  | `ws_correlationid`, `ws_requestedby` | Text, required | Request trace and requester Entra object ID |
|  | `ws_targetuserobjectid`, `ws_action` | Text, optional | Membership audit target and action |
|  | `ws_errorcategory` | Text, optional | Sanitized error category; never raw Graph/Dataverse error bodies |
|  | `ws_startedon`, `ws_completedon` | Date/time, optional | Operation timestamps |

Before use, verify the exact solution publisher prefix and all table/column
logical names against the environment. The repository contract must be changed
in the same release if the tenant schema differs. Enforce unique alternate
keys on `ws_branchid` and `ws_operationid`, and assign the Functions managed
identity only the required read/write privileges on these three tables.
