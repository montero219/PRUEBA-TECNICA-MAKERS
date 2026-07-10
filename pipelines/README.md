# CI/CD

Atlas PARS usa GitHub Actions como pipeline declarativo principal. La prueba tecnica permite GitHub Actions o Azure Pipelines; se elige Actions porque el repositorio ya vive en GitHub y permite dejar visibles los gates de calidad sin provisionar herramientas externas.

## Workflows

- `.github/workflows/ci.yml`: restore, build Release, pruebas unitarias con cobertura y prueba de integracion end-to-end contra PostgreSQL 16 como service container.
- `.github/workflows/security.yml`: CodeQL para SAST, Trivy para SCA/secrets/misconfig en el repositorio, build y escaneo Trivy de la imagen Docker, y Checkov para Terraform.
- `.github/dependabot.yml`: actualizaciones semanales para GitHub Actions, NuGet y la imagen base Docker.

## Gates requeridos para `main`

Configurar branch protection o ruleset de GitHub sobre `main` con estas reglas:

1. Requerir pull request antes de merge.
2. Requerir al menos una aprobacion humana.
3. Descartar aprobaciones cuando haya nuevos commits.
4. Requerir historial lineal.
5. Bloquear force-push y borrado de rama.
6. Requerir que pasen estos checks:
   - `Build, test and coverage`
   - `CodeQL SAST`
   - `Trivy SCA, secrets and source scan`
   - `Docker image scan`
   - `Checkov IaC scan`
7. Requerir conversaciones resueltas antes de merge.

La politica queda documentada porque no hay todavia Terraform de GitHub Rulesets en `infra/`. Cuando se agregue IaC de administracion del repositorio, esta seccion debe migrarse a rulesets declarativos.

## Versionado Semantico

El versionado semantico automatizado queda como siguiente paso. Para mantener el alcance defendible, esta entrega no activa todavia un workflow de release.

Cuando se agregue, los commits deberian seguir Conventional Commits:

- `feat:` incrementa version minor.
- `fix:` incrementa version patch.
- `feat!:` o `BREAKING CHANGE:` incrementa version major.

Una opcion razonable seria Release Please para crear un release PR con `CHANGELOG.md`, tag SemVer y GitHub Release al fusionarse.

## CD

El pipeline no despliega a nube en esta etapa porque la IaC existe como referencia, pero no se ha aplicado contra una suscripcion real ni existe todavia un workflow de despliegue con ambientes. Para no fingir un despliegue, el CD queda limitado a:

- imagen Docker reproducible desde `deploy/docker/Dockerfile`;
- escaneo de la imagen antes de cualquier publicacion;
- release SemVer trazable como siguiente paso.

Ahora existe IaC de referencia en `infra/terraform`. El siguiente incremento debe agregar un workflow separado con ambientes `dev`, `qa` y `prod`, aprobaciones por environment, OIDC federado y secretos referenciados desde Key Vault, nunca hardcodeados.
