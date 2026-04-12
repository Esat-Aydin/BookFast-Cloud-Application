output "metadata" {
  description = "Environment metadata and planned resource names for the current Terraform scaffold."
  value = {
    environment_name         = var.environment_name
    location                 = var.location
    resource_group_name      = local.effective_resource_group_name
    create_resource_group    = var.create_resource_group
    planned_modules          = local.planned_modules
    planned_resource_names   = local.planned_resource_names
    service_bus_topic_name   = var.service_bus_topic_name
    service_bus_subscription = var.service_bus_subscription_name
    tags                     = local.normalized_tags
  }
}
