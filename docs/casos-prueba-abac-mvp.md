# Casos de Prueba ABAC MVP - Finora

## Objetivo

Este documento define los casos mínimos para demostrar ABAC en el MVP de Finora.

La intención es validar decisiones `PERMIT`, `DENY` y `CHALLENGE` usando atributos distribuidos entre actor, recurso, operación y contexto, no únicamente roles.

También sirve como guía para crear los inserts de datos de prueba sin perder la conexión lógica entre las tablas.

## Alcance del MVP

Para el MVP se prueban únicamente casos de transferencias financieras.

Quedan fuera de esta primera versión:

- Modificación de datos personales
- Accesos administrativos
- Casos avanzados de administración de usuarios

## Atributos ABAC usados

| Categoría | Atributos |
|---|---|
| Actor | rol, organización |
| Recurso | monto, organización |
| Operación | `APROBAR` |
| Contexto | hora, dispositivo confiable, nivel de riesgo |

## Identificadores lógicos propuestos

Estos identificadores no tienen que ser los IDs reales de base de datos. Sirven como nombres estables para conectar los inserts entre tablas y explicar cada caso.

| Tipo | ID lógico | Descripción |
|---|---|---|
| Organización | `org_finora` | Tenant principal: Finora |
| Organización | `org_externa` | Otro tenant usado para validar aislamiento |
| Actor | `actor_cliente_finora` | Cliente titular perteneciente a Finora |
| Operación | `op_aprobar_transferencia` | Operación para aprobar una transferencia |
| Recurso | `tx_normal_finora` | Transferencia normal de Finora |
| Recurso | `tx_monto_sensible_finora` | Transferencia de monto sensible de Finora |
| Recurso | `tx_madrugada_finora` | Transferencia en contexto riesgoso de Finora |
| Recurso | `tx_riesgo_critico_finora` | Transferencia con riesgo crítico |
| Recurso | `tx_otro_tenant` | Transferencia perteneciente a otra organización |

## Casos MVP

| Caso | Nombre | Resultado esperado | Qué demuestra |
|---|---|---|---|
| A | Transferencia normal | `PERMIT` | Camino feliz con actor válido, recurso propio y contexto confiable |
| B | Transferencia de monto sensible | `CHALLENGE` | El monto del recurso puede exigir validación adicional |
| C | Monto sensible, madrugada y dispositivo desconocido | `CHALLENGE` | La combinación de atributos contextuales eleva el control |
| D | Riesgo crítico | `DENY` | El contexto puede bloquear aunque el actor sea válido |
| E | Operación sobre otro tenant | `DENY` | Aislamiento multi-tenant |

## Detalle de casos

### Caso A - Transferencia normal

| Categoría | Valor |
|---|---|
| Actor | `actor_cliente_finora` |
| Rol actor | `CLIENTE` |
| Organización actor | `org_finora` |
| Recurso | `tx_normal_finora` |
| Monto | `500000` COP |
| Organización recurso | `org_finora` |
| Operación | `op_aprobar_transferencia` |
| Hora | Horario normal |
| Dispositivo confiable | `true` |
| Nivel de riesgo | `BAJO` |
| Resultado esperado | `PERMIT` |

Explicación: el cliente pertenece a la misma organización del recurso, la transferencia es de bajo monto y el contexto es confiable.

### Caso B - Transferencia de monto sensible

| Categoría | Valor |
|---|---|
| Actor | `actor_cliente_finora` |
| Rol actor | `CLIENTE` |
| Organización actor | `org_finora` |
| Recurso | `tx_monto_sensible_finora` |
| Monto | `12000000` COP |
| Organización recurso | `org_finora` |
| Operación | `op_aprobar_transferencia` |
| Hora | Horario normal |
| Dispositivo confiable | `true` |
| Nivel de riesgo | `BAJO` |
| Resultado esperado | `CHALLENGE` |

Explicación: el actor y el tenant son válidos, pero el monto supera el umbral normal de `1000000` COP y requiere validación adicional.

### Caso C - Monto sensible, madrugada y dispositivo desconocido

| Categoría | Valor |
|---|---|
| Actor | `actor_cliente_finora` |
| Rol actor | `CLIENTE` |
| Organización actor | `org_finora` |
| Recurso | `tx_madrugada_finora` |
| Monto | `8000000` COP |
| Organización recurso | `org_finora` |
| Operación | `op_aprobar_transferencia` |
| Hora | `02:30` |
| Dispositivo confiable | `false` |
| Nivel de riesgo | `MEDIO` |
| Resultado esperado | `CHALLENGE` |

Explicación: aunque el actor pertenece al tenant correcto, la combinación de monto relevante, hora inusual y dispositivo desconocido exige validación adicional.

### Caso D - Riesgo crítico

| Categoría | Valor |
|---|---|
| Actor | `actor_cliente_finora` |
| Rol actor | `CLIENTE` |
| Organización actor | `org_finora` |
| Recurso | `tx_riesgo_critico_finora` |
| Monto | `500000` COP |
| Organización recurso | `org_finora` |
| Operación | `op_aprobar_transferencia` |
| Hora | Horario normal |
| Dispositivo confiable | `true` |
| Nivel de riesgo | `CRITICO` |
| Motivo del riesgo | Sesión marcada por señales antifraude críticas |
| Resultado esperado | `DENY` |

Explicación: el riesgo crítico viene del contexto de seguridad de la sesión. Por ejemplo, señales antifraude críticas, credenciales comprometidas, múltiples intentos fallidos recientes o una alerta activa sobre la cuenta. Ese contexto bloquea la operación incluso si el actor, el recurso y el tenant son válidos.

### Caso E - Operación sobre otro tenant

| Categoría | Valor |
|---|---|
| Actor | `actor_cliente_finora` |
| Rol actor | `CLIENTE` |
| Organización actor | `org_finora` |
| Recurso | `tx_otro_tenant` |
| Monto | `500000` COP |
| Organización recurso | `org_externa` |
| Operación | `op_aprobar_transferencia` |
| Hora | Horario normal |
| Dispositivo confiable | `true` |
| Nivel de riesgo | `BAJO` |
| Resultado esperado | `DENY` |

Explicación: el actor pertenece a Finora, pero intenta operar sobre un recurso de otra organización. La regla de aislamiento multi-tenant debe bloquear la solicitud.

## Matriz de trazabilidad

| Caso | Actor | Recurso | Operación | Contexto | Decisión | Prueba automatizada |
|---|---|---|---|---|---|---|
| A | `actor_cliente_finora` | `tx_normal_finora` | `op_aprobar_transferencia` | Confiable, riesgo bajo, horario normal | `PERMIT` | `ServicioAutorizacionPruebas.AutorizarAsync_CuandoUnaReglaVigenteAplica_RetornaDecisionDeLaRegla` |
| B | `actor_cliente_finora` | `tx_alto_monto_finora` | `op_aprobar_transferencia` | Confiable, riesgo bajo, horario normal | `CHALLENGE` | `ServicioAutorizacionPruebas.AutorizarAsync_CuandoUnaReglaExigeChallengePorMontoSensible_RetornaChallengeAuditado` |
| C | `actor_cliente_finora` | `tx_madrugada_finora` | `op_aprobar_transferencia` | Dispositivo desconocido, `02:30`, riesgo medio | `CHALLENGE` | Pendiente (cubierto solo por `EvaluadorCondicionesPruebas` a nivel de operador, no end-to-end) |
| D | `actor_cliente_finora` | `tx_riesgo_critico_finora` | `op_aprobar_transferencia` | Riesgo crítico | `DENY` | Pendiente (cubierto solo por `EvaluadorCondicionesPruebas` a nivel de operador, no end-to-end) |
| E | `actor_cliente_finora` | `tx_otro_tenant` | `op_aprobar_transferencia` | Confiable, riesgo bajo, horario normal | `DENY` | `ServicioAutorizacionPruebas.AutorizarAsync_CuandoActorYRecursoPertenecenAOrganizacionesDistintas_RetornaDenyPorAislamiento` |

## Guía para inserts

Al crear la migración o seed, se recomienda mantener nombres o códigos únicos equivalentes a los IDs lógicos de este documento.

La distribución esperada de datos puede seguir esta relación:

| Datos | Propósito |
|---|---|
| Organizaciones | Crear `org_finora` y `org_externa` |
| Actores o usuarios | Crear `actor_cliente_finora` asociado a `org_finora` |
| Recursos o transferencias | Crear una transferencia por cada caso |
| Operaciones o permisos | Crear o reutilizar la operación `APROBAR` |
| Contextos o solicitudes de evaluación | Crear un contexto por cada caso |
| Resultados esperados | Asociar cada escenario con `PERMIT`, `CHALLENGE` o `DENY` |

La clave es que cada caso sea reconstruible desde los datos. Si las tablas están normalizadas, el caso debe poder leerse siguiendo esta cadena:

```text
caso -> actor -> organización actor
     -> recurso -> organización recurso
     -> operación
     -> contexto
     -> resultado esperado
```

## Regla conceptual esperada

La decisión no depende solo del rol del actor.

Debe evaluarse la combinación de:

1. Actor válido
2. Relación actor-recurso
3. Organización del actor contra organización del recurso
4. Monto de la transferencia
5. Operación solicitada
6. Contexto de la sesión

## Prioridad de decisión sugerida

En caso de conflicto entre reglas, se recomienda este orden lógico:

1. `DENY` por tenant diferente
2. `DENY` por riesgo crítico
3. `CHALLENGE` por combinación de contexto sospechoso
4. `CHALLENGE` por monto sensible
5. `PERMIT` si no aplica ninguna restricción
