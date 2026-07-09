using Atlas.PARS.Api.Datos;
using Atlas.PARS.Api.Modelos.Entidades;
using Atlas.PARS.Api.Repositorios.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Atlas.PARS.Api.Repositorios;

public sealed class RepositorioOperaciones : IRepositorioOperaciones
{
    private readonly ContextoAtlas _contexto;

    public RepositorioOperaciones(ContextoAtlas contexto)
    {
        _contexto = contexto;
    }

    public Task<Operacion?> ObtenerPorRecursoYCodigoAsync(
        Guid idRecursoProtegido,
        string codigo,
        CancellationToken cancellationToken)
    {
        return _contexto.Operaciones
            .AsNoTracking()
            .SingleOrDefaultAsync(
                operacion =>
                    operacion.IdRecursoProtegido == idRecursoProtegido &&
                    operacion.Codigo == codigo,
                cancellationToken);
    }
}
