# Arquitectura

## Objetivo

Atlas PARS centraliza decisiones de autorizacion para operaciones sensibles. El PoC actual implementa el nucleo ABAC: recibe una solicitud, resuelve tenant/recurso/operacion, evalua reglas declarativas versionadas y devuelve `PERMIT`, `DENY` o `CHALLENGE`.

## Alcance Actual

Incluido:

- API .NET 8.
- Endpoint `POST /authorize`.
- PostgreSQL.
- Versionado temporal de reglas.
- Evaluador JSON para condiciones ABAC.
- Reglas de ejemplo para transferencias financieras del tenant Finora.
- Auditoria persistente de decisiones en tabla append-only.
- `solicitud_hash` SHA-256 sobre solicitud canonica.
- Firma HMAC-SHA256 del resultado de cada decision, con clave inyectada por configuracion.
- `X-Correlation-Id` para rastrear decisiones entre consumidor, API y base.
- IaC de referencia para Azure Container Apps, PostgreSQL Flexible Server, Key Vault, ACR y Log Analytics.

Fuera del alcance implementado:

- Despliegue cloud real.
- Observabilidad productiva.
- Despliegue cloud automatizado completo con OIDC, build/push de imagen y migraciones.
- Multiples claves de firma verificables (rotacion sin perder auditoria retroactiva).

## C4 Nivel 1 - Contexto

```mermaid
flowchart LR
    usuario["Usuario o sistema consumidor"]
    negocio["Sistema de negocio<br/>Transferencias, datos personales o administracion"]
    atlas["Atlas PARS<br/>Servicio central de decisiones ABAC"]
    secretos["Configuracion de clave de firma<br/>User-secrets / env var hoy<br/>Futuro: Key Vault"]
    obs["Observabilidad<br/>Futuro: logs, metricas y trazas"]

    usuario -->|"Inicia operacion"| negocio
    negocio -->|"POST /authorize"| atlas
    atlas -->|"PERMIT / DENY / CHALLENGE + keyId + firma"| negocio
    atlas -->|"Obtiene clave de firma HMAC-SHA256"| secretos
    atlas -.->|"Futuro: emite telemetria"| obs
```

## C4 Nivel 2 - Contenedores

```mermaid
flowchart LR
    consumidor["Squad consumidor<br/>Aplicacion que necesita una decision"]
    api["Atlas.PARS.Api<br/>.NET 8 Web API"]
    db[("PostgreSQL<br/>Catalogo, reglas versionadas y auditoria")]
    secretos["User-secrets / env var<br/>Futuro: Key Vault"]
    ci["GitHub Actions<br/>CI y seguridad baseline"]

    consumidor -->|"HTTP JSON<br/>POST /authorize"| api
    api -->|"EF Core / Npgsql<br/>Consulta reglas y registra auditoria"| db
    api -->|"Obtiene KeyId y clave de firma"| secretos
    ci -->|"Build, test y scan; deploy futuro"| api
```

## C4 Nivel 3 - Componentes API

```mermaid
flowchart TB
    subgraph api["Atlas.PARS.Api"]
        controller["AutorizacionController<br/>Valida header y body"]
        servicio["ServicioAutorizacion<br/>Orquesta decision"]
        evaluador["EvaluadorCondiciones<br/>Motor ABAC JSON"]
        hash["CalculadorHashSolicitud<br/>SHA-256 canonico"]
        firmador["FirmadorDecisionesAutorizacionHmac<br/>HMAC-SHA256 del resultado"]
        repoOrg["RepositorioOrganizaciones"]
        repoRec["RepositorioRecursosProtegidos"]
        repoOp["RepositorioOperaciones"]
        repoReglas["RepositorioReglasAutorizacion"]
        repoAudit["RepositorioDecisionesAutorizacion"]
        dbContext["ContextoAtlas<br/>EF Core DbContext"]
    end

    db[("PostgreSQL")]

    controller -->|"AutorizarAsync"| servicio
    servicio -->|"Obtiene organizacion"| repoOrg
    servicio -->|"Obtiene recurso"| repoRec
    servicio -->|"Obtiene operacion"| repoOp
    servicio -->|"Obtiene reglas vigentes"| repoReglas
    servicio -->|"Evalua condiciones"| evaluador
    servicio -->|"Calcula solicitud_hash"| hash
    servicio -->|"Firma el resultado de la decision"| firmador
    servicio -->|"Registra decision auditada (con firma)"| repoAudit
    repoOrg --> dbContext
    repoRec --> dbContext
    repoOp --> dbContext
    repoReglas --> dbContext
    repoAudit --> dbContext
    dbContext -->|"SQL"| db
```

## Flujo De Autorizacion

```mermaid
sequenceDiagram
    autonumber
    participant C as Consumidor
    participant A as AutorizacionController
    participant S as ServicioAutorizacion
    participant R as Repositorios
    participant E as EvaluadorCondiciones
    participant F as FirmadorDecisionesAutorizacionHmac
    participant DB as PostgreSQL

    C->>A: POST /authorize + X-Tenant-Code + X-Correlation-Id opcional
    A->>S: AutorizarAsync(codigoTenant, solicitud, correlationId)
    S->>S: Normaliza correlationId y calcula solicitud_hash
    S->>R: Obtener organizacion
    R->>DB: SELECT organizaciones
    S->>R: Obtener recurso protegido
    R->>DB: SELECT recursos_protegidos
    S->>R: Obtener operacion
    R->>DB: SELECT operaciones
    S->>R: Obtener reglas vigentes por fecha
    R->>DB: SELECT versiones_regla ordenadas por prioridad
    loop Por cada regla vigente
        S->>E: Evaluar(condiciones, solicitud)
        E-->>S: true/false
    end
    S->>F: Firmar(idDecision, decision, codigoRegla, solicitudHash, correlationId, fechaDecision)
    F-->>S: keyId + firma (HMAC-SHA256)
    S->>DB: INSERT decisiones_autorizacion (incluye key_id_firma, algoritmo_firma, firma)
    S-->>A: ResultadoAutorizacion con idDecision, correlationId, solicitudHash, keyId y firma
    A-->>C: 200 OK + decision auditada y firmada
```

## Modelo De Datos

```mermaid
erDiagram
    ORGANIZACIONES ||--o{ RECURSOS_PROTEGIDOS : contiene
    RECURSOS_PROTEGIDOS ||--o{ OPERACIONES : expone
    OPERACIONES ||--o{ REGLAS_AUTORIZACION : tiene
    REGLAS_AUTORIZACION ||--o{ VERSIONES_REGLA : versiona
    ORGANIZACIONES ||--o{ DECISIONES_AUTORIZACION : audita
    RECURSOS_PROTEGIDOS ||--o{ DECISIONES_AUTORIZACION : referencia
    OPERACIONES ||--o{ DECISIONES_AUTORIZACION : referencia
    REGLAS_AUTORIZACION ||--o{ DECISIONES_AUTORIZACION : explica
    VERSIONES_REGLA ||--o{ DECISIONES_AUTORIZACION : version_aplicada

    ORGANIZACIONES {
        uuid id
        string codigo
        string nombre
        string zona_horaria
    }

    RECURSOS_PROTEGIDOS {
        uuid id
        uuid id_organizacion
        string codigo
        string nombre
    }

    OPERACIONES {
        uuid id
        uuid id_recurso_protegido
        string codigo
        string nombre
    }

    REGLAS_AUTORIZACION {
        uuid id
        uuid id_operacion
        string codigo
        string nombre
    }

    VERSIONES_REGLA {
        uuid id
        uuid id_regla_autorizacion
        string numero_version
        jsonb condiciones
        string decision_si_cumple
        int prioridad
        timestamptz vigencia_desde
        timestamptz vigencia_hasta
    }

    DECISIONES_AUTORIZACION {
        uuid id
        string codigo_organizacion
        uuid id_organizacion
        uuid id_recurso_protegido
        uuid id_operacion
        uuid id_regla_autorizacion
        uuid id_version_regla
        string codigo_recurso_solicitado
        string codigo_operacion_solicitada
        string decision
        string motivo
        string codigo_regla
        string numero_version_regla
        jsonb actor
        jsonb recurso
        jsonb contexto
        string algoritmo_hash
        string solicitud_hash
        string correlation_id
        timestamptz fecha_decision
        string key_id_firma
        string algoritmo_firma
        string firma
    }
```

## Reglas Versionadas

Cada regla puede tener varias versiones, pero la migracion inicial agrega una restriccion de no solapamiento temporal por regla usando `btree_gist` y `EXCLUDE USING gist`. Esto evita que dos versiones de la misma regla esten vigentes al mismo tiempo.

La seleccion de reglas usa:

- `id_operacion`
- `vigencia_desde <= fechaEvaluacion`
- `vigencia_hasta IS NULL OR vigencia_hasta > fechaEvaluacion`
- orden ascendente por `prioridad`

## Decisiones De Diseno

- El motor usa JSON propio para mantener el MVP explicable y testeable.
- El sistema falla cerrado: si no hay regla vigente o ninguna regla aplica, responde `DENY`.
- Los errores tempranos de tenant, recurso u operacion no configurada tambien responden `DENY` auditado.
- La prioridad de reglas permite que controles de seguridad, como aislamiento de tenant y riesgo critico, ganen sobre permisos normales.
- PostgreSQL guarda las reglas como `jsonb` y mantiene integridad relacional del catalogo.
- Cada decision controlada se persiste en `decisiones_autorizacion`.
- La tabla de auditoria es append-only: un trigger bloquea `UPDATE` y `DELETE`.
- `solicitud_hash` usa SHA-256 sobre una representacion canonica de tenant, recurso, operacion, actor, recurso y contexto.
- `correlation_id` viene de `X-Correlation-Id` o se genera internamente si el consumidor no lo envia.
- El resultado de la decision (no la solicitud cruda) se firma con HMAC-SHA256 sobre un payload canonico que incluye `idDecision`, `codigoOrganizacion`, `decision`, `codigoRegla`, `solicitudHash`, `correlationId` y `fechaDecision`.
- El firmador falla cerrado al intentar firmar una decision si la clave no esta configurada o no es Base64 valido.
- Se eligio una unica clave activa (sin arreglo de claves para rotacion) por simplicidad de PoC; ver ADR 0004 para la limitacion aceptada.

## Trazabilidad De Decisiones

La respuesta de `POST /authorize` incluye:

```json
{
  "idDecision": "uuid",
  "decision": "PERMIT",
  "motivo": "Aplico la regla 'PERMITIR_TRANSFERENCIA_NORMAL'.",
  "codigoRegla": "PERMITIR_TRANSFERENCIA_NORMAL",
  "correlationId": "corr-123",
  "solicitudHash": "hex-sha256",
  "keyId": "atlas-pars-hmac-2026-07",
  "firma": "base64-hmac-sha256"
}
```

El registro persistente guarda la solicitud evaluada en tres documentos `jsonb`: `actor`, `recurso` y `contexto`. Tambien guarda la regla y version aplicada cuando existen. Si la decision ocurre antes de resolver todo el catalogo, por ejemplo tenant inexistente, recurso inexistente u operacion inexistente, las FKs no resueltas quedan `NULL`, pero los codigos solicitados y el resultado `DENY` quedan auditados.

La auditoria ahora prueba integridad operacional y criptografica. `key_id_firma`, `algoritmo_firma` y `firma` permiten reconstruir que evaluo Atlas, por que regla respondio, y verificar mediante HMAC-SHA256 que la decision no fue alterada despues de emitida (recalculando la firma con la misma clave y comparando en tiempo constante). Las tres columnas son nullable en conjunto: las decisiones auditadas antes de este incremento no tienen firma retroactiva.

## Riesgos Arquitectonicos

- El evaluador actual soporta `todas` como AND, pero no soporta OR, negaciones compuestas ni reglas anidadas.
- La API no tiene todavia middleware de errores global para fallas inesperadas.
- La cadena de conexion y la clave de firma deben gestionarse con user-secrets o variables de entorno; no deben commitearse valores reales.
- Una unica clave de firma activa: rotarla invalida la verificacion de decisiones historicas firmadas con la clave anterior.
- No se ha probado P95 < 150 ms bajo carga.

## Evolucion Propuesta

1. Migrar a multiples claves de firma verificables para permitir rotacion sin perder auditoria retroactiva.
2. Agregar endpoint o herramienta interna para verificar firma y hash sin tener que instanciar el servicio manualmente.
3. Ampliar pruebas automatizadas end-to-end para el Caso C (contexto sospechoso) y Caso D (riesgo critico) del catalogo ABAC MVP; `CHALLENGE` (monto sensible) y `DENY` por aislamiento de tenant ya estan cubiertos.
4. Incorporar health checks, logs estructurados y OpenTelemetry.
5. Aplicar IaC en una suscripcion real, validar costos y documentar outputs.
6. Extender CI/CD con despliegue cloud, ambientes y aprobaciones.
