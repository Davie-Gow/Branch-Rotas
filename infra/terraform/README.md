# Terraform

This root module provisions the .NET 10 isolated Functions host, its storage
and telemetry-ready hosting plan, a system-assigned managed identity, the
user-facing API app registration, and the function host's storage RBAC.

Graph application roles are intentionally **not granted by default**. The
`graph_permission_approval` variable defaults to `false`; it may only be set
to `true` after documented platform/security approval of
`docs/security/graph-permission-proposal.json`. Setting the flag grants tenant-
wide application roles to the Function App identity and therefore requires
authorized Entra admin consent. This module does not create a Dataverse
application user or import a Power Platform solution; those environment-owned
steps are documented in `power-platform/README.md`.

Provide remote state, environment-specific values, and credentials through the
Azure DevOps service connection and variable groups. Do not check credentials,
tenant-specific IDs, state files, or `.tfvars` files into source control.
Configure approvals/checks on the `workforce-prod` Azure DevOps environment
independently of the pipeline parameters. The G2 and Graph booleans are
explicit safeguards, not substitutes for the organization's recorded approval
workflow.
