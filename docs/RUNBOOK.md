# Runbook

## Objetivo

Guia operativa para levantar, verificar y diagnosticar Atlas PARS en el estado actual del PoC.

## Requisitos

- .NET 8 SDK.
- Docker Desktop o Docker Engine.
- Acceso al repositorio.
- PostgreSQL levantado con Docker Compose o instancia compatible.
- Herramientas locales de .NET restauradas desde `.config/dotnet-tools.json`.

## Convenciones De Comandos

Los comandos comunes usan rutas con `/`, que funcionan en Windows PowerShell, macOS y Linux para `dotnet` y `docker compose`.

Cuando el comando depende del shell, el runbook muestra dos variantes:

- Windows: PowerShell.
- macOS/Linux: Bash o Zsh.

## Levantar Entorno Local

1. Crear archivo de entorno local para PostgreSQL:

Windows PowerShell:

```powershell
Copy-Item deploy/compose/.env.example deploy/compose/.env
```

macOS/Linux:

```bash
cp deploy/compose/.env.example deploy/compose/.env
```

2. Editar `deploy/compose/.env` y cambiar `POSTGRES_PASSWORD`. Ese valor no viene del repositorio: cada persona debe definir un password local y usar el mismo valor mas adelante en `dotnet user-secrets`.

Ejemplo local valido:

```env
POSTGRES_DB=atlas_pars
POSTGRES_USER=atlas_pars
POSTGRES_PASSWORD=atlas_pars_local_dev
POSTGRES_PORT=5433
```

Las variables `ATLAS_FIRMA_*` del ejemplo son referencia para ejecucion containerizada futura; el Compose actual no las consume porque solo levanta PostgreSQL.

3. Levantar base de datos:

```bash
docker compose --env-file deploy/compose/.env -f deploy/compose/compose.yaml up -d
```

El Compose actual solo levanta PostgreSQL. La API se ejecuta localmente con `dotnet run` y toma su configuracion desde user-secrets o variables de entorno.

4. Restaurar herramientas locales:

```bash
dotnet tool restore
```

5. Configurar cadena de conexion como secreto local:

```bash
dotnet user-secrets set "ConnectionStrings:Atlas" "Host=localhost;Port=5433;Database=atlas_pars;Username=atlas_pars;Password=VALOR_DE_POSTGRES_PASSWORD" --project src/Atlas.PARS.Api/Atlas.PARS.Api.csproj
```

Reemplazar `VALOR_DE_POSTGRES_PASSWORD` por el mismo valor definido en `deploy/compose/.env` como `POSTGRES_PASSWORD`.

6. Generar una clave HMAC local para firma de decisiones:

Windows PowerShell:

```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

macOS/Linux:

```bash
openssl rand -base64 32
```

Esta clave no se escribe manualmente. Es un secreto aleatorio de 32 bytes codificado en Base64; el comando imprime el valor que debe copiarse en el siguiente paso. Normalmente se ve como una cadena larga, por ejemplo de 44 caracteres, y puede terminar en `=`.

7. Configurar la clave de firma como secreto local:

```bash
dotnet user-secrets set "FirmaDecisiones:KeyId" "atlas-pars-hmac-local" --project src/Atlas.PARS.Api/Atlas.PARS.Api.csproj
dotnet user-secrets set "FirmaDecisiones:ClaveActivaBase64" "VALOR_BASE64_GENERADO" --project src/Atlas.PARS.Api/Atlas.PARS.Api.csproj
```

`FirmaDecisiones:KeyId` es un identificador legible para saber con que clave se firmo una decision; en local puede quedarse como `atlas-pars-hmac-local`. `FirmaDecisiones:ClaveActivaBase64` debe ser exactamente la salida del comando anterior. La API necesita estos valores para firmar decisiones. Si faltan o la clave no es Base64 valida, la autorizacion falla de forma controlada al intentar firmar la decision.

8. Aplicar migraciones:

```bash
dotnet ef database update --project src/Atlas.PARS.Api/Atlas.PARS.Api.csproj
```

9. Ejecutar API:

```bash
dotnet run --project src/Atlas.PARS.Api/Atlas.PARS.Api.csproj
```

## Verificacion Rapida

Ejecutar pruebas:

```bash
dotnet test Atlas.PARS.sln
```

Ejecutar cobertura de unitarias:

```bash
dotnet test tests/Atlas.PARS.PruebasUnitarias/Atlas.PARS.PruebasUnitarias.csproj --collect:"XPlat Code Coverage"
```

Probar una solicitud `PERMIT`:

Windows PowerShell:

```powershell
$body = @{
  codigoRecurso = "TRANSFERENCIA"
  codigoOperacion = "APROBAR"
  atributosActor = @{
    rol = "CLIENTE"
    organizacion = "FINORA"
  }
  atributosRecurso = @{
    monto = "500000"
    organizacion = "FINORA"
  }
  contexto = @{
    hora = "10:00"
    dispositivoConfiable = "true"
    nivelRiesgo = "BAJO"
  }
} | ConvertTo-Json -Depth 5

Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5000/authorize" `
  -Headers @{
    "X-Tenant-Code" = "FINORA"
    "X-Correlation-Id" = "demo-local-001"
  } `
  -ContentType "application/json" `
  -Body $body
```

macOS/Linux:

```bash
curl -sS -X POST "http://localhost:5000/authorize" \
  -H "X-Tenant-Code: FINORA" \
  -H "X-Correlation-Id: demo-local-001" \
  -H "Content-Type: application/json" \
  -d '{
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
  }'
```

La respuesta incluye `idDecision`, `correlationId`, `solicitudHash`, `keyId` y `firma`. El puerto puede variar segun el perfil local de ASP.NET.

Consultar la auditoria generada:

```sql
SELECT
    id,
    codigo_organizacion,
    codigo_recurso_solicitado,
    codigo_operacion_solicitada,
    decision,
    codigo_regla,
    numero_version_regla,
    solicitud_hash,
    correlation_id,
    key_id_firma,
    algoritmo_firma,
    firma,
    fecha_decision
FROM decisiones_autorizacion
WHERE correlation_id = 'demo-local-001'
ORDER BY fecha_decision DESC;
```

Validar que la tabla de auditoria es append-only:

```sql
UPDATE decisiones_autorizacion
SET motivo = 'prueba'
WHERE correlation_id = 'demo-local-001';
```

La operacion debe fallar con:

```text
decisiones_autorizacion es append-only
```

## Diagnosticar Un `DENY` Sospechoso

1. Obtener `idDecision` o `correlationId` desde la respuesta o logs del consumidor.
2. Consultar `decisiones_autorizacion`.
3. Confirmar `X-Tenant-Code`.
4. Confirmar que `codigoRecurso` existe para ese tenant.
5. Confirmar que `codigoOperacion` existe para ese recurso.
6. Revisar atributos recibidos:
   - `atributosActor.organizacion`
   - `atributosRecurso.organizacion`
   - `atributosRecurso.monto`
   - `contexto.hora`
   - `contexto.dispositivoConfiable`
   - `contexto.nivelRiesgo`
7. Revisar `codigo_regla` en auditoria:
   - `TENANT_NO_CONFIGURADO`
   - `RECURSO_NO_CONFIGURADO`
   - `OPERACION_NO_CONFIGURADA`
   - `SIN_REGLAS_VIGENTES`
   - `SIN_REGLA_APLICABLE`
   - codigo de una regla de negocio, por ejemplo `BLOQUEAR_RIESGO_CRITICO`.
8. Revisar reglas vigentes para la operacion en `versiones_regla`.
9. Revisar prioridad: reglas con menor numero se evaluan primero.
10. Si ninguna regla aplica, el comportamiento esperado es `DENY`.

Consulta util para una decision auditada:

```sql
SELECT
    id,
    decision,
    motivo,
    codigo_regla,
    numero_version_regla,
    actor,
    recurso,
    contexto,
    solicitud_hash,
    correlation_id,
    fecha_decision,
    key_id_firma,
    algoritmo_firma,
    firma
FROM decisiones_autorizacion
WHERE id = '<idDecision>'::uuid;
```

Si `firma` es `NULL`, la decision es anterior al incremento de firma criptografica (no se fabrica una firma retroactiva para filas historicas). Si `firma` esta presente, se puede recalcular con `IFirmadorDecisionesAutorizacion.Verificar` usando el mismo `key_id_firma` y comparar contra el valor persistido — si no coincide, la fila fue alterada fuera del flujo normal de la API.

Consulta util para reglas vigentes:

```sql
SELECT
    o.codigo AS organizacion,
    rp.codigo AS recurso,
    op.codigo AS operacion,
    ra.codigo AS regla,
    vr.numero_version,
    vr.decision_si_cumple,
    vr.prioridad,
    vr.vigencia_desde,
    vr.vigencia_hasta,
    vr.condiciones
FROM versiones_regla vr
JOIN reglas_autorizacion ra ON ra.id = vr.id_regla_autorizacion
JOIN operaciones op ON op.id = ra.id_operacion
JOIN recursos_protegidos rp ON rp.id = op.id_recurso_protegido
JOIN organizaciones o ON o.id = rp.id_organizacion
WHERE o.codigo = 'FINORA'
  AND rp.codigo = 'TRANSFERENCIA'
  AND op.codigo = 'APROBAR'
  AND vr.vigencia_desde <= now()
  AND (vr.vigencia_hasta IS NULL OR vr.vigencia_hasta > now())
ORDER BY vr.prioridad;
```

## Rotacion De Secretos

Estado actual:

- La cadena de conexion debe configurarse por user-secrets o variables de entorno.
- El repositorio no debe contener passwords reales.
- La firma criptografica usa HMAC-SHA256 con una unica clave activa (`FirmaDecisiones:KeyId` / `FirmaDecisiones:ClaveActivaBase64`), tambien por user-secrets o variable de entorno.

Procedimiento local (cadena de conexion):

1. Cambiar `POSTGRES_PASSWORD` en `deploy/compose/.env`.
2. Recrear el contenedor si aplica.
3. Actualizar user-secret:

```bash
dotnet user-secrets set "ConnectionStrings:Atlas" "Host=localhost;Port=5433;Database=atlas_pars;Username=atlas_pars;Password=NUEVO_POSTGRES_PASSWORD" --project src/Atlas.PARS.Api/Atlas.PARS.Api.csproj
```

Procedimiento local (clave de firma):

1. Generar una clave nueva:

Windows PowerShell:

```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
```

macOS/Linux:

```bash
openssl rand -base64 32
```

2. Actualizar user-secrets con el nuevo `KeyId` (identificador legible, por ejemplo `atlas-pars-hmac-2026-08`) y la nueva clave:

```bash
dotnet user-secrets set "FirmaDecisiones:KeyId" "atlas-pars-hmac-2026-08" --project src/Atlas.PARS.Api/Atlas.PARS.Api.csproj
dotnet user-secrets set "FirmaDecisiones:ClaveActivaBase64" "NUEVA_CLAVE_BASE64" --project src/Atlas.PARS.Api/Atlas.PARS.Api.csproj
```

3. Reiniciar la API y ejecutar una prueba de autorizacion. Si la configuracion falta o es invalida, la autorizacion falla de forma controlada al intentar firmar la decision.
4. **Advertencia**: con el diseno actual de una sola clave activa, rotar invalida la verificacion de decisiones firmadas con la clave anterior — no hay forma de recalcular esas firmas viejas con la clave nueva. Si se necesita re-verificar decisiones historicas despues de rotar, conservar la clave anterior fuera del repositorio (por ejemplo en Key Vault, version deshabilitada solo para lectura) antes de rotar.

Procedimiento futuro en nube (ambos secretos):

1. Rotar secreto en Key Vault o Secrets Manager.
2. Publicar nueva version de secreto.
3. Reiniciar workload o refrescar configuracion.
4. Verificar health check y prueba de autorizacion.
5. Revocar version anterior (excepto la clave de firma, que conviene retener solo-lectura para auditoria historica).

## Rollback

Rollback de API:

1. Identificar version anterior estable.
2. Revertir despliegue en orquestador o pipeline.
3. Verificar `POST /authorize` con casos `PERMIT`, `DENY` y `CHALLENGE`.

Rollback de base de datos:

1. Revisar migracion aplicada.
2. Ejecutar rollback controlado con EF Core solo si la migracion es reversible:

```bash
dotnet ef database update MIGRACION_ANTERIOR --project src/Atlas.PARS.Api/Atlas.PARS.Api.csproj
```

3. Si la migracion afecta reglas, validar que no haya versiones solapadas ni ventanas de vigencia incorrectas.

Rollback de reglas:

1. Crear nueva version de regla con vigencia actual y prioridad correcta.
2. Cerrar vigencia de la version defectuosa.
3. Verificar decision con casos de prueba.

## Incidentes Comunes

| Sintoma | Causa probable | Accion |
|---|---|---|
| `TENANT_REQUERIDO` | Falta header `X-Tenant-Code`. | Enviar header. |
| Error de conexion a PostgreSQL | Base apagada o secreto incorrecto. | Revisar Docker y user-secrets. |
| `DENY` inesperado | Atributo faltante o regla no vigente. | Revisar request y reglas vigentes. |
| `CHALLENGE` inesperado | Monto sensible o contexto sospechoso. | Revisar monto, hora, dispositivo y riesgo. |
| `TENANT_NO_CONFIGURADO` | Tenant no existe en catalogo. | Revisar `X-Tenant-Code` y tabla `organizaciones`. |
| `RECURSO_NO_CONFIGURADO` | Recurso no existe para el tenant. | Revisar `recursos_protegidos`. |
| `OPERACION_NO_CONFIGURADA` | Operacion no existe para el recurso. | Revisar `operaciones`. |

## Controles Antes De Entregar

- `dotnet test Atlas.PARS.sln`
- Confirmar que la prueba de integracion `AutorizacionAuditoriaIntegracionPruebas` pasa.
- Revisar que no haya secretos reales en `appsettings*.json`.
- Revisar que `README.md` explique alcance y limitaciones.
- Revisar que `THREAT-MODEL.md` no prometa mitigaciones no implementadas.
- Confirmar que `AI-JOURNAL.md` sea honesto y trazable.
