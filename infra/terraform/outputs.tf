output "function_app_name" {
  value = azurerm_linux_function_app.api.name
}

output "function_app_default_hostname" {
  value = azurerm_linux_function_app.api.default_hostname
}

output "function_managed_identity_principal_id" {
  value = azurerm_linux_function_app.api.identity[0].principal_id
}

output "api_application_client_id" {
  value = azuread_application.api.client_id
}

output "api_identifier_uri" {
  value = var.api_identifier_uri
}

output "storage_account_name" {
  value = azurerm_storage_account.function.name
}
