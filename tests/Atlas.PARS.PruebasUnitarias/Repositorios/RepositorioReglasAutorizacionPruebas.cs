using System.Text.Json;
using Atlas.PARS.Api.Datos;
using Atlas.PARS.Api.Modelos.Entidades;
using Atlas.PARS.Api.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Atlas.PARS.PruebasUnitarias.Repositorios;

public class RepositorioReglasAutorizacionPruebas
{
    [Fact]
    public async Task ObtenerVigentesPorOperacionAsync_RetornaVersionesVigentesOrdenadasPorPrioridad()
    {
        var fechaEvaluacion = new DateTimeOffset(2026, 7, 9, 15, 0, 0, TimeSpan.Zero);
        var idOperacionObjetivo = Guid.NewGuid();
        var idOtraOperacion = Guid.NewGuid();
        var reglaPrioridadBaja = CrearRegla(
            idOperacionObjetivo,
            "PERMITIR_TRANSFERENCIA_NORMAL",
            "Permitir transferencia normal");
        var reglaPrioridadAlta = CrearRegla(
            idOperacionObjetivo,
            "BLOQUEAR_RIESGO_CRITICO",
            "Bloquear sesion con riesgo critico");
        var reglaExpirada = CrearRegla(
            idOperacionObjetivo,
            "MONTO_SENSIBLE",
            "Monto sensible requiere validacion adicional");
        var reglaFutura = CrearRegla(
            idOperacionObjetivo,
            "CONTEXTO_SOSPECHOSO",
            "Contexto sospechoso requiere validacion adicional");
        var reglaOtraOperacion = CrearRegla(
            idOtraOperacion,
            "BLOQUEAR_RIESGO_CRITICO",
            "Bloquear sesion con riesgo critico");

        await using var contexto = CrearContexto();
        contexto.ReglasAutorizacion.AddRange(
            reglaPrioridadBaja,
            reglaPrioridadAlta,
            reglaExpirada,
            reglaFutura,
            reglaOtraOperacion);
        contexto.VersionesRegla.AddRange(
            CrearVersion(reglaPrioridadBaja, "PERMIT", 40, fechaEvaluacion.AddDays(-5), null),
            CrearVersion(reglaPrioridadAlta, "DENY", 10, fechaEvaluacion.AddDays(-5), null),
            CrearVersion(reglaExpirada, "CHALLENGE", 20, fechaEvaluacion.AddDays(-10), fechaEvaluacion.AddDays(-1)),
            CrearVersion(reglaFutura, "CHALLENGE", 30, fechaEvaluacion.AddDays(1), null),
            CrearVersion(reglaOtraOperacion, "DENY", 5, fechaEvaluacion.AddDays(-5), null));
        await contexto.SaveChangesAsync();
        var repositorio = new RepositorioReglasAutorizacion(contexto);

        var reglas = await repositorio.ObtenerVigentesPorOperacionAsync(
            idOperacionObjetivo,
            fechaEvaluacion,
            CancellationToken.None);

        Assert.Collection(
            reglas,
            regla =>
            {
                Assert.Equal("BLOQUEAR_RIESGO_CRITICO", regla.CodigoRegla);
                Assert.Equal("DENY", regla.DecisionSiCumple);
                Assert.Equal(10, regla.Prioridad);
            },
            regla =>
            {
                Assert.Equal("PERMITIR_TRANSFERENCIA_NORMAL", regla.CodigoRegla);
                Assert.Equal("PERMIT", regla.DecisionSiCumple);
                Assert.Equal(40, regla.Prioridad);
            });
    }

    private static ContextoAtlas CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<ContextoAtlas>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ContextoAtlasReglasPruebas(opciones);
    }

    private static ReglaAutorizacion CrearRegla(
        Guid idOperacion,
        string codigo,
        string nombre)
    {
        return new ReglaAutorizacion
        {
            Id = Guid.NewGuid(),
            IdOperacion = idOperacion,
            Codigo = codigo,
            Nombre = nombre
        };
    }

    private static VersionRegla CrearVersion(
        ReglaAutorizacion regla,
        string decisionSiCumple,
        int prioridad,
        DateTimeOffset vigenciaDesde,
        DateTimeOffset? vigenciaHasta)
    {
        return new VersionRegla
        {
            Id = Guid.NewGuid(),
            IdReglaAutorizacion = regla.Id,
            ReglaAutorizacion = regla,
            NumeroVersion = "1.0.0",
            Condiciones = JsonDocument.Parse("{}"),
            DecisionSiCumple = decisionSiCumple,
            Prioridad = prioridad,
            VigenciaDesde = vigenciaDesde,
            VigenciaHasta = vigenciaHasta,
            FechaCreacion = vigenciaDesde
        };
    }

    private sealed class ContextoAtlasReglasPruebas : ContextoAtlas
    {
        public ContextoAtlasReglasPruebas(DbContextOptions<ContextoAtlas> opciones)
            : base(opciones)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<Organizacion>();
            modelBuilder.Ignore<RecursoProtegido>();
            modelBuilder.Ignore<Operacion>();
            modelBuilder.Ignore<DecisionAutorizacion>();

            modelBuilder.Entity<ReglaAutorizacion>(constructor =>
            {
                constructor.HasKey(regla => regla.Id);
                constructor.Ignore(regla => regla.Operacion);
            });

            modelBuilder.Entity<VersionRegla>(constructor =>
            {
                constructor.HasKey(version => version.Id);
                constructor.Property(version => version.Condiciones)
                    .HasConversion(
                        documento => documento.RootElement.GetRawText(),
                        json => JsonDocument.Parse(json, default(JsonDocumentOptions)));
                constructor.HasOne(version => version.ReglaAutorizacion)
                    .WithMany(regla => regla.VersionesRegla)
                    .HasForeignKey(version => version.IdReglaAutorizacion);
            });
        }
    }
}
