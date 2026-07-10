# Atlas PARS

Atlas PARS es un PoC industrializable de un servicio de Politica de Acceso a Recursos Sensibles. Su objetivo es centralizar decisiones de autorizacion para operaciones criticas usando un flujo ABAC: actor, recurso, accion y contexto.

## Estado Actual

Implementado:

- API .NET 8 con endpoint `POST /authorize`.
- Resolucion multi-tenant mediante header `X-Tenant-Code`.
- Modelo relacional para organizaciones, recursos protegidos, operaciones, reglas de autorizacion y versiones de regla.
- Motor ABAC declarativo basado en JSON.
- Evaluacion de reglas vigentes por fecha y prioridad.
- Decisiones `PERMIT`, `DENY` y `CHALLENGE`.
- Seeds para el caso Finora: transferencia normal, monto sensible, contexto sospechoso, riesgo critico y aislamiento de tenant.
- Pruebas unitarias de evaluador, servicio y repositorios.
- Auditoria persistente de decisiones en `decisiones_autorizacion`.
- `idDecision`, `correlationId` y `solicitudHash` en la respuesta.
- Firma HMAC-SHA256 de cada decision (`keyId` y `firma` en la respuesta y en la auditoria).
- Prueba de integracion end-to-end para `POST /authorize`, auditoria y firma.
- Docker Compose local para PostgreSQL.
- IaC de referencia en Terraform para Azure Container Apps, PostgreSQL, Key Vault, ACR y Log Analytics.

No implementado aun:

- Despliegue cloud aplicado en una suscripcion real.
- Pipeline de despliegue cloud completo con OIDC, build/push de imagen y migraciones.
- Observabilidad productiva con OpenTelemetry, metricas y health checks no triviales.
- Video pitch.

## Arquitectura Rapida

El flujo actual es:

1. El consumidor llama `POST /authorize` con `X-Tenant-Code` y opcionalmente `X-Correlation-Id`.
2. La API busca la organizacion por codigo.
3. La API resuelve recurso y operacion dentro del tenant.
4. El repositorio obtiene las versiones de reglas vigentes para esa operacion.
5. El evaluador compara condiciones JSON contra atributos de actor, recurso y contexto.
6. El servicio calcula `solicitudHash`.
7. El servicio retorna la primera decision aplicable segun prioridad.
8. El servicio firma el resultado de la decision con HMAC-SHA256.
9. El servicio persiste la decision en auditoria append-only, incluida la firma.
10. Si no hay regla aplicable, responde `DENY` por defecto.

Mas detalle en [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Endpoint

`POST /authorize`

Headers:

```http
X-Tenant-Code: FINORA
X-Correlation-Id: demo-local-001
Content-Type: application/json
```

Body de ejemplo:

```json
{
  "codigoRecurso": "TRANSFERENCIA",
  "codigoOperacion": "APROBAR",
  "atributosActor": {
    "rol": "CLIENTE",
    "organizacion": "FINORA"
  },
  "atributosRecurso": {
    "monto": "500000",
    "organizacion": "FINORA"
  },
  "contexto": {
    "hora": "10:00",
    "dispositivoConfiable": "true",
    "nivelRiesgo": "BAJO"
  }
}
```

Respuesta esperada:

```json
{
  "idDecision": "00000000-0000-0000-0000-000000000000",
  "decision": "PERMIT",
  "motivo": "Aplico la regla 'PERMITIR_TRANSFERENCIA_NORMAL'.",
  "codigoRegla": "PERMITIR_TRANSFERENCIA_NORMAL",
  "correlationId": "demo-local-001",
  "solicitudHash": "hex-sha256",
  "keyId": "atlas-pars-hmac-2026-07",
  "firma": "base64-hmac-sha256"
}
```

`keyId` y `firma` requieren configurar `FirmaDecisiones:KeyId` y `FirmaDecisiones:ClaveActivaBase64` (user-secrets en local, variable de entorno en contenedor) — la API falla al arrancar si faltan. Ver [docs/RUNBOOK.md](docs/RUNBOOK.md).

## Reglas ABAC

Las reglas viven versionadas en PostgreSQL como JSONB. El formato actual usa una clausula `todas`, equivalente a un AND logico:

```json
{
  "todas": [
    {
      "fuente": "actor",
      "atributo": "organizacion",
      "operador": "igual",
      "compararCon": {
        "fuente": "recurso",
        "atributo": "organizacion"
      }
    }
  ]
}
```

Fuentes soportadas:

- `actor`
- `recurso`
- `contexto`

Operadores soportados:

- `igual`
- `distinto`
- `mayor_o_igual`
- `menor_o_igual`
- `en`
- `entre_horas`

## Ejecutar Localmente

Requisitos:

- .NET 8 SDK
- Docker Desktop o Docker Engine

Levantar PostgreSQL:

```powershell
Copy-Item deploy\compose\.env.example deploy\compose\.env
docker compose --env-file deploy\compose\.env -f deploy\compose\compose.yaml up -d
```

Configurar la cadena de conexion como secreto local:

```powershell
dotnet user-secrets set "ConnectionStrings:Atlas" "Host=localhost;Port=5433;Database=atlas_pars;Username=atlas_pars;Password=<password-local>" --project src\Atlas.PARS.Api\Atlas.PARS.Api.csproj
```

Configurar la clave de firma de decisiones (la API falla al arrancar si falta):

```powershell
dotnet user-secrets set "FirmaDecisiones:KeyId" "atlas-pars-hmac-2026-07" --project src\Atlas.PARS.Api\Atlas.PARS.Api.csproj
dotnet user-secrets set "FirmaDecisiones:ClaveActivaBase64" "<base64-32-bytes>" --project src\Atlas.PARS.Api\Atlas.PARS.Api.csproj
```

Aplicar migraciones:

```powershell
dotnet ef database update --project src\Atlas.PARS.Api\Atlas.PARS.Api.csproj
```

Ejecutar la API:

```powershell
dotnet run --project src\Atlas.PARS.Api\Atlas.PARS.Api.csproj
```

## Pruebas

Ejecutar todas las pruebas:

```powershell
dotnet test Atlas.PARS.sln
```

Ejecutar unitarias con cobertura:

```powershell
dotnet test tests\Atlas.PARS.PruebasUnitarias\Atlas.PARS.PruebasUnitarias.csproj --collect:"XPlat Code Coverage"
```

Estado verificado el 2026-07-10:

- 34 pruebas unitarias pasan (incluye casos `CHALLENGE` por monto sensible y `DENY` por aislamiento de tenant end-to-end).
- 1 prueba de integracion end-to-end pasa (incluye persistencia de auditoria y firma).
- Cobertura de la logica de evaluacion (el requisito pide >= 70% aqui, no en el resto):
  - `EvaluadorCondiciones` (motor de condiciones ABAC): 84.8%.
  - `ServicioAutorizacion` (orquestacion de la decision): 100% en el metodo principal.
  - `FirmadorDecisionesAutorizacionHmac` (firma criptografica): 80.8%.
  - `CalculadorHashSolicitud`: 100%.
- Cobertura global del proyecto: 35.3%. Deliberadamente no se persigue mas alla de la logica de evaluacion: repositorios, migraciones, configuraciones EF y el controlador son capas delgadas (mapeo directo a EF Core / ASP.NET) que ya se ejercitan end-to-end en la prueba de integracion contra Postgres real, no en unitarias con mocks.

## Documentacion

- [Guia de entrega](ENTREGA.md)
- [Arquitectura](docs/ARCHITECTURE.md)
- [Contexto de negocio](docs/BUSINESS-CONTEXT.md)
- [Casos de prueba ABAC](docs/casos-prueba-abac-mvp.md)
- [Modelo de amenazas](docs/THREAT-MODEL.md)
- [Runbook](docs/RUNBOOK.md)
- [ADR](docs/ADR)
- [Terraform Azure](infra/terraform/README.md)
- [AI Journal](AI-JOURNAL.md)

## Backlog Priorizado

1. Migrar a multiples claves de firma verificables para permitir rotacion sin perder auditoria retroactiva.
2. Implementar health checks, logs estructurados y metricas OpenTelemetry.
3. Aplicar IaC en una suscripcion real y validar costos/outputs.
4. Crear pipeline de despliegue con build/push de imagen, migraciones y aprobaciones por ambiente.
5. Grabar pitch de 5 minutos para producto y arquitectura.

## Decisiones de Alcance

Este PoC prioriza el nucleo ABAC sobre features perifericas. No incluye frontend porque la prueba indica que no se evalua. No incluye mensajeria porque el flujo de autorizacion es sincrono y de baja latencia. No usa OPA/Rego en esta primera version para mantener el motor explicable durante la sustentacion, aunque esa opcion queda abierta para evolucion.
