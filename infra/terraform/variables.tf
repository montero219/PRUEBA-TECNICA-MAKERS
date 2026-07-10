variable "project_name" {
  description = "Short project slug used in Azure resource names."
  type        = string
  default     = "atlas-pars"

  validation {
    condition     = can(regex("^[a-z0-9-]{3,20}$", var.project_name))
    error_message = "project_name must use 3-20 lowercase letters, numbers, or hyphens."
  }
}

variable "environment" {
  description = "Deployment environment name."
  type        = string
  default     = "dev"

  validation {
    condition     = contains(["dev", "qa", "prod"], var.environment)
    error_message = "environment must be dev, qa, or prod."
  }
}

variable "name_suffix" {
  description = "Short lowercase suffix to reduce collisions for globally unique Azure names."
  type        = string
  default     = "001"

  validation {
    condition     = can(regex("^[a-z0-9]{3,8}$", var.name_suffix))
    error_message = "name_suffix must use 3-8 lowercase letters or numbers."
  }
}

variable "location" {
  description = "Azure region for all resources."
  type        = string
  default     = "eastus2"
}

variable "container_image_name" {
  description = "Image name and tag already pushed to the Azure Container Registry, for example atlas-pars:latest."
  type        = string
  default     = "atlas-pars:latest"
}

variable "postgres_administrator_login" {
  description = "PostgreSQL administrator username."
  type        = string
  default     = "atlas_pars_admin"
  sensitive   = true
}

variable "postgres_administrator_password" {
  description = "PostgreSQL administrator password. Provide through a local tfvars file or CI secret."
  type        = string
  sensitive   = true

  validation {
    condition     = length(var.postgres_administrator_password) >= 16
    error_message = "postgres_administrator_password must be at least 16 characters."
  }
}

variable "postgres_sku_name" {
  description = "Azure PostgreSQL Flexible Server SKU for the PoC environment."
  type        = string
  default     = "B_Standard_B1ms"
}

variable "postgres_storage_mb" {
  description = "PostgreSQL storage in MB."
  type        = number
  default     = 32768
}

variable "postgres_backup_retention_days" {
  description = "PostgreSQL backup retention window in days."
  type        = number
  default     = 7
}

variable "enable_postgres_geo_redundant_backup" {
  description = "Enable geo-redundant backups. Keep false in dev to control cost; enable for production."
  type        = bool
  default     = false
}

variable "decision_signing_key_id" {
  description = "Logical key id exposed by the authorization API."
  type        = string
  default     = "atlas-pars-hmac-dev"
}

variable "decision_signing_key_base64" {
  description = "Base64 HMAC key used by the authorization API. Provide through a local tfvars file or CI secret."
  type        = string
  sensitive   = true
}

variable "secret_expiration_date" {
  description = "RFC3339 expiration date applied to bootstrap Key Vault secrets."
  type        = string
  default     = "2027-12-31T23:59:59Z"
}

variable "vnet_address_space" {
  description = "CIDR range for the Atlas PARS virtual network."
  type        = string
  default     = "10.42.0.0/16"
}

variable "container_apps_subnet_cidr" {
  description = "Dedicated subnet for Azure Container Apps environment."
  type        = string
  default     = "10.42.0.0/23"
}

variable "postgres_subnet_cidr" {
  description = "Delegated subnet for PostgreSQL Flexible Server private access."
  type        = string
  default     = "10.42.2.0/24"
}

variable "key_vault_allowed_ip_ranges" {
  description = "Public IP CIDR ranges allowed to write bootstrap secrets during terraform apply."
  type        = list(string)
  default     = []
}

variable "container_cpu" {
  description = "CPU allocated to the API container."
  type        = number
  default     = 0.5
}

variable "container_memory" {
  description = "Memory allocated to the API container."
  type        = string
  default     = "1Gi"
}

variable "container_min_replicas" {
  description = "Minimum API replicas."
  type        = number
  default     = 0
}

variable "container_max_replicas" {
  description = "Maximum API replicas."
  type        = number
  default     = 2
}
