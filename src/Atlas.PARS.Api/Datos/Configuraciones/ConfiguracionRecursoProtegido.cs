using Atlas.PARS.Api.Modelos.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.PARS.Api.Datos.Configuraciones;

public class ConfiguracionRecursoProtegido : IEntityTypeConfiguration<RecursoProtegido>
{
    public void Configure(EntityTypeBuilder<RecursoProtegido> constructor)
    {
        constructor.ToTable("recursos_protegidos", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_recursos_protegidos_codigo_formato",
                "codigo ~ '^[A-Z0-9_]+$'");
        });

        constructor.HasKey(recurso => recurso.Id)
            .HasName("pk_recursos_protegidos");

        constructor.Property(recurso => recurso.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        constructor.Property(recurso => recurso.IdOrganizacion)
            .HasColumnName("id_organizacion")
            .HasColumnType("uuid")
            .IsRequired();

        constructor.Property(recurso => recurso.Codigo)
            .HasColumnName("codigo")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        constructor.Property(recurso => recurso.Nombre)
            .HasColumnName("nombre")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        constructor.HasIndex(recurso => new { recurso.IdOrganizacion, recurso.Codigo })
            .IsUnique()
            .HasDatabaseName("ux_recursos_protegidos_id_organizacion_codigo");

        constructor.HasOne(recurso => recurso.Organizacion)
            .WithMany(organizacion => organizacion.RecursosProtegidos)
            .HasForeignKey(recurso => recurso.IdOrganizacion)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_recursos_protegidos_organizaciones_id_organizacion");
    }
}
