variable "resource_group_name" {
  type        = string
  description = "Resource group to contain the workforce scheduling Function App."
}

variable "location" {
  type        = string
  description = "Approved Azure region."
}

variable "function_app_name" {
  type        = string
  description = "Globally unique Azure Function App name."
}

variable "storage_account_name" {
  type        = string
  description = "Globally unique storage account name (3-24 lowercase alphanumeric characters)."

  validation {
    condition     = can(regex("^[a-z0-9]{3,24}$", var.storage_account_name))
    error_message = "Storage account names must contain 3-24 lowercase alphanumeric characters."
  }
}

variable "entra_tenant_id" {
  type        = string
  description = "Entra tenant ID for inbound API token validation."
}

variable "api_identifier_uri" {
  type        = string
  description = "Approved API audience URI, for example api://<API application client ID>."
}

variable "dataverse_url" {
  type        = string
  description = "Dataverse environment URL."
}

variable "schedule_provisioning_timeout_minutes" {
  type        = number
  description = "G2-approved timeout for native Shifts schedule readiness; no default is provided."

  validation {
    condition     = var.schedule_provisioning_timeout_minutes > 0
    error_message = "Set a positive G2-approved schedule readiness timeout."
  }
}

variable "schedule_polling_interval_seconds" {
  type        = number
  description = "G2-approved durable polling interval for Shifts readiness; no default is provided."

  validation {
    condition     = var.schedule_polling_interval_seconds > 0
    error_message = "Set a positive G2-approved schedule polling interval."
  }
}

variable "log_retention_days" {
  type        = number
  description = "G2-approved monitoring log retention period; no default is provided."

  validation {
    condition     = var.log_retention_days > 0
    error_message = "Set a positive G2-approved monitoring retention period."
  }
}

variable "graph_permission_approval" {
  type        = bool
  description = "Set true only after recorded platform/security approval of the Graph permission proposal."
  default     = false
}
