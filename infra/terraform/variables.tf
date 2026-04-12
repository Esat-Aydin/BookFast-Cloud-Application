variable "app_name" {
  description = "Short application identifier used in Azure resource names."
  type        = string
  default     = "bookfast"

  validation {
    condition     = length(var.app_name) >= 3 && length(var.app_name) <= 12
    error_message = "app_name must be between 3 and 12 characters."
  }
}

variable "environment_name" {
  description = "Deployment environment name."
  type        = string
  default     = "dev"

  validation {
    condition     = contains(["dev", "test", "prod"], var.environment_name)
    error_message = "environment_name must be one of dev, test, or prod."
  }
}

variable "location" {
  description = "Primary Azure region for the infrastructure scaffold."
  type        = string
  default     = "westeurope"
}

variable "resource_group_name" {
  description = "Existing or planned Azure resource group name."
  type        = string
  default     = ""
}

variable "create_resource_group" {
  description = "Whether Terraform should create the resource group defined by resource_group_name."
  type        = bool
  default     = false
}

variable "service_bus_topic_name" {
  description = "Default Service Bus topic name used by BookFast integration publishing."
  type        = string
  default     = "bookfast.integration"
}

variable "service_bus_subscription_name" {
  description = "Default Service Bus subscription name used by the reporting function."
  type        = string
  default     = "reporting"
}

variable "tags" {
  description = "Tags applied consistently across all managed Azure resources."
  type        = map(string)
  default     = {}
}
