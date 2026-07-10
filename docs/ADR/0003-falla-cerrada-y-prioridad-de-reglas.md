# ADR 0003 - Falla Cerrada Y Prioridad De Reglas

## Estado

Aceptada

## Contexto

Un servicio de autorizacion debe ser conservador. Si el sistema no encuentra reglas, encuentra reglas invalidas o ninguna regla aplica, permitir por defecto seria un riesgo de seguridad.

Ademas, algunas reglas deben ganar siempre sobre permisos normales. Por ejemplo:

- Aislamiento entre tenants.
- Riesgo critico.
- Contextos sospechosos.
- Montos sensibles.

## Decision

Atlas PARS falla cerrado:

- Si no hay reglas vigentes, retorna `DENY`.
- Si una regla vigente esta mal configurada, retorna `DENY`.
- Si ninguna regla aplica, retorna `DENY`.

Las reglas se evaluan por prioridad ascendente. Numeros menores tienen mayor prioridad. En los seeds actuales:

| Prioridad | Regla | Decision |
|---|---|---|
| 10 | `VALIDAR_AISLAMIENTO_TENANT` | `DENY` |
| 20 | `BLOQUEAR_RIESGO_CRITICO` | `DENY` |
| 30 | `CONTEXTO_SOSPECHOSO` | `CHALLENGE` |
| 40 | `MONTO_SENSIBLE` | `CHALLENGE` |
| 100 | `PERMITIR_TRANSFERENCIA_NORMAL` | `PERMIT` |

## Alternativas Consideradas

### Permitir por defecto

Rechazada. Reduce friccion operativa, pero abre una falla grave si el catalogo o las reglas no estan configurados.

### Evaluar todas las reglas y combinar resultados

Ventajas:

- Permite explicaciones mas ricas.
- Puede mostrar todas las razones de una decision.

Desventajas:

- Mas complejidad para el MVP.
- Requiere definir precedencia entre resultados multiples.
- Aumenta costo de evaluacion.

## Consecuencias Positivas

- Postura de seguridad clara.
- Facil de defender en sustentacion.
- Menor probabilidad de permisos accidentales.
- Permite que reglas restrictivas ganen sobre permisos normales.

## Consecuencias Negativas

- Puede generar `DENY` por errores de configuracion.
- Requiere buen runbook para diagnosticar rechazos.
- La auditoria actual registra la decision final, regla/version aplicada cuando existe y motivo.
- A futuro conviene guardar explicaciones mas completas con todas las reglas evaluadas.

## Evolucion

La auditoria implementada registra:

- Decision final.
- Primera regla aplicable, cuando existe.
- Version de regla, cuando existe.
- Motivo de rechazo si ninguna regla aplica o si falta configuracion de tenant/recurso/operacion.

Una evolucion posterior deberia registrar:

- Reglas evaluadas.
- Resultado parcial por regla.
- Tiempo de evaluacion por decision.
