using Atlas.PARS.Api.Datos;
using Atlas.PARS.Api.Modelos.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Atlas.PARS.PruebasUnitarias.Datos;

public class ModeloDecisionAutorizacionPruebas
{
    [Fact]
    public void ContextoAtlas_ConfiguraDecisionesAutorizacionComoAuditoriaDeDecisiones()
    {
        var opciones = new DbContextOptionsBuilder<ContextoAtlas>()
            .UseNpgsql("Host=localhost;Database=atlas_pars;Username=atlas_pars;Password=atlas_pars")
            .Options;
        using var contexto = new ContextoAtlas(opciones);

        var entidad = contexto.Model.FindEntityType(typeof(DecisionAutorizacion));

        Assert.NotNull(entidad);
        Assert.Equal("decisiones_autorizacion", entidad.GetTableName());

        var tabla = StoreObjectIdentifier.Table(
            entidad.GetTableName()!,
            entidad.GetSchema());

        AssertColumna(entidad, tabla, nameof(DecisionAutorizacion.CodigoOrganizacion), "codigo_organizacion", "varchar(100)");
        AssertColumna(entidad, tabla, nameof(DecisionAutorizacion.Decision), "decision", "varchar(20)");
        AssertColumna(entidad, tabla, nameof(DecisionAutorizacion.Motivo), "motivo", "varchar(500)");
        AssertColumna(entidad, tabla, nameof(DecisionAutorizacion.Actor), "actor", "jsonb");
        AssertColumna(entidad, tabla, nameof(DecisionAutorizacion.Recurso), "recurso", "jsonb");
        AssertColumna(entidad, tabla, nameof(DecisionAutorizacion.Contexto), "contexto", "jsonb");
        AssertColumna(entidad, tabla, nameof(DecisionAutorizacion.SolicitudHash), "solicitud_hash", "varchar(64)");
        AssertColumna(entidad, tabla, nameof(DecisionAutorizacion.CorrelationId), "correlation_id", "varchar(100)");
        AssertColumna(entidad, tabla, nameof(DecisionAutorizacion.FechaDecision), "fecha_decision", "timestamp with time zone");

        Assert.Contains(entidad.GetIndexes(), indice =>
            indice.GetDatabaseName() == "ix_decisiones_autorizacion_codigo_organizacion_fecha_decision");
        Assert.Contains(entidad.GetIndexes(), indice =>
            indice.GetDatabaseName() == "ix_decisiones_autorizacion_correlation_id");
    }

    private static void AssertColumna(
        IEntityType entidad,
        StoreObjectIdentifier tabla,
        string propiedad,
        string columna,
        string tipo)
    {
        var propiedadModelo = entidad.FindProperty(propiedad);

        Assert.NotNull(propiedadModelo);
        Assert.Equal(columna, propiedadModelo.GetColumnName(tabla));
        Assert.Equal(tipo, propiedadModelo.GetColumnType());
    }
}
