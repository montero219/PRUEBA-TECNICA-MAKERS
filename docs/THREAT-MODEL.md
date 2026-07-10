# Modelo De Amenazas

## Alcance

Este modelo cubre el PoC actual de Atlas PARS: API .NET 8, endpoint `POST /authorize`, PostgreSQL, reglas ABAC versionadas y evaluador JSON. Las amenazas se analizan con STRIDE.

## Activos Protegidos

- Decisiones de autorizacion `PERMIT`, `DENY` y `CHALLENGE`.
- Reglas y versiones de reglas.
- Atributos de actor, recurso y contexto enviados por consumidores.
- Separacion entre tenants.
- Cadena de conexion y futuros secretos criptograficos.
- Evidencia de auditoria en `decisiones_autorizacion`.

## Suposiciones

- El consumidor autenticado es responsable de identificar al actor.
- Atlas PARS decide autorizacion, no autenticacion.
- Existe auditoria persistente y firma criptografica HMAC-SHA256 de cada decision.
- PostgreSQL se ejecuta localmente para el PoC.

## Amenazas STRIDE

| ID | STRIDE | Amenaza | Impacto | Mitigacion actual | Mitigacion pendiente |
|---|---|---|---|---|---|
| T1 | Spoofing | Un consumidor envia `X-Tenant-Code` de otro tenant. | Acceso cruzado o decisiones incorrectas. | Las consultas de recurso y operacion se acotan por organizacion. Hay regla de aislamiento actor/recurso. | Autenticacion de consumidor, mTLS/JWT, claims de tenant firmados y autorizacion de cliente por tenant. |
| T2 | Tampering | Un atacante altera atributos del body, por ejemplo `organizacion`, `monto` o `nivelRiesgo`. | Puede forzar `PERMIT` si el consumidor no valida datos. | El motor evalua todos los atributos recibidos, falla cerrado si no aplica regla, guarda `solicitud_hash` SHA-256 sobre payload canonico y firma el resultado de la decision con HMAC-SHA256 (`firma`, `key_id_firma`). | Validar atributos contra fuentes confiables y schema validation. |
| T3 | Repudiation | Un consumidor niega haber recibido o enviado una decision especifica. | Complica auditoria e investigacion de incidentes. | Cada decision controlada se persiste con `idDecision`, `correlationId`, `solicitud_hash`, regla/version, atributos evaluados y firma HMAC-SHA256 (`keyId` + timestamp + hash del request). La tabla es append-only. | Migrar a multiples claves verificables para no perder la capacidad de re-verificar decisiones historicas al rotar la clave activa. |
| T4 | Information Disclosure | Mensajes de error revelan codigos de recursos u operaciones. | Un atacante enumera catalogo interno. | Los mensajes actuales son claros para desarrollo. | Normalizar errores publicos, logs internos separados, correlation id y rate limiting. |
| T5 | Denial of Service | Solicitudes masivas o reglas complejas degradan latencia. | P95 > 150 ms o caida del servicio. | Evaluador en memoria simple y consultas acotadas por indices. | Rate limiting, cache de reglas vigentes, pruebas de carga, timeouts y circuit breakers. |
| T6 | Elevation of Privilege | Una regla mal priorizada permite antes de negar. | Bypass de controles fuertes. | Prioridades explicitas; DENY por tenant y riesgo critico tienen prioridad mas alta. | Validaciones de politica en CI, pruebas de regresion por matriz ABAC y revision de cambios de reglas. |
| T7 | Tampering | Dos versiones de regla quedan vigentes al mismo tiempo. | Decisiones ambiguas. | Restriccion `EXCLUDE USING gist` evita solapamientos temporales por regla. | Prueba de integridad contra PostgreSQL real en integracion. |
| T8 | Information Disclosure | Se commitea una cadena de conexion real o secreto local. | Exposicion de credenciales. | La documentacion exige user-secrets o variables de entorno. | Secret scanning en CI y rotacion de cualquier secreto expuesto. |

## OWASP API Top 10 Relevante

| Riesgo | Estado actual | Accion recomendada |
|---|---|---|
| API1 Broken Object Level Authorization | Parcialmente mitigado por scoping tenant/recurso/operacion. | Atar tenant a identidad autenticada, no solo header. |
| API2 Broken Authentication | Fuera del alcance actual. | Agregar JWT/mTLS entre squads. |
| API3 Broken Object Property Level Authorization | Riesgo por atributos confiados al body. | Validar atributos contra sistemas fuente. |
| API4 Unrestricted Resource Consumption | No implementado. | Rate limiting, limites de payload y pruebas de carga. |
| API8 Security Misconfiguration | Riesgo si secretos quedan en config. | Secret scanning y configuracion por entorno. |

## Decisiones De Seguridad Ya Tomadas

- Falla cerrada: sin reglas vigentes o aplicables, se retorna `DENY`.
- Reglas versionadas con ventana de vigencia.
- Prioridades para que reglas restrictivas se evalen antes de permisos normales.
- Check constraints en base de datos para formato de codigos, decisiones validas y rangos temporales.
- Separacion logica por tenant desde el catalogo.
- Auditoria persistente append-only para decisiones controladas.
- Hash SHA-256 canonico de la solicitud evaluada.
- Firma HMAC-SHA256 del resultado de la decision, con clave inyectada por configuracion (nunca hardcodeada) y verificacion en tiempo constante.
- `correlation_id` para rastrear una operacion entre consumidor, API y base.

## Riesgos Aceptados En El PoC

- No hay autenticacion del consumidor.
- Una unica clave de firma activa: rotarla invalida la verificacion de decisiones historicas (ver ADR 0004).
- No hay rate limiting ni proteccion de carga.
- El pipeline de seguridad es baseline: SAST, SCA, secrets e imagen; faltan DAST, validaciones de politicas ABAC y gates de despliegue.

Estos riesgos no se aceptarian en produccion. Se documentan porque el objetivo actual es demostrar el nucleo ABAC y priorizar el siguiente incremento.

## Siguiente Incremento De Seguridad

1. Eliminar cualquier secreto real del repositorio y rotarlo si fue expuesto.
2. Endurecer el pipeline con DAST, validaciones de politicas ABAC y gates de despliegue.
3. Migrar a multiples claves de firma verificables para permitir rotacion sin perder auditoria retroactiva.
