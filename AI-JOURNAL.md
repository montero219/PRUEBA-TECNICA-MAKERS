# AI Journal

## Proposito

Este documento registra como utilice herramientas de inteligencia artificial durante el desarrollo de Atlas PARS. No busca presentar la IA como autora de la solucion, sino mostrar criterio: que decisiones tome, que sugerencias acepte o rechace, como verifique resultados y que limites mantuve bajo responsabilidad humana.

La IA fue utilizada como apoyo para explorar alternativas, cuestionar decisiones de diseno, acelerar tareas mecanicas, revisar consistencia arquitectonica y preparar cambios concretos sobre el repositorio.

Las decisiones finales de alcance, arquitectura, modelo de datos y logica de negocio fueron revisadas y asumidas por mi. No trate las respuestas de la IA como fuente de verdad: cuando una propuesta agregaba complejidad innecesaria, mezclaba responsabilidades o no podia defenderla tecnicamente, la descarte o la reformule.

## Resumen De Uso

| Herramienta | Uso principal | Resultado |
|---|---|---|
| ChatGPT | Discusion, analisis critico, explicacion y revision de alternativas. | Ayudo a razonar alcance del MVP, arquitectura, modelo de datos, ABAC, multi-tenancy, Docker, EF Core y separacion entre evaluacion y auditoria. |
| OpenAI Codex | Ejecucion asistida de cambios concretos sobre el repositorio. | Apoyo migraciones EF Core, datos iniciales, Docker/PostgreSQL, repositorios, servicios, pruebas, CI/security gates, IaC y documentacion de entrega. |

## Como La IA Apoyo El Proceso

### 1. ChatGPT

Use ChatGPT principalmente para discutir y cuestionar decisiones antes de intervenir el repositorio. En lugar de solicitar una solucion completa, trabaje decisiones de forma incremental.

Ejemplos:

- Discutir si Atlas debia almacenar usuarios, roles y permisos, o limitarse a decidir autorizacion sobre atributos recibidos.
- Revisar la relacion entre organizacion, recurso protegido, operacion, regla y version.
- Cuestionar columnas redundantes en el modelo de datos.
- Separar evaluacion de autorizacion, trazabilidad y auditoria.
- Entender implicaciones de Docker y PostgreSQL antes de incorporarlos.
- Disenar prompts mas acotados para Codex.

### 2. Codex

Use Codex para intervenir el repositorio cuando la decision tecnica ya estaba razonablemente acotada. Mi criterio fue evitar pedirle a Codex que "construyera Atlas PARS completo"; preferi entregarle tareas pequenas, con contexto limitado y resultado verificable.

Ejemplos:

- Generar o ajustar migraciones.
- Crear inserts de datos iniciales.
- Preparar PostgreSQL mediante Docker Compose.
- Implementar metodos concretos entre repositorios, servicios e interfaces.
- Crear pruebas unitarias e integracion end-to-end.
- Ajustar GitHub Actions, Terraform y documentacion.

### 3. Analisis de alcance

Use IA para comparar la prueba contra el estado real del repositorio y separar lo implementado, lo incompleto y lo pendiente. Esto ayudo a priorizar el nucleo evaluable sobre tareas perifericas.

Decisiones tomadas:

- Priorizar el motor ABAC, auditoria, firma de decisiones, pruebas y CI/security gates.
- No construir frontend porque no aportaba al criterio principal de evaluacion.
- Documentar explicitamente lo pendiente: despliegue cloud real, observabilidad productiva y pitch.

### 4. Implementacion asistida

Use IA como copiloto para acelerar cambios tecnicos, especialmente en:

- Auditoria persistente de decisiones.
- Firma HMAC-SHA256 de cada decision.
- Pruebas unitarias e integracion end-to-end.
- GitHub Actions para CI y seguridad.
- IaC de referencia con Terraform para Azure.

Cada cambio relevante fue validado con comandos locales y/o GitHub Actions antes de considerarlo parte de la entrega.

### 5. Depuracion de CI

Cuando fallaron workflows por dependencias, use IA para leer el error y encontrar la causa raiz:

- Dependabot habia propuesto upgrades incompatibles entre EF Core/Npgsql 9.x y paquetes 8.x usados por el proyecto .NET 8.
- Se limito Dependabot para evitar upgrades mayores automaticos en NuGet y Docker base images.
- Se verifico nuevamente que `CI` y `Security` quedaran en verde.

### 6. Documentacion y comunicacion

Use IA para estructurar documentacion que pudiera defenderse en sustentacion:

- `README.md`
- `docs/ARCHITECTURE.md`
- `docs/THREAT-MODEL.md`
- `docs/RUNBOOK.md`
- `docs/ADR/`
- `infra/terraform/README.md`
- `ENTREGA.md`

El criterio usado fue evitar documentacion generica: cada documento debe corresponder a decisiones y codigo existentes.

## Prompts Representativos

### Prompt 1 - Evaluar avance contra requisitos

```text
Compara el estado actual del repositorio contra los requisitos de la prueba.
Separa lo implementado, lo incompleto y lo ausente. No inventes avances.
Prioriza que se pueda defender tecnicamente en sustentacion.
```

Uso: ayudo a identificar que el nucleo ABAC existia, pero faltaban trazabilidad fuerte, auditoria, firma y gates de CI/security.

### Prompt 2 - Analizar fallos de CI

```text
Estos son los logs de GitHub Actions. Explica la causa raiz del fallo y dime
que cambio minimo haria para evitar que vuelva a ocurrir sin romper el proyecto.
```

Uso: ayudo a diagnosticar downgrades NU1605 por mezcla de paquetes EF Core/Npgsql 8.x y 9.x.

### Prompt 3 - Documentar alcance honestamente

```text
Ayudame a escribir una documentacion de entrega que explique lo que existe,
lo que se valido y lo que queda pendiente. No presentes como hecho nada que
no este implementado o verificado.
```

Uso: ayudo a construir una narrativa honesta para README, entrega y sustentacion.


## Sugerencias Aceptadas

| Sugerencia | Por que se acepto | Evidencia |
|---|---|---|
| Agregar auditoria persistente y firma criptografica de decisiones. | Aumenta trazabilidad y seguridad, y alinea la solucion con decisiones auditables. | Codigo en API, migraciones, pruebas unitarias e integracion. |
| Separar CI de Security en GitHub Actions. | Permite ver rapido si falla build/test o controles de seguridad. | Workflows `CI` y `Security` en Actions. |
| Agregar IaC de referencia con Terraform. | Muestra criterio de infraestructura sin requerir gastar o crear nube real. | `infra/terraform` validado con Terraform y Checkov. |
| Crear una guia de entrega. | Facilita que el evaluador encuentre el punto de entrada correcto. | `ENTREGA.md`. |
| Separar el uso de ChatGPT y Codex en el journal. | Hace mas clara la diferencia entre razonar decisiones y ejecutar cambios. | Este documento. |

## Sugerencias Rechazadas O Ajustadas

| Sugerencia | Decision | Motivo |
|---|---|---|
| Presentar la solucion como completa. | Rechazada. | Hay avances fuertes, pero no existe despliegue cloud aplicado, observabilidad productiva ni pitch terminado. |
| Usar el modelo de datos inicial generado por ChatGPT. | Rechazada y reestructurada. | El modelo propuesto era mas amplio de lo necesario para el PoC. Se redujo a organizaciones, recursos protegidos, operaciones, reglas y versiones para cubrir el alcance ABAC sin cargar el proyecto con usuarios, roles o permisos propios. |
| Resolver el problema con un frontend. | Rechazada. | La prueba no evalua UI y el tiempo debia concentrarse en arquitectura, seguridad, pruebas e infraestructura. |
| Tratar Terraform como despliegue real. | Ajustada. | Terraform esta definido y validado, pero no se ejecuto `apply` contra Azure porque requiere suscripcion, secretos y costos reales. |

## Verificacion Realizada

Antes de cerrar la entrega se verifico:

- Restore y build .NET en Release.
- Pruebas unitarias: 34/34.
- Prueba de integracion contra PostgreSQL: 1/1.
- Docker build de la API.
- Trivy filesystem e imagen Docker sin hallazgos HIGH/CRITICAL.
- Terraform `fmt`, `validate`.
- Checkov sobre IaC con cero fallos.
- GitHub Actions `CI` y `Security` en verde sobre la rama `main`.

## Riesgos Y Limites

- La IA pudo acelerar escritura y analisis, pero no reemplaza la responsabilidad de entender la arquitectura.
- Las decisiones de seguridad no deben aceptarse solo porque una IA las sugiera.
- La evidencia de pruebas debe venir de comandos reales, no de inferencias.
- El IaC no fue aplicado en Azure; por tanto no se debe vender como despliegue cloud real.
- La solucion requiere evolucion para produccion: observabilidad, rotacion de claves, despliegue automatizado, aprobaciones por ambiente y pruebas de carga.

## Ahorro De Tiempo Estimado

La IA redujo tiempo en:

- Lectura comparativa de requisitos vs repositorio.
- Redaccion inicial de documentacion.
- Diagnostico de logs de CI.
- Generacion de comandos de validacion.
- Revision de alcance pendiente.

Estimacion: entre 4 y 6 horas de ahorro, principalmente en analisis, documentacion y depuracion guiada. El tiempo humano se concentro en decidir alcance, validar resultados y preparar una entrega defendible.

## Reflexion Final

La IA fue usada como acelerador y revisor, no como sustituto del criterio tecnico. La entrega refleja decisiones humanas sobre alcance, seguridad y priorizacion: se eligio construir un nucleo ABAC verificable, con auditoria y firma, antes que intentar cubrir superficialmente todos los posibles extras.
