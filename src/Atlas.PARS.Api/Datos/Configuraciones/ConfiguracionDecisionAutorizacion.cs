using Atlas.PARS.Api.Modelos.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.PARS.Api.Datos.Configuraciones;

public class ConfiguracionDecisionAutorizacion
    : IEntityTypeConfiguration<DecisionAutorizacion>
{
    public void Configure(EntityTypeBuilder<DecisionAutorizacion> constructor)
    {
        constructor.ToTable("decisiones_autorizacion", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_decisiones_autorizacion_codigo_organizacion_formato",
                "codigo_organizacion ~ '^[A-Z0-9_]+$'");
            tabla.HasCheckConstraint(
                "ck_decisiones_autorizacion_decision_valor",
                "decision IN ('PERMIT', 'DENY', 'CHALLENGE')");
            tabla.HasCheckConstraint(
                "ck_decisiones_autorizacion_actor_objeto",
                "jsonb_typeof(actor) = 'object'");
            tabla.HasCheckConstraint(
                "ck_decisiones_autorizacion_recurso_objeto",
                "jsonb_typeof(recurso) = 'object'");
            tabla.HasCheckConstraint(
                "ck_decisiones_autorizacion_contexto_objeto",
                "jsonb_typeof(contexto) = 'object'");
            tabla.HasCheckConstraint(
                "ck_decisiones_autorizacion_solicitud_hash_formato",
                "solicitud_hash ~ '^[a-f0-9]{64}$'");
            tabla.HasCheckConstraint(
                "ck_decisiones_autorizacion_firma_consistente",
                "(firma IS NULL AND key_id_firma IS NULL AND algoritmo_firma IS NULL) " +
                "OR (firma IS NOT NULL AND key_id_firma IS NOT NULL AND algoritmo_firma IS NOT NULL)");
        });

        constructor.HasKey(decision => decision.Id)
            .HasName("pk_decisiones_autorizacion");

        constructor.Property(decision => decision.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        constructor.Property(decision => decision.CodigoOrganizacion)
            .HasColumnName("codigo_organizacion")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        constructor.Property(decision => decision.IdOrganizacion)
            .HasColumnName("id_organizacion")
            .HasColumnType("uuid");

        constructor.Property(decision => decision.IdRecursoProtegido)
            .HasColumnName("id_recurso_protegido")
            .HasColumnType("uuid");

        constructor.Property(decision => decision.IdOperacion)
            .HasColumnName("id_operacion")
            .HasColumnType("uuid");

        constructor.Property(decision => decision.IdReglaAutorizacion)
            .HasColumnName("id_regla_autorizacion")
            .HasColumnType("uuid");

        constructor.Property(decision => decision.IdVersionRegla)
            .HasColumnName("id_version_regla")
            .HasColumnType("uuid");

        constructor.Property(decision => decision.CodigoRecursoSolicitado)
            .HasColumnName("codigo_recurso_solicitado")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        constructor.Property(decision => decision.CodigoOperacionSolicitada)
            .HasColumnName("codigo_operacion_solicitada")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        constructor.Property(decision => decision.Decision)
            .HasColumnName("decision")
            .HasColumnType("varchar(20)")
            .HasMaxLength(20)
            .IsRequired();

        constructor.Property(decision => decision.Motivo)
            .HasColumnName("motivo")
            .HasColumnType("varchar(500)")
            .HasMaxLength(500)
            .IsRequired();

        constructor.Property(decision => decision.CodigoRegla)
            .HasColumnName("codigo_regla")
            .HasColumnType("varchar(120)")
            .HasMaxLength(120);

        constructor.Property(decision => decision.NumeroVersionRegla)
            .HasColumnName("numero_version_regla")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        constructor.Property(decision => decision.Actor)
            .HasColumnName("actor")
            .HasColumnType("jsonb")
            .IsRequired();

        constructor.Property(decision => decision.Recurso)
            .HasColumnName("recurso")
            .HasColumnType("jsonb")
            .IsRequired();

        constructor.Property(decision => decision.Contexto)
            .HasColumnName("contexto")
            .HasColumnType("jsonb")
            .IsRequired();

        constructor.Property(decision => decision.AlgoritmoHash)
            .HasColumnName("algoritmo_hash")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .HasDefaultValue("SHA-256")
            .IsRequired();

        constructor.Property(decision => decision.SolicitudHash)
            .HasColumnName("solicitud_hash")
            .HasColumnType("varchar(64)")
            .HasMaxLength(64)
            .IsRequired();

        constructor.Property(decision => decision.CorrelationId)
            .HasColumnName("correlation_id")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        constructor.Property(decision => decision.FechaDecision)
            .HasColumnName("fecha_decision")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        constructor.Property(decision => decision.KeyIdFirma)
            .HasColumnName("key_id_firma")
            .HasColumnType("varchar(60)")
            .HasMaxLength(60);

        constructor.Property(decision => decision.AlgoritmoFirma)
            .HasColumnName("algoritmo_firma")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30);

        constructor.Property(decision => decision.Firma)
            .HasColumnName("firma")
            .HasColumnType("varchar(64)")
            .HasMaxLength(64);

        constructor.HasIndex(decision => decision.FechaDecision)
            .IsDescending()
            .HasDatabaseName("ix_decisiones_autorizacion_fecha_decision");

        constructor.HasIndex(decision => new
            {
                decision.CodigoOrganizacion,
                decision.FechaDecision
            })
            .IsDescending(false, true)
            .HasDatabaseName("ix_decisiones_autorizacion_codigo_organizacion_fecha_decision");

        constructor.HasIndex(decision => new
            {
                decision.Decision,
                decision.FechaDecision
            })
            .IsDescending(false, true)
            .HasDatabaseName("ix_decisiones_autorizacion_decision_fecha_decision");

        constructor.HasIndex(decision => new
            {
                decision.CodigoRegla,
                decision.FechaDecision
            })
            .IsDescending(false, true)
            .HasDatabaseName("ix_decisiones_autorizacion_codigo_regla_fecha_decision");

        constructor.HasIndex(decision => decision.CorrelationId)
            .HasDatabaseName("ix_decisiones_autorizacion_correlation_id");

        constructor.HasIndex(decision => decision.SolicitudHash)
            .HasDatabaseName("ix_decisiones_autorizacion_solicitud_hash");

        constructor.HasIndex(decision => decision.IdOrganizacion)
            .HasDatabaseName("ix_decisiones_autorizacion_id_organizacion");

        constructor.HasIndex(decision => decision.IdRecursoProtegido)
            .HasDatabaseName("ix_decisiones_autorizacion_id_recurso_protegido");

        constructor.HasIndex(decision => decision.IdOperacion)
            .HasDatabaseName("ix_decisiones_autorizacion_id_operacion");

        constructor.HasIndex(decision => decision.IdReglaAutorizacion)
            .HasDatabaseName("ix_decisiones_autorizacion_id_regla_autorizacion");

        constructor.HasIndex(decision => new
            {
                decision.IdVersionRegla,
                decision.FechaDecision
            })
            .IsDescending(false, true)
            .HasDatabaseName("ix_decisiones_autorizacion_id_version_regla_fecha_decision");

        constructor.HasOne(decision => decision.Organizacion)
            .WithMany()
            .HasForeignKey(decision => decision.IdOrganizacion)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_decisiones_autorizacion_organizaciones_id_organizacion");

        constructor.HasOne(decision => decision.RecursoProtegido)
            .WithMany()
            .HasForeignKey(decision => decision.IdRecursoProtegido)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_decisiones_autorizacion_recursos_protegidos_id_recurso_protegido");

        constructor.HasOne(decision => decision.Operacion)
            .WithMany()
            .HasForeignKey(decision => decision.IdOperacion)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_decisiones_autorizacion_operaciones_id_operacion");

        constructor.HasOne(decision => decision.ReglaAutorizacion)
            .WithMany()
            .HasForeignKey(decision => decision.IdReglaAutorizacion)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_decisiones_autorizacion_reglas_autorizacion_id_regla_autorizacion");

        constructor.HasOne(decision => decision.VersionRegla)
            .WithMany()
            .HasForeignKey(decision => decision.IdVersionRegla)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_decisiones_autorizacion_versiones_regla_id_version_regla");
    }
}
