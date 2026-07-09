using Atlas.PARS.Api.Modelos.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.PARS.Api.Datos.Configuraciones;

public class ConfiguracionReglaAutorizacion : IEntityTypeConfiguration<ReglaAutorizacion>
{
    public void Configure(EntityTypeBuilder<ReglaAutorizacion> constructor)
    {
        constructor.ToTable("reglas_autorizacion", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_reglas_autorizacion_codigo_formato",
                "codigo ~ '^[A-Z0-9_]+$'");
        });

        constructor.HasKey(regla => regla.Id)
            .HasName("pk_reglas_autorizacion");

        constructor.Property(regla => regla.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        constructor.Property(regla => regla.IdOperacion)
            .HasColumnName("id_operacion")
            .HasColumnType("uuid")
            .IsRequired();

        constructor.Property(regla => regla.Codigo)
            .HasColumnName("codigo")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120)
            .IsRequired();

        constructor.Property(regla => regla.Nombre)
            .HasColumnName("nombre")
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        constructor.HasIndex(regla => new { regla.IdOperacion, regla.Codigo })
            .IsUnique()
            .HasDatabaseName("ux_reglas_autorizacion_id_operacion_codigo");

        constructor.HasOne(regla => regla.Operacion)
            .WithMany(operacion => operacion.ReglasAutorizacion)
            .HasForeignKey(regla => regla.IdOperacion)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_reglas_autorizacion_operaciones_id_operacion");
    }
}
