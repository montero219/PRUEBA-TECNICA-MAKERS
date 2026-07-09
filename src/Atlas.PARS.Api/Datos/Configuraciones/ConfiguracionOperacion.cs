using Atlas.PARS.Api.Modelos.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.PARS.Api.Datos.Configuraciones;

public class ConfiguracionOperacion : IEntityTypeConfiguration<Operacion>
{
    public void Configure(EntityTypeBuilder<Operacion> constructor)
    {
        constructor.ToTable("operaciones", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_operaciones_codigo_formato",
                "codigo ~ '^[A-Z0-9_]+$'");
        });

        constructor.HasKey(operacion => operacion.Id)
            .HasName("pk_operaciones");

        constructor.Property(operacion => operacion.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        constructor.Property(operacion => operacion.IdRecursoProtegido)
            .HasColumnName("id_recurso_protegido")
            .HasColumnType("uuid")
            .IsRequired();

        constructor.Property(operacion => operacion.Codigo)
            .HasColumnName("codigo")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        constructor.Property(operacion => operacion.Nombre)
            .HasColumnName("nombre")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        constructor.HasIndex(operacion => new { operacion.IdRecursoProtegido, operacion.Codigo })
            .IsUnique()
            .HasDatabaseName("ux_operaciones_id_recurso_protegido_codigo");

        constructor.HasOne(operacion => operacion.RecursoProtegido)
            .WithMany(recurso => recurso.Operaciones)
            .HasForeignKey(operacion => operacion.IdRecursoProtegido)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_operaciones_recursos_protegidos_id_recurso_protegido");
    }
}
