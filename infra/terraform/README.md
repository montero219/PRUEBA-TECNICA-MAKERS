# Atlas PARS Terraform

This folder defines a reference Azure environment for Atlas PARS. It is intended for a cost-conscious `dev` deployment that can be reviewed, scanned, and later promoted to `qa` or `prod`.

## Scope

Included:

- Resource Group.
- Virtual Network with dedicated subnets for Container Apps and PostgreSQL.
- Azure Container Registry with admin access disabled.
- Azure Container Apps environment and API container app.
- Azure Database for PostgreSQL Flexible Server with private network access.
- Azure Key Vault for the PostgreSQL connection string and HMAC signing configuration.
- Log Analytics workspace for container logs.

Not included:

- Production-grade landing zone, hub/spoke networking, WAF, DDoS protection, or private endpoints for every service.
- GitHub OIDC deployment wiring.
- Real secret values.
- Applying migrations automatically during deployment.

## Files

- `providers.tf`: Terraform and AzureRM provider requirements.
- `variables.tf`: Input contract, including sensitive values.
- `locals.tf`: Naming, tags, and derived connection string.
- `main.tf`: Azure resources.
- `outputs.tf`: Values needed by deployment and review.
- `environments/dev/dev.tfvars.example`: Non-secret example for the dev environment.

## Usage

Copy the example variables file and replace placeholders locally:

```powershell
Copy-Item infra\terraform\environments\dev\dev.tfvars.example infra\terraform\environments\dev\dev.tfvars
```

Generate the HMAC key value:

```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
```

Allow the operator or CI runner public IP to write initial Key Vault secrets:

```powershell
(Invoke-RestMethod https://api.ipify.org) + "/32"
```

Run Terraform:

```powershell
cd infra\terraform
terraform init
terraform fmt -check -recursive
terraform validate
terraform plan -var-file="environments/dev/dev.tfvars"
```

Apply only after reviewing the plan:

```powershell
terraform apply -var-file="environments/dev/dev.tfvars"
```

## Container Image

The Container App expects the image tag defined by `container_image_name` to exist in the created ACR. A later deployment workflow should:

1. Build `deploy/docker/Dockerfile`.
2. Push the image to `container_registry_login_server`.
3. Update the Container App revision.
4. Run EF Core migrations against the PostgreSQL server.

## Secret Handling

Do not commit `*.tfvars`, `*.tfstate`, `.terraform/`, or plan files. Terraform state can contain sensitive values because the PostgreSQL password and HMAC key are used to create Key Vault secrets. Store state in a protected remote backend before applying outside a local demo.

## Cost Notes

Approximate low-cost dev choices:

- Container Apps: scales to zero with `container_min_replicas = 0`.
- PostgreSQL Flexible Server: `B_Standard_B1ms`, 32 GB storage.
- ACR: Basic SKU.
- Log Analytics: pay-per-GB ingestion, 30-day retention.
- Key Vault: standard SKU, low transaction volume.

For production, increase PostgreSQL backup posture, replicas, monitoring, and network restrictions, and move Terraform state to a remote backend.
