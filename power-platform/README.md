# Power Platform control plane

Power Apps is an administration control plane only. Schedulers and employees
continue to use the native Microsoft Teams Shifts experience; this solution
must not create a parallel rota editor.

## Source-controlled connector

`connectors/branch-rotas-api.swagger.json` defines the custom connector
contract for the authenticated Functions API. The connector uses the
delegated `access_as_user` scope exposed by the API app registration in
Terraform. The API validates the user's Entra token and independently applies
branch/franchise scope checks; hiding controls in the app is not authorization.
It is a connector contract, not a packaged Canvas app.

When creating the connector, replace the tenant ID, API audience, and Function
host placeholders in the OpenAPI file with environment variables in the
Power Platform solution. Configure the OAuth client/redirect URI through the
approved Entra/Power Platform process; do not put client secrets in this
repository.

## Dataverse schema contract

`dataverse/schema.md` defines the logical table and column names currently used
by the Functions adapter. The tables, alternate keys, application-user role,
and a Power Apps canvas app must be created/imported into each tenant's Power
Platform solution with PAC after the platform owner assigns solution publisher
and environment-specific IDs. No tenant-specific publisher prefix or existing
solution identity was supplied, so this repository does not claim to contain
an importable `.zip`/`.msapp` package.

The control-plane screens should expose only allow-listed trial branches and
support branch/franchise-scoped user search, membership commands, reconciliation,
and operation status. The platform owner must configure the Dataverse
application user for least-privilege access to these tables and configure
Power Platform environment-variable values for the connector's API URL.
