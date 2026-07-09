using Atlas.PARS.Api.Modelos.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.PARS.Api.Datos.Configuraciones;

public class ConfiguracionVersionRegla : IEntityTypeConfiguration<VersionRegla>
{
    public void Configure(EntityTypeBuilder<VersionRegla> constructor)
    {
        constructor.ToTable("versiones_regla", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_versiones_regla_numero_version_formato",
                "numero_version ~ '^[0-9]+\\.[0-9]+\\.[0-9]+$'");
            tabla.HasCheckConstraint(
                "ck_versiones_regla_condiciones_objeto",
                "jsonb_typeof(condiciones) = 'object'");
            tabla.HasCheckConstraint(
                "ck_versiones_regla_decision_si_cumple_valor",
                "decision_si_cumple IN ('PERMIT', 'DENY', 'CHALLENGE')");
            tabla.HasCheckConstraint(
                "ck_versiones_regla_prioridad_no_negativa",
                "prioridad >= 0");
            tabla.HasCheckConstraint(
                "ck_versiones_regla_vigencia_rango",
                "vigencia_hasta IS NULL OR vigencia_hasta > vigencia_desde");
        });

        constructor.HasKey(version => version.Id)
            .HasName("pk_versiones_regla");

        constructor.Property(version => version.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        constructor.Property(version => version.IdReglaAutorizacion)
            .HasColumnName("id_regla_autorizacion")
            .HasColumnType("uuid")
            .IsRequired();

        constructor.Property(version => version.NumeroVersion)
            .HasColumnName("numero_version")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();

        constructor.Property(version => version.Condiciones)
            .HasColumnName("condiciones")
            .HasColumnType("jsonb")
            .IsRequired();

        constructor.Property(version => version.DecisionSiCumple)
            .HasColumnName("decision_si_cumple")
            .HasColumnType("varchar(20)")
            .HasMaxLength(20)
            .IsRequired();

        constructor.Property(version => version.Prioridad)
            .HasColumnName("prioridad")
            .HasColumnType("integer")
            .IsRequired();

        constructor.Property(version => version.VigenciaDesde)
            .HasColumnName("vigencia_desde")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        constructor.Property(version => version.VigenciaHasta)
            .HasColumnName("vigencia_hasta")
            .HasColumnType("timestamp with time zone");

        constructor.Property(version => version.FechaCreacion)
            .HasColumnName("fecha_creacion")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        constructor.HasIndex(version => new { version.IdReglaAutorizacion, version.NumeroVersion })
            .IsUnique()
            .HasDatabaseName("ux_versiones_regla_id_regla_autorizacion_numero_version");

        constructor.HasIndex(version => new { version.IdReglaAutorizacion, version.VigenciaDesde })
            .IsDescending(false, true)
            .HasDatabaseName("ix_versiones_regla_vigencia");

        constructor.HasOne(version => version.ReglaAutorizacion)
            .WithMany(regla => regla.VersionesRegla)
            .HasForeignKey(version => version.IdReglaAutorizacion)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_versiones_regla_reglas_autorizacion_id_regla_autorizacion");
    }
}
