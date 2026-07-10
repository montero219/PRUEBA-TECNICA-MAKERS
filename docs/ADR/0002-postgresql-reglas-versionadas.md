# ADR 0002 - PostgreSQL Para Catalogo Y Reglas Versionadas

## Estado

Aceptada

## Contexto

Atlas PARS necesita persistir:

- Tenants u organizaciones.
- Recursos protegidos.
- Operaciones.
- Reglas de autorizacion.
- Versiones de reglas con vigencia temporal.

Tambien necesita evitar ambiguedad entre versiones vigentes de una misma regla.

## Decision

Usar PostgreSQL con Entity Framework Core y Npgsql. Las condiciones de las reglas se almacenan como `jsonb` en `versiones_regla`.

El modelo actual sigue esta cadena:

```text
organizaciones -> recursos_protegidos -> operaciones -> reglas_autorizacion -> versiones_regla
```

Cada version tiene:

- `numero_version`
- `condiciones`
- `decision_si_cumple`
- `prioridad`
- `vigencia_desde`
- `vigencia_hasta`

La migracion inicial agrega una restriccion de no solapamiento temporal por regla usando `btree_gist` y `EXCLUDE USING gist`.

## Alternativas Consideradas

### Base documental

Ventajas:

- Natural para documentos JSON.
- Flexible para cambios de esquema de politica.

Desventajas:

- Menor claridad relacional para tenant, recurso, operacion y version.
- Mas cuidado para imponer integridad y no solapamiento temporal.

### Archivos JSON/YAML en repositorio

Ventajas:

- Simple para empezar.
- Compatible con GitOps.

Desventajas:

- Menos adecuado para multi-tenant dinamico.
- Requiere mecanismo adicional para consultar vigencia, version y auditoria.

## Consecuencias Positivas

- Integridad relacional clara.
- Indices por tenant/recurso/operacion.
- Reglas declarativas persistidas como `jsonb`.
- Restriccion de no solapamiento para versiones de regla.
- Buen camino hacia auditoria append-only.

## Consecuencias Negativas

- Requiere operar PostgreSQL.
- El uso de `jsonb` exige validaciones adicionales.
- Las pruebas con InMemory no prueban constraints propias de PostgreSQL.

## Evolucion

Agregar pruebas de integracion contra PostgreSQL real o Testcontainers para validar constraints, migraciones y queries tal como correrian fuera del entorno unitario.
