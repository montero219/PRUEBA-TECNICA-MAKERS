# ADR 0004 - Trazabilidad Y Firma De Decisiones

## Estado

Aceptada

## Contexto

La prueba exige que cada decision sea auditable y tenga trazabilidad criptografica. Antes de este incremento la respuesta solo incluia:

- `decision`
- `motivo`
- `codigoRegla`

Esto explicaba la decision, pero no permitia reconstruir el evento completo ni correlacionarlo con otros sistemas.

## Decision

Implementar la trazabilidad en dos etapas:

1. Auditoria persistente de decisiones con `idDecision`, `correlationId` y `solicitudHash`.
2. Firma criptografica en un incremento posterior, cuando el formato de payload firmado y la estrategia de llaves esten entendidos y documentados.

La etapa 1 ya esta implementada. La respuesta actual incluye:

```json
{
  "idDecision": "uuid",
  "decision": "PERMIT",
  "motivo": "Aplico la regla 'PERMITIR_TRANSFERENCIA_NORMAL'.",
  "codigoRegla": "PERMITIR_TRANSFERENCIA_NORMAL",
  "correlationId": "corr-123",
  "solicitudHash": "hex-sha256"
}
```

La tabla `decisiones_autorizacion` guarda:

- `id`
- `codigo_organizacion`
- `id_organizacion`
- `id_recurso_protegido`
- `id_operacion`
- `id_regla_autorizacion`
- `id_version_regla`
- `codigo_recurso_solicitado`
- `codigo_operacion_solicitada`
- `decision`
- `motivo`
- `codigo_regla`
- `numero_version_regla`
- `actor`
- `recurso`
- `contexto`
- `algoritmo_hash`
- `solicitud_hash`
- `correlation_id`
- `fecha_decision`

La tabla es append-only: PostgreSQL bloquea `UPDATE` y `DELETE` mediante trigger. Si una decision ocurre antes de resolver todo el catalogo, por ejemplo tenant inexistente, recurso inexistente u operacion inexistente, las FKs no resueltas quedan `NULL`, pero los codigos solicitados y el resultado quedan auditados.

La etapa 2 (firma criptografica) ya esta implementada con HMAC-SHA256.

Se firma el resultado de la decision, no la solicitud cruda (eso ya lo cubre `solicitud_hash`). El payload canonico firmado es:

```json
{
  "idDecision": "uuid",
  "codigoOrganizacion": "FINORA",
  "decision": "PERMIT",
  "codigoRegla": "PERMITIR_TRANSFERENCIA_NORMAL",
  "solicitudHash": "hex-sha256",
  "correlationId": "corr-123",
  "fechaDecision": "2026-07-09T10:00:00+00:00"
}
```

La respuesta de `POST /authorize` ahora incluye `keyId` y `firma` (Base64 de HMAC-SHA256, para diferenciarla visualmente del `solicitudHash` en hex). La tabla `decisiones_autorizacion` persiste `key_id_firma`, `algoritmo_firma` y `firma`, con un check constraint que exige que las tres columnas esten todas presentes o todas ausentes (`ck_decisiones_autorizacion_firma_consistente`). Las tres son nullable porque las decisiones auditadas antes de este incremento no tienen firma retroactiva — no se fabrica una firma para filas historicas, eso seria deshonesto.

La clave activa se inyecta por configuracion (`FirmaDecisiones:KeyId` y `FirmaDecisiones:ClaveActivaBase64`), nunca hardcodeada: user-secrets en desarrollo local, variable de entorno en el contenedor. El firmador falla cerrado al intentar firmar una decision si la clave no esta configurada o no es Base64 valido, siguiendo el mismo principio de fallo cerrado del ADR 0003. En produccion, esa configuracion se reemplazaria por una referencia a Azure Key Vault (o el KMS equivalente) sin cambiar la interfaz `IFirmadorDecisionesAutorizacion`.

La verificacion (`Verificar`) recalcula la firma y compara en tiempo constante (`CryptographicOperations.FixedTimeEquals`) para evitar timing attacks.

## Alternativas Consideradas

### JWS

Ventajas:

- Estandar conocido.
- Payload y firma transportables.
- Mejor interoperabilidad.

Desventajas:

- Mas trabajo inicial.
- Requiere elegir libreria y detalles de canonicalizacion.

### HMAC directo

Ventajas:

- Simple para PoC.
- Rapido de probar.
- Suficiente si se documenta formato canonico.

Desventajas:

- Menos interoperable que JWS.
- Requiere disciplina para rotacion y `keyId`.

### COSE/PASETO

Ventajas:

- Buenas propiedades criptograficas si se usan librerias maduras.

Desventajas:

- Menos familiar para el equipo promedio.
- Mayor costo de explicacion en sustentacion.

## Consecuencias Positivas

- Cumple la parte auditable de trazabilidad.
- Permite investigar decisiones historicas.
- Permite correlacionar llamadas con logs de consumidores usando `correlationId`.
- Permite detectar cambios en la solicitud evaluada mediante `solicitud_hash`.
- Habilita "time-travel audit" en una evolucion futura.
- Se conecta bien con Key Vault y rotacion de llaves.

## Consecuencias Negativas

- Aumenta complejidad del modelo.
- Exige canonicalizar payload antes de firmar.
- Persistir atributos en `jsonb` exige cuidar datos sensibles y retencion.
- **Limitacion aceptada del PoC**: se eligio una unica clave activa (sin array de claves para rotacion) por simplicidad. Esto significa que rotar la clave invalida la verificacion de decisiones historicas firmadas con la clave anterior — no hay forma de re-verificar una firma vieja despues de rotar. Evolucion futura: mantener un mapa `keyId -> clave` donde las claves antiguas quedan disponibles solo para verificar (nunca para firmar), permitiendo rotacion sin perder capacidad de auditoria retroactiva.

## Criterios De Aceptacion

- Cada llamada a `POST /authorize` genera un registro de auditoria.
- Cada respuesta incluye `idDecision`, `correlationId` y `solicitudHash`.
- La prueba de integracion verifica que una llamada a `POST /authorize` inserta auditoria.
- La tabla de auditoria bloquea `UPDATE` y `DELETE`.
- El runbook explica diagnostico de una decision.
- Cada respuesta incluye `keyId` y `firma`.
- La firma se puede verificar en una prueba automatizada (`FirmadorDecisionesAutorizacionHmacPruebas` y la prueba de integracion end-to-end).
- La llave no esta hardcodeada en el repositorio (configuracion vacia en `appsettings.json`, inyectada por user-secrets o variable de entorno).
