# AI Journal

## Proposito

Este journal documenta el uso de IA durante el desarrollo de Atlas PARS. La intencion es mostrar criterio profesional: que se acepto, que se rechazo, que se verifico y que queda bajo responsabilidad humana.

## Inventario De Herramientas IA

| Herramienta | Uso | Resultado |
|---|---|---|
| Codex | Analisis del PDF de la prueba, revision del repositorio, estimacion de avance, apoyo en documentacion evaluable e implementacion asistida. | Se identificaron brechas y se cerro un incremento de auditoria, firma criptografica, prueba de integracion, documentacion y CI/security baseline. |

## Prompts Disenados

Estos son prompts usados o utiles dentro del proceso actual. Deben completarse con fecha/hora si se usa otra herramienta adicional.

### Prompt 1 - Evaluar avance contra rubrica

```text
Lee la prueba tecnica y compara los entregables contra el repositorio actual.
Dame un porcentaje de avance, separando nucleo funcional, seguridad, infraestructura,
CI/CD, pruebas, documentacion y uso de IA. No inventes avances: marca claramente lo
implementado, lo incompleto y lo ausente.
```

Motivo: obliga a contrastar contra la rubrica, no contra sensacion subjetiva de progreso.

### Prompt 2 - Separar ABAC de trazabilidad

```text
Revisa el flujo de autorizacion actual y dime si el nucleo ABAC ya existe.
Separa la respuesta entre evaluacion de reglas, trazabilidad, auditoria y firma criptografica.
```

Motivo: evita mezclar "motor de reglas funcionando" con "requisito completo de decision auditable".

### Prompt 3 - Documentacion honesta

```text
Ayudame a convertir el estado actual del proyecto en documentacion evaluable.
No prometas componentes que aun no existen. Documenta arquitectura, amenazas, runbook,
ADRs y backlog con el estado actual del codigo.
```

Motivo: la sustentacion penaliza no poder defender el repositorio. Este prompt fuerza honestidad.

## Sugerencias IA Rechazadas

| Sugerencia | Decision | Motivo |
|---|---|---|
| Presentar la solucion como completa. | Rechazada. | Aunque ya hay auditoria, firma, prueba de integracion, CI/security baseline e IaC de referencia, todavia faltan despliegue cloud real, observabilidad productiva y video pitch. |
| Tratar la documentacion como relleno generico. | Rechazada. | La rubrica da peso alto a comunicacion, arquitectura y seguridad. La documentacion debe explicar decisiones reales del repo. |

## Ahorro De Tiempo

La IA ayudo a mapear el PDF contra el repositorio y detectar brechas en minutos. Hacer esa matriz manualmente habria tomado aproximadamente 45 a 60 minutos, especialmente al revisar codigo, pruebas, docs y rubrica.

## Momento Donde La IA Pudo Desviar

Una posible desviacion fue estimar progreso solo por cantidad de archivos creados. Se corrigio contrastando contra requisitos verificables: `dotnet test`, cobertura, existencia real de pipeline/IaC, y presencia o ausencia de auditoria/firma.

## Que No Debe Delegarse A La IA

- Decidir si un riesgo de seguridad es aceptable para produccion.
- Defender decisiones arquitectonicas sin entender sus consecuencias.
- Aprobar criptografia sin revision humana.
- Inventar evidencia de pruebas, cobertura, costos o despliegues.
- Escribir un journal falso para aparentar criterio.

## Estado Honesto

La IA se uso como apoyo de analisis, implementacion y redaccion. Las decisiones de alcance siguen siendo responsabilidad humana. La solucion actual demuestra un nucleo ABAC con trazabilidad, auditoria append-only, firma HMAC-SHA256, prueba de integracion, CI/security baseline e IaC de referencia; aun necesita despliegue cloud aplicado, observabilidad productiva y endurecimiento operativo para acercarse a una entrega completa.
