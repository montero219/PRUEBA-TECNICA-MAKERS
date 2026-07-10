locals {
  name_prefix     = "${var.project_name}-${var.environment}"
  compact_project = replace(var.project_name, "-", "")
  compact_env     = replace(var.environment, "-", "")

  resource_group_name          = "rg-${local.name_prefix}-${var.name_suffix}"
  log_analytics_workspace_name = "law-${local.name_prefix}-${var.name_suffix}"
  container_registry_name      = substr("acr${local.compact_project}${local.compact_env}${var.name_suffix}", 0, 50)
  key_vault_name               = substr("kv-${local.compact_project}-${local.compact_env}-${var.name_suffix}", 0, 24)
  postgresql_server_name       = "psql-${local.name_prefix}-${var.name_suffix}"
  container_environment_name   = "cae-${local.name_prefix}-${var.name_suffix}"
  container_app_name           = "ca-${local.name_prefix}-${var.name_suffix}"
  user_assigned_identity_name  = "id-${local.name_prefix}-${var.name_suffix}"

  postgres_database_name = "atlas_pars"

  postgres_connection_string = "Host=${azurerm_postgresql_flexible_server.main.fqdn};Port=5432;Database=${local.postgres_database_name};Username=${var.postgres_administrator_login};Password=${var.postgres_administrator_password};SSL Mode=Require;Trust Server Certificate=true"

  common_tags = {
    app         = "atlas-pars"
    environment = var.environment
    managed_by  = "terraform"
    owner       = "makers-technical-test"
  }
}
