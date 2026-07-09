# Contexto de negocio y enfoque ABAC

## 1. Historia del caso

**Finora** es una plataforma financiera ficticia que presta servicios de pagos y transferencias a diferentes empresas en Colombia. Cada empresa opera como un **tenant independiente** dentro de la plataforma y administra sus propios clientes, empleados y operaciones.

Actualmente, las reglas de autorización están distribuidas entre diferentes aplicaciones. Esto genera comportamientos inconsistentes: una misma operación puede ser permitida en un canal y rechazada en otro, y resulta difícil explicar posteriormente por qué una acción fue autorizada.

Para resolver este problema, Finora adopta **Atlas PARS** como servicio central de decisiones de acceso para operaciones sensibles.

## 2. Tipo de ABAC propuesto

Se implementará un modelo de **ABAC contextual y orientado a riesgo**.

La decisión no dependerá únicamente del rol del usuario. Se evaluará la combinación de:

- **Actor:** quién solicita la operación.
- **Recurso:** sobre qué elemento actúa.
- **Acción:** qué intenta realizar.
- **Contexto:** bajo qué condiciones ocurre.
- **Riesgo:** qué tan anormal o sensible es la situación.

### Ejemplo

Un supervisor puede tener permiso para aprobar transferencias, pero una transferencia de alto monto realizada de madrugada desde un dispositivo desconocido puede requerir validación adicional.

## 3. Escenarios de negocio

Atlas evaluará inicialmente tres tipos de operaciones sensibles:

### Transferencias financieras

Control de operaciones según monto, propiedad de la cuenta, ubicación, horario y nivel de riesgo.

### Modificación de datos personales

Control de cambios sobre información propia, información de terceros y datos considerados críticos.

### Accesos administrativos

Control de funciones privilegiadas según tenant, rol, dispositivo y contexto de acceso.

## 4. Decisiones posibles

- **PERMIT:** la operación cumple las condiciones necesarias y puede ejecutarse.
- **DENY:** la operación incumple una política de seguridad o aislamiento y debe rechazarse.
- **CHALLENGE:** la operación podría ser válida, pero requiere una validación adicional antes de continuar, como MFA, confirmación reforzada o segunda aprobación.

## 5. Principios de negocio

1. Un rol por sí solo no garantiza acceso.
2. Ningún usuario puede operar sobre recursos de otro tenant.
3. A mayor sensibilidad o riesgo, mayor nivel de control.
4. Toda decisión debe poder explicar qué política y condiciones produjeron el resultado.

## 6. Ejemplo representativo

Un supervisor de la **Empresa A** intenta aprobar una transferencia por **$8.000.000 COP** a las **2:30 a. m.** desde un **dispositivo no reconocido**.

Aunque su rol permite aprobar transferencias, el monto elevado, el horario atípico y el dispositivo desconocido incrementan el riesgo.

**Resultado esperado:** `CHALLENGE`

**Motivo:** la operación requiere una validación adicional antes de ser autorizada.
