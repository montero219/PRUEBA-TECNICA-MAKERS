# Casos De Prueba ABAC MVP - Finora

## Objetivo

Este documento conecta los casos de negocio del MVP con las migraciones, reglas sembradas y pruebas automatizadas. La meta es que durante la sustentacion se pueda explicar que cada decision (`PERMIT`, `DENY`, `CHALLENGE`) sale de reglas versionadas y no de condicionales hardcodeados.

## Relacion Con Las Migraciones

Las migraciones no almacenan usuarios, actores concretos ni transferencias individuales. El actor, el monto y el contexto llegan en cada request a `POST /authorize`.

Las migraciones siembran el catalogo minimo que permite evaluar esos requests:

- `20260709120000_SembrarCatalogoAbacMvpFinora`: crea tenants `FINORA` y `ORG_EXTERNA`, recurso `TRANSFERENCIA`, operacion `APROBAR` y cinco reglas por tenant.
- `20260709123000_SembrarVersionesReglasAbacMvpFinora`: crea la version `1.0.0` vigente de cada regla, con condiciones JSON, prioridad y decision esperada.
- `20260709170000_AgregarAuditoriaDecisionesAutorizacion`: agrega auditoria append-only para registrar cada decision.
- `20260709180000_AgregarFirmaDecisionesAutorizacion`: agrega `key_id_firma`, `algoritmo_firma` y `firma` para trazabilidad criptografica.

## Reglas Sembradas

| Prioridad | Regla | Decision | Condicion principal |
|---:|---|---|---|
| 10 | `VALIDAR_AISLAMIENTO_TENANT` | `DENY` | `actor.organizacion` distinto de `recurso.organizacion`. |
| 20 | `BLOQUEAR_RIESGO_CRITICO` | `DENY` | `contexto.nivelRiesgo` en `["CRITICO"]`. |
| 30 | `CONTEXTO_SOSPECHOSO` | `CHALLENGE` | Monto >= 8000000, hora entre 00:00 y 05:00, dispositivo no confiable y riesgo bajo/medio. |
| 40 | `MONTO_SENSIBLE` | `CHALLENGE` | `recurso.monto` >= 1000001. |
| 100 | `PERMITIR_TRANSFERENCIA_NORMAL` | `PERMIT` | Cliente de la misma organizacion, monto <= 1000000, horario normal, dispositivo confiable y riesgo bajo. |

La prioridad menor gana. Por eso los bloqueos (`DENY`) se evaluan antes que los permisos normales.

## Casos MVP

| Caso | Nombre | Condicion que dispara la regla | Regla esperada | Decision |
|---|---|---|---|---|
| A | Transferencia normal | Actor `CLIENTE`, misma organizacion, monto `500000`, hora `10:00`, dispositivo confiable, riesgo `BAJO`. | `PERMITIR_TRANSFERENCIA_NORMAL` | `PERMIT` |
| B | Monto sensible | Misma organizacion, monto mayor a `1000000`, riesgo `BAJO`. | `MONTO_SENSIBLE` | `CHALLENGE` |
| C | Contexto sospechoso | Monto `8000000`, hora `02:30`, dispositivo no confiable, riesgo `MEDIO`. | `CONTEXTO_SOSPECHOSO` | `CHALLENGE` |
| D | Riesgo critico | Misma organizacion, monto normal, riesgo `CRITICO`. | `BLOQUEAR_RIESGO_CRITICO` | `DENY` |
| E | Aislamiento multi-tenant | Request enviado con `X-Tenant-Code: FINORA`, pero con `atributosRecurso.organizacion = "ORG_EXTERNA"`. | `VALIDAR_AISLAMIENTO_TENANT` | `DENY` |

## Trazabilidad Automatizada

| Caso | Cobertura automatizada actual | Archivo |
|---|---|---|
| A - `PERMIT` normal | Cubierto en unitarias de servicio y en integracion end-to-end contra PostgreSQL real. | `ServicioAutorizacionPruebas`, `AutorizacionAuditoriaIntegracionPruebas` |
| B - `CHALLENGE` por monto sensible | Cubierto en unitarias de servicio y validado contra las migraciones por prueba de semillas. | `ServicioAutorizacionPruebas`, `SemillasAbacMvpFinoraPruebas` |
| C - `CHALLENGE` por contexto sospechoso | Cubierto a nivel de evaluador por operadores `mayor_o_igual`, `entre_horas`, booleanos y `en`; no tiene prueba end-to-end propia. | `EvaluadorCondicionesPruebas`, `SemillasAbacMvpFinoraPruebas` |
| D - `DENY` por riesgo critico | Cubierto a nivel de evaluador y repositorio de reglas vigentes; no tiene prueba end-to-end propia. | `EvaluadorCondicionesPruebas`, `RepositorioReglasAutorizacionPruebas` |
| E - `DENY` por aislamiento tenant | Cubierto en unitarias de servicio. | `ServicioAutorizacionPruebas` |

## Request Base

Todos los casos usan:

```http
POST /authorize
X-Tenant-Code: FINORA
X-Correlation-Id: caso-a-transferencia-normal
Content-Type: application/json
```

`X-Correlation-Id` es opcional para la API, pero en estos casos se envia explicitamente para demostrar trazabilidad. El valor se puede cambiar por caso (`caso-b-monto-sensible`, `caso-c-contexto-sospechoso`, etc.) y debe regresar en la respuesta como `correlationId` y persistirse en `decisiones_autorizacion.correlation_id`.

Body base para el caso A:

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

Para probar los otros casos manualmente se cambian solo los atributos relevantes:

| Caso | Cambios sobre el body base |
|---|---|
| B | `atributosRecurso.monto = "12000000"` |
| C | `atributosRecurso.monto = "8000000"`, `contexto.hora = "02:30"`, `contexto.dispositivoConfiable = "false"`, `contexto.nivelRiesgo = "MEDIO"` |
| D | `contexto.nivelRiesgo = "CRITICO"` |
| E | `atributosRecurso.organizacion = "ORG_EXTERNA"` |

## Interfaz Visual

No se incluyo Swagger UI/OpenAPI visual en el alcance actual. La API se valida mediante pruebas automatizadas, ejemplos HTTP en `README.md` y el runbook operativo. Agregar Swagger seria un incremento pequeno y razonable si se quiere mejorar la exploracion manual del endpoint sin cambiar la logica de negocio.

## Resumen Honesto

Los casos A, B y E tienen cobertura directa en pruebas de servicio; el caso A ademas esta cubierto end-to-end con migraciones reales, auditoria y firma. Los casos C y D estan alineados con reglas sembradas por migracion y cubiertos por pruebas del evaluador/repositorio, pero no tienen prueba end-to-end propia en esta entrega.
