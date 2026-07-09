using Atlas.PARS.Api.Modelos.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.PARS.Api.Datos.Configuraciones;

public class ConfiguracionOrganizacion : IEntityTypeConfiguration<Organizacion>
{
    public void Configure(EntityTypeBuilder<Organizacion> constructor)
    {
        constructor.ToTable("organizaciones", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_organizaciones_codigo_formato",
                "codigo ~ '^[A-Z0-9_]+$'");
        });

        constructor.HasKey(organizacion => organizacion.Id)
            .HasName("pk_organizaciones");

        constructor.Property(organizacion => organizacion.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        constructor.Property(organizacion => organizacion.Codigo)
            .HasColumnName("codigo")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        constructor.Property(organizacion => organizacion.Nombre)
            .HasColumnName("nombre")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        constructor.Property(organizacion => organizacion.ZonaHoraria)
            .HasColumnName("zona_horaria")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        constructor.HasIndex(organizacion => organizacion.Codigo)
            .IsUnique()
            .HasDatabaseName("ux_organizaciones_codigo");
    }
}
