# Atlas PARS Azure reference infrastructure.
# This module models a cost-conscious dev environment, not a full production landing zone.

resource "azurerm_resource_group" "main" {
  name     = local.resource_group_name
  location = var.location
  tags     = local.common_tags
}

resource "azurerm_log_analytics_workspace" "main" {
  name                = local.log_analytics_workspace_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = local.common_tags
}

resource "azurerm_virtual_network" "main" {
  name                = "vnet-${local.name_prefix}-${var.name_suffix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  address_space       = [var.vnet_address_space]
  tags                = local.common_tags
}

resource "azurerm_network_security_group" "container_apps" {
  name                = "nsg-container-apps-${local.name_prefix}-${var.name_suffix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.common_tags
}

resource "azurerm_network_security_group" "postgres" {
  name                = "nsg-postgres-${local.name_prefix}-${var.name_suffix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.common_tags
}

resource "azurerm_subnet" "container_apps" {
  name                 = "snet-container-apps"
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = [var.container_apps_subnet_cidr]

  delegation {
    name = "container-apps-delegation"

    service_delegation {
      name = "Microsoft.App/environments"
      actions = [
        "Microsoft.Network/virtualNetworks/subnets/join/action"
      ]
    }
  }
}

resource "azurerm_subnet_network_security_group_association" "container_apps" {
  subnet_id                 = azurerm_subnet.container_apps.id
  network_security_group_id = azurerm_network_security_group.container_apps.id
}

resource "azurerm_subnet" "postgres" {
  name                 = "snet-postgres"
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = [var.postgres_subnet_cidr]

  delegation {
    name = "postgres-flexible-server-delegation"

    service_delegation {
      name = "Microsoft.DBforPostgreSQL/flexibleServers"
      actions = [
        "Microsoft.Network/virtualNetworks/subnets/join/action"
      ]
    }
  }
}

resource "azurerm_subnet_network_security_group_association" "postgres" {
  subnet_id                 = azurerm_subnet.postgres.id
  network_security_group_id = azurerm_network_security_group.postgres.id
}

resource "azurerm_private_dns_zone" "postgres" {
  name                = "privatelink.postgres.database.azure.com"
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.common_tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "postgres" {
  name                  = "pdns-${local.name_prefix}-${var.name_suffix}"
  resource_group_name   = azurerm_resource_group.main.name
  private_dns_zone_name = azurerm_private_dns_zone.postgres.name
  virtual_network_id    = azurerm_virtual_network.main.id
  registration_enabled  = false
  tags                  = local.common_tags
}

resource "azurerm_container_registry" "main" {
  #checkov:skip=CKV_AZURE_139:Dev PoC uses public ACR endpoint with admin disabled; private endpoint is a production hardening item.
  #checkov:skip=CKV_AZURE_163:Image vulnerability scanning is handled by Trivy in GitHub Actions for this PoC.
  #checkov:skip=CKV_AZURE_164:Trusted image signing is a production supply-chain hardening item.
  #checkov:skip=CKV_AZURE_165:Dev PoC is single-region to control cost; geo-replication belongs in prod.
  #checkov:skip=CKV_AZURE_166:ACR quarantine/verified images are production controls outside this dev baseline.
  #checkov:skip=CKV_AZURE_167:Basic ACR is used for cost; untagged manifest retention requires Premium.
  #checkov:skip=CKV_AZURE_233:Zone-redundant ACR is intentionally deferred for the cost-conscious dev environment.
  #checkov:skip=CKV_AZURE_237:Dedicated data endpoints require Premium ACR and are deferred for prod.

  name                = local.container_registry_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "Basic"
  admin_enabled       = false
  tags                = local.common_tags
}

resource "azurerm_user_assigned_identity" "api" {
  name                = local.user_assigned_identity_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  tags                = local.common_tags
}

resource "azurerm_role_assignment" "api_acr_pull" {
  scope                = azurerm_container_registry.main.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.api.principal_id
}

resource "azurerm_key_vault" "main" {
  #checkov:skip=CKV_AZURE_189:Dev bootstrap keeps public access with default deny and operator IP allowlist; use private endpoint in prod.
  #checkov:skip=CKV2_AZURE_32:Private endpoint is deferred for prod to keep dev apply simple and low cost.

  name                          = local.key_vault_name
  location                      = azurerm_resource_group.main.location
  resource_group_name           = azurerm_resource_group.main.name
  tenant_id                     = data.azurerm_client_config.current.tenant_id
  sku_name                      = "standard"
  rbac_authorization_enabled    = true
  purge_protection_enabled      = true
  soft_delete_retention_days    = 7
  public_network_access_enabled = true
  tags                          = local.common_tags

  network_acls {
    bypass         = "AzureServices"
    default_action = "Deny"
    ip_rules       = var.key_vault_allowed_ip_ranges
  }
}

resource "azurerm_role_assignment" "deployer_key_vault_admin" {
  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = data.azurerm_client_config.current.object_id
}

resource "azurerm_role_assignment" "api_key_vault_secrets_user" {
  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_user_assigned_identity.api.principal_id
}

resource "azurerm_postgresql_flexible_server" "main" {
  #checkov:skip=CKV_AZURE_136:Geo-redundant backups are disabled by default for dev cost control; enable in qa/prod.
  #checkov:skip=CKV2_AZURE_57:Flexible Server uses delegated private subnet and public access disabled instead of Private Endpoint.

  name                          = local.postgresql_server_name
  resource_group_name           = azurerm_resource_group.main.name
  location                      = azurerm_resource_group.main.location
  version                       = "16"
  delegated_subnet_id           = azurerm_subnet.postgres.id
  private_dns_zone_id           = azurerm_private_dns_zone.postgres.id
  public_network_access_enabled = false
  administrator_login           = var.postgres_administrator_login
  administrator_password        = var.postgres_administrator_password
  sku_name                      = var.postgres_sku_name
  storage_mb                    = var.postgres_storage_mb
  backup_retention_days         = var.postgres_backup_retention_days
  geo_redundant_backup_enabled  = var.enable_postgres_geo_redundant_backup
  zone                          = "1"
  tags                          = local.common_tags

  depends_on = [
    azurerm_private_dns_zone_virtual_network_link.postgres
  ]
}

resource "azurerm_postgresql_flexible_server_database" "atlas" {
  name      = local.postgres_database_name
  server_id = azurerm_postgresql_flexible_server.main.id
  charset   = "UTF8"
  collation = "en_US.utf8"
}

resource "azurerm_key_vault_secret" "postgres_connection_string" {
  name            = "ConnectionStrings--Atlas"
  value           = local.postgres_connection_string
  key_vault_id    = azurerm_key_vault.main.id
  content_type    = "text/plain"
  expiration_date = var.secret_expiration_date
  tags            = local.common_tags

  depends_on = [
    azurerm_role_assignment.deployer_key_vault_admin
  ]
}

resource "azurerm_key_vault_secret" "decision_signing_key_id" {
  name            = "FirmaDecisiones--KeyId"
  value           = var.decision_signing_key_id
  key_vault_id    = azurerm_key_vault.main.id
  content_type    = "text/plain"
  expiration_date = var.secret_expiration_date
  tags            = local.common_tags

  depends_on = [
    azurerm_role_assignment.deployer_key_vault_admin
  ]
}

resource "azurerm_key_vault_secret" "decision_signing_key_base64" {
  name            = "FirmaDecisiones--ClaveActivaBase64"
  value           = var.decision_signing_key_base64
  key_vault_id    = azurerm_key_vault.main.id
  content_type    = "text/plain"
  expiration_date = var.secret_expiration_date
  tags            = local.common_tags

  depends_on = [
    azurerm_role_assignment.deployer_key_vault_admin
  ]
}

resource "azurerm_container_app_environment" "main" {
  name                       = local.container_environment_name
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id
  infrastructure_subnet_id   = azurerm_subnet.container_apps.id
  tags                       = local.common_tags
}

resource "azurerm_container_app" "api" {
  name                         = local.container_app_name
  resource_group_name          = azurerm_resource_group.main.name
  container_app_environment_id = azurerm_container_app_environment.main.id
  revision_mode                = "Single"
  tags                         = local.common_tags

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.api.id]
  }

  registry {
    server   = azurerm_container_registry.main.login_server
    identity = azurerm_user_assigned_identity.api.id
  }

  secret {
    name                = "connectionstrings-atlas"
    key_vault_secret_id = azurerm_key_vault_secret.postgres_connection_string.versionless_id
    identity            = azurerm_user_assigned_identity.api.id
  }

  secret {
    name                = "firma-key-id"
    key_vault_secret_id = azurerm_key_vault_secret.decision_signing_key_id.versionless_id
    identity            = azurerm_user_assigned_identity.api.id
  }

  secret {
    name                = "firma-clave-activa-base64"
    key_vault_secret_id = azurerm_key_vault_secret.decision_signing_key_base64.versionless_id
    identity            = azurerm_user_assigned_identity.api.id
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "http"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = var.container_min_replicas
    max_replicas = var.container_max_replicas

    container {
      name   = "atlas-pars-api"
      image  = "${azurerm_container_registry.main.login_server}/${var.container_image_name}"
      cpu    = var.container_cpu
      memory = var.container_memory

      env {
        name  = "ASPNETCORE_HTTP_PORTS"
        value = "8080"
      }

      env {
        name        = "ConnectionStrings__Atlas"
        secret_name = "connectionstrings-atlas"
      }

      env {
        name        = "FirmaDecisiones__KeyId"
        secret_name = "firma-key-id"
      }

      env {
        name        = "FirmaDecisiones__ClaveActivaBase64"
        secret_name = "firma-clave-activa-base64"
      }
    }
  }

  depends_on = [
    azurerm_role_assignment.api_acr_pull,
    azurerm_role_assignment.api_key_vault_secrets_user
  ]
}
