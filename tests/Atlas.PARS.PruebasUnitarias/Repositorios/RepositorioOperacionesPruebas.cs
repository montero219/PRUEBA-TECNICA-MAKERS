using Atlas.PARS.Api.Datos;
using Atlas.PARS.Api.Modelos.Entidades;
using Atlas.PARS.Api.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace Atlas.PARS.PruebasUnitarias.Repositorios;

public class RepositorioOperacionesPruebas
{
    [Fact]
    public async Task ObtenerPorRecursoYCodigoAsync_BuscaOperacionAcotadaAlRecursoProtegido()
    {
        var idRecursoObjetivo = Guid.NewGuid();
        var idOtroRecurso = Guid.NewGuid();
        var operacionEsperada = new Operacion
        {
            Id = Guid.NewGuid(),
            IdRecursoProtegido = idRecursoObjetivo,
            Codigo = "APROBAR",
            Nombre = "Aprobar transferencia"
        };

        await using var contexto = CrearContexto();
        contexto.Operaciones.AddRange(
            operacionEsperada,
            new Operacion
            {
                Id = Guid.NewGuid(),
                IdRecursoProtegido = idOtroRecurso,
                Codigo = "APROBAR",
                Nombre = "Aprobar cupo"
            });
        await contexto.SaveChangesAsync();

        var repositorio = new RepositorioOperaciones(contexto);

        var operacion = await repositorio.ObtenerPorRecursoYCodigoAsync(
            idRecursoObjetivo,
            "APROBAR",
            CancellationToken.None);

        Assert.NotNull(operacion);
        Assert.Equal(operacionEsperada.Id, operacion.Id);
        Assert.Equal(idRecursoObjetivo, operacion.IdRecursoProtegido);
    }

    private static ContextoAtlas CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<ContextoAtlas>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ContextoAtlasOperacionesPruebas(opciones);
    }

    private sealed class ContextoAtlasOperacionesPruebas : ContextoAtlas
    {
        public ContextoAtlasOperacionesPruebas(DbContextOptions<ContextoAtlas> opciones)
            : base(opciones)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<Organizacion>();
            modelBuilder.Ignore<RecursoProtegido>();
            modelBuilder.Ignore<ReglaAutorizacion>();
            modelBuilder.Ignore<VersionRegla>();

            modelBuilder.Entity<Operacion>(constructor =>
            {
                constructor.HasKey(operacion => operacion.Id);
                constructor.Ignore(operacion => operacion.RecursoProtegido);
                constructor.Ignore(operacion => operacion.ReglasAutorizacion);
            });
        }
    }
}
