using Atlas.PARS.Api.Datos;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.PARS.Api.Datos.Migraciones
{
    /// <inheritdoc />
    [DbContext(typeof(ContextoAtlas))]
    [Migration("20260709180000_AgregarFirmaDecisionesAutorizacion")]
    public partial class AgregarFirmaDecisionesAutorizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "key_id_firma",
                table: "decisiones_autorizacion",
                type: "varchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "algoritmo_firma",
                table: "decisiones_autorizacion",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "firma",
                table: "decisiones_autorizacion",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_decisiones_autorizacion_firma_consistente",
                table: "decisiones_autorizacion",
                sql: "(firma IS NULL AND key_id_firma IS NULL AND algoritmo_firma IS NULL) " +
                    "OR (firma IS NOT NULL AND key_id_firma IS NOT NULL AND algoritmo_firma IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_decisiones_autorizacion_firma_consistente",
                table: "decisiones_autorizacion");

            migrationBuilder.DropColumn(
                name: "firma",
                table: "decisiones_autorizacion");

            migrationBuilder.DropColumn(
                name: "algoritmo_firma",
                table: "decisiones_autorizacion");

            migrationBuilder.DropColumn(
                name: "key_id_firma",
                table: "decisiones_autorizacion");
        }
    }
}
