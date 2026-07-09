using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.PARS.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class CrearModeloInicialEvaluacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            migrationBuilder.CreateTable(
                name: "organizaciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    zona_horaria = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizaciones", x => x.id);
                    table.CheckConstraint("ck_organizaciones_codigo_formato", "codigo ~ '^[A-Z0-9_]+$'");
                });

            migrationBuilder.CreateTable(
                name: "recursos_protegidos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    id_organizacion = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recursos_protegidos", x => x.id);
                    table.CheckConstraint("ck_recursos_protegidos_codigo_formato", "codigo ~ '^[A-Z0-9_]+$'");
                    table.ForeignKey(
                        name: "fk_recursos_protegidos_organizaciones_id_organizacion",
                        column: x => x.id_organizacion,
                        principalTable: "organizaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operaciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    id_recurso_protegido = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operaciones", x => x.id);
                    table.CheckConstraint("ck_operaciones_codigo_formato", "codigo ~ '^[A-Z0-9_]+$'");
                    table.ForeignKey(
                        name: "fk_operaciones_recursos_protegidos_id_recurso_protegido",
                        column: x => x.id_recurso_protegido,
                        principalTable: "recursos_protegidos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reglas_autorizacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    id_operacion = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    nombre = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reglas_autorizacion", x => x.id);
                    table.CheckConstraint("ck_reglas_autorizacion_codigo_formato", "codigo ~ '^[A-Z0-9_]+$'");
                    table.ForeignKey(
                        name: "fk_reglas_autorizacion_operaciones_id_operacion",
                        column: x => x.id_operacion,
                        principalTable: "operaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "versiones_regla",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    id_regla_autorizacion = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_version = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    condiciones = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    decision_si_cumple = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    prioridad = table.Column<int>(type: "integer", nullable: false),
                    vigencia_desde = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    vigencia_hasta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_versiones_regla", x => x.id);
                    table.CheckConstraint("ck_versiones_regla_condiciones_objeto", "jsonb_typeof(condiciones) = 'object'");
                    table.CheckConstraint("ck_versiones_regla_decision_si_cumple_valor", "decision_si_cumple IN ('PERMIT', 'DENY', 'CHALLENGE')");
                    table.CheckConstraint("ck_versiones_regla_numero_version_formato", "numero_version ~ '^[0-9]+\\.[0-9]+\\.[0-9]+$'");
                    table.CheckConstraint("ck_versiones_regla_prioridad_no_negativa", "prioridad >= 0");
                    table.CheckConstraint("ck_versiones_regla_vigencia_rango", "vigencia_hasta IS NULL OR vigencia_hasta > vigencia_desde");
                    table.ForeignKey(
                        name: "fk_versiones_regla_reglas_autorizacion_id_regla_autorizacion",
                        column: x => x.id_regla_autorizacion,
                        principalTable: "reglas_autorizacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_operaciones_id_recurso_protegido_codigo",
                table: "operaciones",
                columns: new[] { "id_recurso_protegido", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_organizaciones_codigo",
                table: "organizaciones",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_recursos_protegidos_id_organizacion_codigo",
                table: "recursos_protegidos",
                columns: new[] { "id_organizacion", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_reglas_autorizacion_id_operacion_codigo",
                table: "reglas_autorizacion",
                columns: new[] { "id_operacion", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_versiones_regla_vigencia",
                table: "versiones_regla",
                columns: new[] { "id_regla_autorizacion", "vigencia_desde" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_versiones_regla_id_regla_autorizacion_numero_version",
                table: "versiones_regla",
                columns: new[] { "id_regla_autorizacion", "numero_version" },
                unique: true);

            migrationBuilder.Sql("""
                ALTER TABLE versiones_regla
                ADD CONSTRAINT ex_versiones_regla_sin_solapamiento
                EXCLUDE USING gist (
                    id_regla_autorizacion WITH =,
                    tstzrange(
                        vigencia_desde,
                        COALESCE(
                            vigencia_hasta,
                            'infinity'::timestamptz
                        ),
                        '[)'
                    ) WITH &&
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE IF EXISTS versiones_regla
                DROP CONSTRAINT IF EXISTS ex_versiones_regla_sin_solapamiento;
                """);

            migrationBuilder.DropTable(
                name: "versiones_regla");

            migrationBuilder.DropTable(
                name: "reglas_autorizacion");

            migrationBuilder.DropTable(
                name: "operaciones");

            migrationBuilder.DropTable(
                name: "recursos_protegidos");

            migrationBuilder.DropTable(
                name: "organizaciones");

            migrationBuilder.Sql("DROP EXTENSION IF EXISTS btree_gist;");
        }
    }
}
