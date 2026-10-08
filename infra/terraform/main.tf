data "azurerm_client_config" "current" {}

data "azurerm_role_definition" "blob_data_owner" {
  name = "Storage Blob Data Owner"
}

data "azurerm_role_definition" "queue_data_contributor" {
  name = "Storage Queue Data Contributor"
}

data "azurerm_role_definition" "table_data_contributor" {
  name = "Storage Table Data Contributor"
}

resource "azurerm_log_analytics_workspace" "workforce" {
  name                = "${var.function_app_name}-logs"
  resource_group_name = azurerm_resource_group.workforce.name
  location            = azurerm_resource_group.workforce.location
  sku                 = "PerGB2018"
  retention_in_days   = var.log_retention_days
}

resource "azurerm_application_insights" "workforce" {
  name                = "${var.function_app_name}-insights"
  resource_group_name = azurerm_resource_group.workforce.name
  location            = azurerm_resource_group.workforce.location
  application_type    = "web"
  workspace_id        = azurerm_log_analytics_workspace.workforce.id
}

data "azuread_service_principal" "microsoft_graph" {
  client_id = "00000003-0000-0000-c000-000000000000"
}

resource "azurerm_resource_group" "workforce" {
  name     = var.resource_group_name
  location = var.location
}

resource "azurerm_storage_account" "function" {
  name                            = var.storage_account_name
  resource_group_name             = azurerm_resource_group.workforce.name
  location                        = azurerm_resource_group.workforce.location
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false
  shared_access_key_enabled      = false
  public_network_access_enabled  = true
}

resource "azurerm_service_plan" "function" {
  name                = "${var.function_app_name}-plan"
  resource_group_name = azurerm_resource_group.workforce.name
  location            = azurerm_resource_group.workforce.location
  os_type             = "Linux"
  sku_name            = "EP1"
}

resource "azurerm_linux_function_app" "api" {
  name                          = var.function_app_name
  resource_group_name           = azurerm_resource_group.workforce.name
  location                      = azurerm_resource_group.workforce.location
  service_plan_id               = azurerm_service_plan.function.id
  storage_account_name          = azurerm_storage_account.function.name
  storage_uses_managed_identity = true
  https_only                    = true
  functions_extension_version   = "~4"
  builtin_logging_enabled       = false
  ftp_publish_basic_authentication_enabled = false
  webdeploy_publish_basic_authentication_enabled = false

  identity {
    type = "SystemAssigned"
  }

  site_config {
    always_on          = true
    minimum_tls_version = "1.2"
    application_stack {
      dotnet_version              = "10.0"
      use_dotnet_isolated_runtime = true
    }
  }

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet-isolated"
    FUNCTIONS_EXTENSION_VERSION    = "~4"
    WEBSITE_RUN_FROM_PACKAGE       = "1"
    APPLICATIONINSIGHTS_CONNECTION_STRING = azurerm_application_insights.workforce.connection_string
    AzureWebJobsStorage__accountName = azurerm_storage_account.function.name
    AzureWebJobsStorage__credential  = "managedidentity"
    "Entra__TenantId"              = var.entra_tenant_id
    "Entra__Audience"              = var.api_identifier_uri
    "Workforce__DataverseUrl"      = var.dataverse_url
    "Workforce__DataverseApiVersion" = "v9.2"
    "Workforce__GraphScope"        = "https://graph.microsoft.com/.default"
    "Workforce__LockStorageServiceUri" = azurerm_storage_account.function.primary_blob_endpoint
    "Workforce__ManagedIdentityClientId" = ""
    "Workforce__ScheduleProvisioningTimeoutMinutes" = tostring(var.schedule_provisioning_timeout_minutes)
    "Workforce__SchedulePollingIntervalSeconds" = tostring(var.schedule_polling_interval_seconds)
  }

  lifecycle {
    ignore_changes = [app_settings["WEBSITE_RUN_FROM_PACKAGE"]]
  }
}

resource "azurerm_role_assignment" "function_blob" {
  scope              = azurerm_storage_account.function.id
  role_definition_id = data.azurerm_role_definition.blob_data_owner.id
  principal_id       = azurerm_linux_function_app.api.identity[0].principal_id
}

resource "azurerm_role_assignment" "function_queue" {
  scope              = azurerm_storage_account.function.id
  role_definition_id = data.azurerm_role_definition.queue_data_contributor.id
  principal_id       = azurerm_linux_function_app.api.identity[0].principal_id
}

resource "azurerm_role_assignment" "function_table" {
  scope              = azurerm_storage_account.function.id
  role_definition_id = data.azurerm_role_definition.table_data_contributor.id
  principal_id       = azurerm_linux_function_app.api.identity[0].principal_id
}

resource "azuread_application" "api" {
  display_name          = "${var.function_app_name}-api"
  identifier_uris       = [var.api_identifier_uri]
  sign_in_audience      = "AzureADMyOrg"
  group_membership_claims = ["SecurityGroup"]

  api {
    requested_access_token_version = 2

    oauth2_permission_scope {
      admin_consent_description  = "Allows an authenticated workforce scheduling user to call the Branch Rotas API."
      admin_consent_display_name = "Access Branch Rotas API"
      enabled                   = true
      id                        = random_uuid.api_scope.result
      type                      = "User"
      value                     = "access_as_user"
    }
  }
}

resource "random_uuid" "api_scope" {}

resource "azuread_service_principal" "api" {
  client_id = azuread_application.api.client_id
}

locals {
  proposed_graph_roles = var.graph_permission_approval ? toset([
    "User.Read.All",
    "GroupMember.ReadWrite.All",
    "Group.ReadWrite.All",
    "Team.ReadBasic.All",
    "Schedule.ReadWrite.All",
  ]) : toset([])
  graph_roles_by_value = {
    for role in data.azuread_service_principal.microsoft_graph.app_roles :
    role.value => role
    if contains(role.allowed_member_types, "Application")
  }
}

resource "azuread_app_role_assignment" "function_graph" {
  for_each = local.proposed_graph_roles

  app_role_id         = local.graph_roles_by_value[each.value].id
  principal_object_id = azurerm_linux_function_app.api.identity[0].principal_id
  resource_object_id  = data.azuread_service_principal.microsoft_graph.object_id
}
