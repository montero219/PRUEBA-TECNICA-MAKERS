using Atlas.PARS.Api.Datos;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.PARS.Api.Datos.Migraciones
{
    /// <inheritdoc />
    [DbContext(typeof(ContextoAtlas))]
    [Migration("20260709120000_SembrarCatalogoAbacMvpFinora")]
    public partial class SembrarCatalogoAbacMvpFinora : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO organizaciones (id, codigo, nombre, zona_horaria)
                VALUES
                    ('10000000-0000-0000-0000-000000000001', 'FINORA', 'Finora', 'America/Bogota'),
                    ('10000000-0000-0000-0000-000000000002', 'ORG_EXTERNA', 'Organizacion externa', 'America/Bogota')
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql("""
                WITH organizaciones_base AS (
                    SELECT
                        id,
                        codigo
                    FROM organizaciones
                    WHERE codigo IN ('FINORA', 'ORG_EXTERNA')
                )
                INSERT INTO recursos_protegidos (id, id_organizacion, codigo, nombre)
                SELECT
                    datos.id_recurso::uuid,
                    organizaciones_base.id,
                    'TRANSFERENCIA',
                    'Transferencia financiera'
                FROM organizaciones_base
                JOIN (
                    VALUES
                        ('FINORA', '20000000-0000-0000-0000-000000000001'),
                        ('ORG_EXTERNA', '20000000-0000-0000-0000-000000000002')
                ) AS datos(codigo_organizacion, id_recurso)
                    ON datos.codigo_organizacion = organizaciones_base.codigo
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql("""
                WITH recursos_base AS (
                    SELECT
                        recursos_protegidos.id,
                        organizaciones.codigo AS codigo_organizacion
                    FROM recursos_protegidos
                    JOIN organizaciones
                        ON organizaciones.id = recursos_protegidos.id_organizacion
                    WHERE
                        organizaciones.codigo IN ('FINORA', 'ORG_EXTERNA')
                        AND recursos_protegidos.codigo = 'TRANSFERENCIA'
                )
                INSERT INTO operaciones (id, id_recurso_protegido, codigo, nombre)
                SELECT
                    datos.id_operacion::uuid,
                    recursos_base.id,
                    'APROBAR',
                    'Aprobar transferencia'
                FROM recursos_base
                JOIN (
                    VALUES
                        ('FINORA', '30000000-0000-0000-0000-000000000001'),
                        ('ORG_EXTERNA', '30000000-0000-0000-0000-000000000002')
                ) AS datos(codigo_organizacion, id_operacion)
                    ON datos.codigo_organizacion = recursos_base.codigo_organizacion
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql("""
                WITH operaciones_base AS (
                    SELECT
                        operaciones.id,
                        organizaciones.codigo AS codigo_organizacion
                    FROM operaciones
                    JOIN recursos_protegidos
                        ON recursos_protegidos.id = operaciones.id_recurso_protegido
                    JOIN organizaciones
                        ON organizaciones.id = recursos_protegidos.id_organizacion
                    WHERE
                        organizaciones.codigo IN ('FINORA', 'ORG_EXTERNA')
                        AND recursos_protegidos.codigo = 'TRANSFERENCIA'
                        AND operaciones.codigo = 'APROBAR'
                )
                INSERT INTO reglas_autorizacion (id, id_operacion, codigo, nombre)
                SELECT
                    reglas.id_regla::uuid,
                    operaciones_base.id,
                    reglas.codigo_regla,
                    reglas.nombre
                FROM operaciones_base
                JOIN (
                    VALUES
                        ('FINORA', '40000000-0000-0000-0000-000000000001', 'VALIDAR_AISLAMIENTO_TENANT', 'Validar aislamiento entre tenants'),
                        ('FINORA', '40000000-0000-0000-0000-000000000002', 'BLOQUEAR_RIESGO_CRITICO', 'Bloquear sesion con riesgo critico'),
                        ('FINORA', '40000000-0000-0000-0000-000000000003', 'MONTO_SENSIBLE', 'Monto sensible requiere validacion adicional'),
                        ('FINORA', '40000000-0000-0000-0000-000000000004', 'CONTEXTO_SOSPECHOSO', 'Contexto sospechoso requiere validacion adicional'),
                        ('FINORA', '40000000-0000-0000-0000-000000000005', 'PERMITIR_TRANSFERENCIA_NORMAL', 'Permitir transferencia normal'),
                        ('ORG_EXTERNA', '40000000-0000-0000-0000-000000000101', 'VALIDAR_AISLAMIENTO_TENANT', 'Validar aislamiento entre tenants'),
                        ('ORG_EXTERNA', '40000000-0000-0000-0000-000000000102', 'BLOQUEAR_RIESGO_CRITICO', 'Bloquear sesion con riesgo critico'),
                        ('ORG_EXTERNA', '40000000-0000-0000-0000-000000000103', 'MONTO_SENSIBLE', 'Monto sensible requiere validacion adicional'),
                        ('ORG_EXTERNA', '40000000-0000-0000-0000-000000000104', 'CONTEXTO_SOSPECHOSO', 'Contexto sospechoso requiere validacion adicional'),
                        ('ORG_EXTERNA', '40000000-0000-0000-0000-000000000105', 'PERMITIR_TRANSFERENCIA_NORMAL', 'Permitir transferencia normal')
                ) AS reglas(codigo_organizacion, id_regla, codigo_regla, nombre)
                    ON reglas.codigo_organizacion = operaciones_base.codigo_organizacion
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM reglas_autorizacion
                WHERE id IN (
                    '40000000-0000-0000-0000-000000000001',
                    '40000000-0000-0000-0000-000000000002',
                    '40000000-0000-0000-0000-000000000003',
                    '40000000-0000-0000-0000-000000000004',
                    '40000000-0000-0000-0000-000000000005',
                    '40000000-0000-0000-0000-000000000101',
                    '40000000-0000-0000-0000-000000000102',
                    '40000000-0000-0000-0000-000000000103',
                    '40000000-0000-0000-0000-000000000104',
                    '40000000-0000-0000-0000-000000000105'
                );
                """);

            migrationBuilder.Sql("""
                DELETE FROM operaciones
                WHERE id IN (
                    '30000000-0000-0000-0000-000000000001',
                    '30000000-0000-0000-0000-000000000002'
                );
                """);

            migrationBuilder.Sql("""
                DELETE FROM recursos_protegidos
                WHERE id IN (
                    '20000000-0000-0000-0000-000000000001',
                    '20000000-0000-0000-0000-000000000002'
                );
                """);

            migrationBuilder.Sql("""
                DELETE FROM organizaciones
                WHERE id IN (
                    '10000000-0000-0000-0000-000000000001',
                    '10000000-0000-0000-0000-000000000002'
                );
                """);
        }
    }
}
