locals {
  default_tags = {
    application = var.app_name
    environment = var.environment_name
    workload    = "integration-platform"
  }

  normalized_tags = merge(local.default_tags, var.tags)

  effective_resource_group_name = var.resource_group_name != "" ? var.resource_group_name : "${var.app_name}-${var.environment_name}-rg"
  unique_token                  = substr(md5(join("-", [var.app_name, var.environment_name, local.effective_resource_group_name, var.location])), 0, 6)
  platform_prefix               = "${var.app_name}-${var.environment_name}"
  key_vault_name                = substr(replace(lower("${var.app_name}${var.environment_name}${local.unique_token}"), "-", ""), 0, 24)
  storage_account_name          = substr(replace(lower("${var.app_name}${var.environment_name}${local.unique_token}sa"), "-", ""), 0, 24)

  planned_modules = [
    "modules/platform",
    "modules/api",
    "modules/functions",
    "modules/messaging",
    "modules/security",
    "modules/observability"
  ]

  planned_resource_names = {
    application_insights       = "${local.platform_prefix}-appi"
    api_management             = "${local.platform_prefix}-apim"
    api_web_app                = "${local.platform_prefix}-api"
    function_app               = "${local.platform_prefix}-func"
    key_vault                  = local.key_vault_name
    log_analytics_workspace    = "${local.platform_prefix}-log"
    service_bus_namespace      = "${local.platform_prefix}-sb"
    service_bus_topic          = var.service_bus_topic_name
    service_bus_subscription   = var.service_bus_subscription_name
    sql_server                 = "${local.platform_prefix}-sql"
    deployment_storage_account = local.storage_account_name
  }
}

resource "azurerm_resource_group" "platform" {
  count    = var.create_resource_group ? 1 : 0
  name     = local.effective_resource_group_name
  location = var.location
  tags     = local.normalized_tags
}
