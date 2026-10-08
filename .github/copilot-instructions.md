# Copilot implementation rules

- Cite the requirement IDs implemented by each change and update their tests.
- Treat Teams Shifts as the rota and staff self-service experience; do not build a rota editor.
- Enforce every privileged command server-side using the authenticated Entra principal and stable branch/franchise scope.
- Reuse the existing SharePoint-connected Microsoft 365 group for branch membership; do not provision a parallel membership group.
- Keep ordinary group membership separate from managerial roles and scope assignments.
- Keep integration boundaries behind interfaces. Use only documented Graph operations. Treat the checked-in Graph role list and Dataverse schema as proposals/contracts until the platform owners approve and provision them.
- Make provisioning idempotent, resumable, concurrency-safe, and observable.
- Do not commit credentials, tenant-specific identifiers, or production configuration.
- Add concise comments for security boundaries and non-obvious policy only.
