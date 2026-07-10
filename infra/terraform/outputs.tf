output "resource_group_name" {
  description = "Azure resource group created for Atlas PARS."
  value       = azurerm_resource_group.main.name
}

output "container_registry_login_server" {
  description = "ACR login server where the Atlas PARS image must be pushed."
  value       = azurerm_container_registry.main.login_server
}

output "container_app_name" {
  description = "Azure Container App hosting the API."
  value       = azurerm_container_app.api.name
}

output "container_app_url" {
  description = "Public HTTPS URL for the API container app."
  value       = "https://${azurerm_container_app.api.latest_revision_fqdn}"
}

output "postgres_fqdn" {
  description = "Private PostgreSQL Flexible Server FQDN."
  value       = azurerm_postgresql_flexible_server.main.fqdn
}

output "key_vault_uri" {
  description = "Key Vault URI for runtime secrets."
  value       = azurerm_key_vault.main.vault_uri
}
