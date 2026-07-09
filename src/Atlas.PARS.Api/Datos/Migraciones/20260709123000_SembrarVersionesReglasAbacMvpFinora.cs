using Atlas.PARS.Api.Datos;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.PARS.Api.Datos.Migraciones
{
    /// <inheritdoc />
    [DbContext(typeof(ContextoAtlas))]
    [Migration("20260709123000_SembrarVersionesReglasAbacMvpFinora")]
    public partial class SembrarVersionesReglasAbacMvpFinora : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH reglas_base AS (
                    SELECT
                        reglas_autorizacion.id,
                        organizaciones.codigo AS codigo_organizacion,
                        reglas_autorizacion.codigo AS codigo_regla
                    FROM reglas_autorizacion
                    JOIN operaciones
                        ON operaciones.id = reglas_autorizacion.id_operacion
                    JOIN recursos_protegidos
                        ON recursos_protegidos.id = operaciones.id_recurso_protegido
                    JOIN organizaciones
                        ON organizaciones.id = recursos_protegidos.id_organizacion
                    WHERE
                        organizaciones.codigo IN ('FINORA', 'ORG_EXTERNA')
                        AND recursos_protegidos.codigo = 'TRANSFERENCIA'
                        AND operaciones.codigo = 'APROBAR'
                )
                INSERT INTO versiones_regla (
                    id,
                    id_regla_autorizacion,
                    numero_version,
                    condiciones,
                    decision_si_cumple,
                    prioridad,
                    vigencia_desde,
                    vigencia_hasta,
                    fecha_creacion
                )
                SELECT
                    versiones.id_version::uuid,
                    reglas_base.id,
                    '1.0.0',
                    versiones.condiciones::jsonb,
                    versiones.decision_si_cumple,
                    versiones.prioridad,
                    '2026-07-09T00:00:00Z'::timestamptz,
                    NULL,
                    '2026-07-09T00:00:00Z'::timestamptz
                FROM reglas_base
                JOIN (
                    VALUES
                        (
                            'FINORA',
                            'VALIDAR_AISLAMIENTO_TENANT',
                            '50000000-0000-0000-0000-000000000001',
                            'DENY',
                            10,
                            $$
                            {
                                "todas": [
                                    {
                                        "fuente": "actor",
                                        "atributo": "organizacion",
                                        "operador": "distinto",
                                        "compararCon": {
                                            "fuente": "recurso",
                                            "atributo": "organizacion"
                                        }
                                    }
                                ]
                            }
                            $$
                        ),
                        (
                            'FINORA',
                            'BLOQUEAR_RIESGO_CRITICO',
                            '50000000-0000-0000-0000-000000000002',
                            'DENY',
                            20,
                            $$
                            {
                                "todas": [
                                    {
                                        "fuente": "contexto",
                                        "atributo": "nivelRiesgo",
                                        "operador": "en",
                                        "valor": ["CRITICO"]
                                    }
                                ]
                            }
                            $$
                        ),
                        (
                            'FINORA',
                            'MONTO_SENSIBLE',
                            '50000000-0000-0000-0000-000000000003',
                            'CHALLENGE',
                            40,
                            $$
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
                            $$
                        ),
                        (
                            'FINORA',
                            'CONTEXTO_SOSPECHOSO',
                            '50000000-0000-0000-0000-000000000004',
                            'CHALLENGE',
                            30,
                            $$
                            {
                                "todas": [
                                    {
                                        "fuente": "recurso",
                                        "atributo": "monto",
                                        "operador": "mayor_o_igual",
                                        "valor": 8000000
                                    },
                                    {
                                        "fuente": "contexto",
                                        "atributo": "hora",
                                        "operador": "entre_horas",
                                        "desde": "00:00",
                                        "hasta": "05:00"
                                    },
                                    {
                                        "fuente": "contexto",
                                        "atributo": "dispositivoConfiable",
                                        "operador": "igual",
                                        "valor": false
                                    },
                                    {
                                        "fuente": "contexto",
                                        "atributo": "nivelRiesgo",
                                        "operador": "en",
                                        "valor": ["BAJO", "MEDIO"]
                                    }
                                ]
                            }
                            $$
                        ),
                        (
                            'FINORA',
                            'PERMITIR_TRANSFERENCIA_NORMAL',
                            '50000000-0000-0000-0000-000000000005',
                            'PERMIT',
                            100,
                            $$
                            {
                                "todas": [
                                    {
                                        "fuente": "actor",
                                        "atributo": "rol",
                                        "operador": "igual",
                                        "valor": "CLIENTE"
                                    },
                                    {
                                        "fuente": "actor",
                                        "atributo": "organizacion",
                                        "operador": "igual",
                                        "compararCon": {
                                            "fuente": "recurso",
                                            "atributo": "organizacion"
                                        }
                                    },
                                    {
                                        "fuente": "recurso",
                                        "atributo": "monto",
                                        "operador": "menor_o_igual",
                                        "valor": 1000000
                                    },
                                    {
                                        "fuente": "contexto",
                                        "atributo": "hora",
                                        "operador": "entre_horas",
                                        "desde": "06:00",
                                        "hasta": "22:00"
                                    },
                                    {
                                        "fuente": "contexto",
                                        "atributo": "dispositivoConfiable",
                                        "operador": "igual",
                                        "valor": true
                                    },
                                    {
                                        "fuente": "contexto",
                                        "atributo": "nivelRiesgo",
                                        "operador": "igual",
                                        "valor": "BAJO"
                                    }
                                ]
                            }
                            $$
                        ),
                        (
                            'ORG_EXTERNA',
                            'VALIDAR_AISLAMIENTO_TENANT',
                            '50000000-0000-0000-0000-000000000101',
                            'DENY',
                            10,
                            $$
                            {
                                "todas": [
                                    {
                                        "fuente": "actor",
                                        "atributo": "organizacion",
                                        "operador": "distinto",
                                        "compararCon": {
                                            "fuente": "recurso",
                                            "atributo": "organizacion"
                                        }
                                    }
                                ]
                            }
                            $$
                        ),
                        (
                            'ORG_EXTERNA',
                            'BLOQUEAR_RIESGO_CRITICO',
                            '50000000-0000-0000-0000-000000000102',
                            'DENY',
                            20,
                            $$
                            {
                                "todas": [
                                    {
                                        "fuente": "contexto",
                                        "atributo": "nivelRiesgo",
                                        "operador": "en",
                                        "valor": ["CRITICO"]
                                    }
                                ]
                            }
                            $$
                        ),
                        (
                            'ORG_EXTERNA',
                            'MONTO_SENSIBLE',
                            '50000000-0000-0000-0000-000000000103',
                            'CHALLENGE',
                            40,
                            $$
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
                            $$
                        ),
                        (
                            'ORG_EXTERNA',
                            'CONTEXTO_SOSPECHOSO',
                            '50000000-0000-0000-0000-000000000104',
                            'CHALLENGE',
                            30,
                            $$
                            {
                                "todas": [
                                    {
                                        "fuente": "recurso",
                                        "atributo": "monto",
                                        "operador": "mayor_o_igual",
                                        "valor": 8000000
                                    },
                                    {
                                        "fuente": "contexto",
                                        "atributo": "hora",
                                        "operador": "entre_horas",
                                        "desde": "00:00",
                                        "hasta": "05:00"
                                    },
                                    {
                                        "fuente": "contexto",
                                        "atributo": "dispositivoConfiable",
                                        "operador": "igual",
                                        "valor": false
                                    },
                                    {
                                        "fuente": "contexto",
                                        "atributo": "nivelRiesgo",
                                        "operador": "en",
                                        "valor": ["BAJO", "MEDIO"]
                                    }
                                ]
                            }
                            $$
                        ),
                        (
                            'ORG_EXTERNA',
                            'PERMITIR_TRANSFERENCIA_NORMAL',
                            '50000000-0000-0000-0000-000000000105',
                            'PERMIT',
                            100,
                            $$
                            {
                                "todas": [
                                    {
                                        "fuente": "actor",
                                        "atributo": "rol",
                                        "operador": "igual",
                                        "valor": "CLIENTE"
                                    },
                                    {
                                        "fuente": "actor",
                                        "atributo": "organizacion",
                                        "operador": "igual",
                                        "compararCon": {
                                            "fuente": "recurso",
                                            "atributo": "organizacion"
                                        }
                                    },
                                    {
                                        "fuente": "recurso",
                                        "atributo": "monto",
                                        "operador": "menor_o_igual",
                                        "valor": 1000000
                                    },
                                    {
                                        "fuente": "contexto",
                                        "atributo": "hora",
                                        "operador": "entre_horas",
                                        "desde": "06:00",
                                        "hasta": "22:00"
                                    },
                                    {
                                        "fuente": "contexto",
                                        "atributo": "dispositivoConfiable",
                                        "operador": "igual",
                                        "valor": true
                                    },
                                    {
                                        "fuente": "contexto",
                                        "atributo": "nivelRiesgo",
                                        "operador": "igual",
                                        "valor": "BAJO"
                                    }
                                ]
                            }
                            $$
                        )
                ) AS versiones(
                    codigo_organizacion,
                    codigo_regla,
                    id_version,
                    decision_si_cumple,
                    prioridad,
                    condiciones
                )
                    ON versiones.codigo_organizacion = reglas_base.codigo_organizacion
                    AND versiones.codigo_regla = reglas_base.codigo_regla
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM versiones_regla
                WHERE id IN (
                    '50000000-0000-0000-0000-000000000001',
                    '50000000-0000-0000-0000-000000000002',
                    '50000000-0000-0000-0000-000000000003',
                    '50000000-0000-0000-0000-000000000004',
                    '50000000-0000-0000-0000-000000000005',
                    '50000000-0000-0000-0000-000000000101',
                    '50000000-0000-0000-0000-000000000102',
                    '50000000-0000-0000-0000-000000000103',
                    '50000000-0000-0000-0000-000000000104',
                    '50000000-0000-0000-0000-000000000105'
                );
                """);
        }
    }
}
