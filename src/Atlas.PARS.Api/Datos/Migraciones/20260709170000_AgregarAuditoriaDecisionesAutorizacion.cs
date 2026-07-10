using System;
using System.Text.Json;
using Atlas.PARS.Api.Datos;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.PARS.Api.Datos.Migraciones
{
    /// <inheritdoc />
    [DbContext(typeof(ContextoAtlas))]
    [Migration("20260709170000_AgregarAuditoriaDecisionesAutorizacion")]
    public partial class AgregarAuditoriaDecisionesAutorizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "decisiones_autorizacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_organizacion = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    id_organizacion = table.Column<Guid>(type: "uuid", nullable: true),
                    id_recurso_protegido = table.Column<Guid>(type: "uuid", nullable: true),
                    id_operacion = table.Column<Guid>(type: "uuid", nullable: true),
                    id_regla_autorizacion = table.Column<Guid>(type: "uuid", nullable: true),
                    id_version_regla = table.Column<Guid>(type: "uuid", nullable: true),
                    codigo_recurso_solicitado = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    codigo_operacion_solicitada = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    decision = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    motivo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    codigo_regla = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true),
                    numero_version_regla = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true),
                    actor = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    recurso = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    contexto = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    algoritmo_hash = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: "SHA-256"),
                    solicitud_hash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    correlation_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    fecha_decision = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_decisiones_autorizacion", x => x.id);
                    table.CheckConstraint("ck_decisiones_autorizacion_actor_objeto", "jsonb_typeof(actor) = 'object'");
                    table.CheckConstraint("ck_decisiones_autorizacion_codigo_organizacion_formato", "codigo_organizacion ~ '^[A-Z0-9_]+$'");
                    table.CheckConstraint("ck_decisiones_autorizacion_contexto_objeto", "jsonb_typeof(contexto) = 'object'");
                    table.CheckConstraint("ck_decisiones_autorizacion_decision_valor", "decision IN ('PERMIT', 'DENY', 'CHALLENGE')");
                    table.CheckConstraint("ck_decisiones_autorizacion_recurso_objeto", "jsonb_typeof(recurso) = 'object'");
                    table.CheckConstraint("ck_decisiones_autorizacion_solicitud_hash_formato", "solicitud_hash ~ '^[a-f0-9]{64}$'");
                    table.ForeignKey(
                        name: "fk_decisiones_autorizacion_operaciones_id_operacion",
                        column: x => x.id_operacion,
                        principalTable: "operaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_decisiones_autorizacion_organizaciones_id_organizacion",
                        column: x => x.id_organizacion,
                        principalTable: "organizaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_decisiones_autorizacion_recursos_protegidos_id_recurso_protegido",
                        column: x => x.id_recurso_protegido,
                        principalTable: "recursos_protegidos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_decisiones_autorizacion_reglas_autorizacion_id_regla_autorizacion",
                        column: x => x.id_regla_autorizacion,
                        principalTable: "reglas_autorizacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_decisiones_autorizacion_versiones_regla_id_version_regla",
                        column: x => x.id_version_regla,
                        principalTable: "versiones_regla",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_decisiones_autorizacion_codigo_organizacion_fecha_decision",
                table: "decisiones_autorizacion",
                columns: new[] { "codigo_organizacion", "fecha_decision" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_decisiones_autorizacion_codigo_regla_fecha_decision",
                table: "decisiones_autorizacion",
                columns: new[] { "codigo_regla", "fecha_decision" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_decisiones_autorizacion_correlation_id",
                table: "decisiones_autorizacion",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_decisiones_autorizacion_decision_fecha_decision",
                table: "decisiones_autorizacion",
                columns: new[] { "decision", "fecha_decision" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_decisiones_autorizacion_fecha_decision",
                table: "decisiones_autorizacion",
                column: "fecha_decision",
                descending: new[] { true });

            migrationBuilder.CreateIndex(
                name: "ix_decisiones_autorizacion_id_operacion",
                table: "decisiones_autorizacion",
                column: "id_operacion");

            migrationBuilder.CreateIndex(
                name: "ix_decisiones_autorizacion_id_organizacion",
                table: "decisiones_autorizacion",
                column: "id_organizacion");

            migrationBuilder.CreateIndex(
                name: "ix_decisiones_autorizacion_id_recurso_protegido",
                table: "decisiones_autorizacion",
                column: "id_recurso_protegido");

            migrationBuilder.CreateIndex(
                name: "ix_decisiones_autorizacion_id_regla_autorizacion",
                table: "decisiones_autorizacion",
                column: "id_regla_autorizacion");

            migrationBuilder.CreateIndex(
                name: "ix_decisiones_autorizacion_id_version_regla_fecha_decision",
                table: "decisiones_autorizacion",
                columns: new[] { "id_version_regla", "fecha_decision" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_decisiones_autorizacion_solicitud_hash",
                table: "decisiones_autorizacion",
                column: "solicitud_hash");

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION bloquear_mutaciones_decisiones_autorizacion()
                RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'decisiones_autorizacion es append-only';
                END;
                $$ LANGUAGE plpgsql;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER tr_decisiones_autorizacion_append_only
                BEFORE UPDATE OR DELETE ON decisiones_autorizacion
                FOR EACH ROW EXECUTE FUNCTION bloquear_mutaciones_decisiones_autorizacion();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS tr_decisiones_autorizacion_append_only
                ON decisiones_autorizacion;
                """);

            migrationBuilder.Sql("""
                DROP FUNCTION IF EXISTS bloquear_mutaciones_decisiones_autorizacion();
                """);

            migrationBuilder.DropTable(
                name: "decisiones_autorizacion");
        }
    }
}
