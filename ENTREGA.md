# Entrega Prueba Tecnica - Atlas PARS

## Resumen Ejecutivo

Atlas PARS es un PoC industrializable para centralizar decisiones de autorizacion de operaciones sensibles mediante reglas ABAC versionadas. La solucion prioriza el nucleo tecnico evaluable: API .NET 8, PostgreSQL, motor declarativo, auditoria persistente, firma criptografica de decisiones, pruebas automatizadas, CI/security gates e IaC de referencia para Azure.

## Enlace Principal

- Repositorio: `https://github.com/montero219/PRUEBA-TECNICA-MAKERS`
- Rama de entrega: `main`
- Punto de entrada: `README.md`

## Que Revisar Primero

1. `README.md`: alcance, ejecucion local, endpoint y estado honesto.
2. `docs/ARCHITECTURE.md`: arquitectura, flujo de autorizacion, auditoria y decisiones de diseno.
3. `docs/casos-prueba-abac-mvp.md`: casos A-E alineados con migraciones, reglas y pruebas.
4. `docs/THREAT-MODEL.md`: amenazas STRIDE y controles implementados.
5. `docs/RUNBOOK.md`: guia operativa para levantar, probar y diagnosticar.
6. `infra/terraform/README.md`: IaC de referencia para Azure.
7. `AI-JOURNAL.md`: uso de IA, decisiones aceptadas/rechazadas y alcance pendiente.

## Validacion Automatizada

La rama `main` ejecuta dos workflows de GitHub Actions:

- `CI`: restore, build Release, pruebas unitarias con cobertura y prueba de integracion contra PostgreSQL 16.
- `Security`: CodeQL, Trivy filesystem, Trivy Docker image y Checkov para Terraform.

Validaciones locales realizadas antes de la entrega:

- Terraform `fmt`, `validate` y Checkov: sin fallos.
- Docker build de la API: exitoso.
- Trivy filesystem e imagen Docker: sin hallazgos HIGH/CRITICAL.
- Build .NET Release: exitoso.
- Pruebas unitarias: 34/34.
- Prueba de integracion: 1/1.

## Alcance Implementado

- Endpoint `POST /authorize`.
- Resolucion multi-tenant con `X-Tenant-Code`.
- Modelo relacional PostgreSQL para organizaciones, recursos, operaciones, reglas y versiones.
- Motor ABAC declarativo basado en JSON.
- Decisiones `PERMIT`, `DENY` y `CHALLENGE`.
- Falla cerrada por defecto.
- Auditoria append-only de decisiones.
- `correlationId`, `solicitudHash`, `keyId` y `firma` en respuesta y auditoria.
- Firma HMAC-SHA256 de decisiones.
- Docker Compose local.
- CI/CD de validacion y seguridad.
- Dependabot configurado para evitar upgrades incompatibles.
- Terraform de referencia para Azure Container Apps, PostgreSQL Flexible Server, Key Vault, ACR y Log Analytics.
- Matriz de casos ABAC A-E trazada contra migraciones, reglas y pruebas automatizadas.

## Alcance Pendiente

- Video pitch de 5 minutos.
- Despliegue cloud aplicado en una suscripcion real.
- Pipeline de despliegue con OIDC, build/push de imagen, migraciones y aprobaciones por ambiente.
- Observabilidad productiva con OpenTelemetry, metricas y health checks avanzados.
- Rotacion completa de multiples claves de firma verificables.

## Mensaje Sugerido De Envio

Buenos dias,

Comparto la entrega de la prueba tecnica para Especialista Tecnico:

- Repositorio: `https://github.com/montero219/PRUEBA-TECNICA-MAKERS`
- Rama: `main`
- Punto de entrada recomendado: `README.md`
- Guia de entrega: `ENTREGA.md`

La solucion implementa un PoC industrializable de autorizacion ABAC con .NET 8, PostgreSQL, auditoria persistente, firma HMAC-SHA256 de decisiones, pruebas automatizadas, CI/security gates e IaC de referencia en Terraform para Azure.

Tambien deje documentadas las decisiones tecnicas, alcance, amenazas, runbook operativo, ADRs y uso de IA. El pitch de 5 minutos queda como complemento de sustentacion.

Quedo atento a cualquier comentario.
