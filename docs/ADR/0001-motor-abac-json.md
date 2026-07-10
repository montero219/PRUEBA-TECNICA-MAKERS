# ADR 0001 - Motor ABAC JSON Para El MVP

## Estado

Aceptada

## Contexto

Atlas PARS debe evaluar politicas declarativas estilo ABAC/OPA-like. Para el PoC se necesita un motor que sea:

- Facil de explicar en la sustentacion.
- Suficiente para demostrar decisiones `PERMIT`, `DENY` y `CHALLENGE`.
- Versionable en base de datos.
- Probable de completar dentro del tiempo sugerido de la prueba.

Las reglas actuales evaluan atributos de:

- `actor`
- `recurso`
- `contexto`

## Decision

Usar un DSL JSON propio para el MVP. El formato actual define una clausula `todas`, equivalente a AND logico, con condiciones simples:

```json
{
  "todas": [
    {
      "fuente": "recurso",
      "atributo": "monto",
      "operador": "mayor_o_igual",
      "valor": 1000001
    }
  ]
}
```

Operadores soportados:

- `igual`
- `distinto`
- `mayor_o_igual`
- `menor_o_igual`
- `en`
- `entre_horas`

## Alternativas Consideradas

### OPA/Rego

Ventajas:

- Motor maduro.
- Lenguaje conocido para Policy-as-Code.
- Mejor para politicas complejas.

Desventajas para este PoC:

- Aumenta la superficie de integracion.
- Requiere defender Rego, bundle management y forma de despliegue.
- Puede distraer del objetivo de demostrar el nucleo y sus trade-offs.

### Hardcodear reglas en C#

Ventajas:

- Rapido para un demo minimo.
- Facil de depurar.

Desventajas:

- No cumple bien la idea de politicas declarativas versionadas.
- Hace mas dificil evolucionar reglas por tenant.
- Mezcla politica con codigo de aplicacion.

## Consecuencias Positivas

- El motor es pequeno y entendible.
- Las reglas se pueden guardar como `jsonb`.
- Las pruebas unitarias cubren comportamiento del evaluador.
- El evaluador se puede extender incrementalmente.

## Consecuencias Negativas

- No soporta OR, negaciones compuestas ni reglas anidadas.
- Es un DSL propio, por lo que debe mantenerse con disciplina.
- Requiere validacion extra para evitar reglas invalidas en produccion.

## Evolucion

Si el producto crece, se debe evaluar migrar a OPA/Rego o a un motor de reglas formal. La migracion debe hacerse cuando existan suficientes reglas reales que justifiquen el costo operativo.
