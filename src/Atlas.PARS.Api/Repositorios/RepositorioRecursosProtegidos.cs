using Atlas.PARS.Api.Datos;
using Atlas.PARS.Api.Modelos.Entidades;
using Atlas.PARS.Api.Repositorios.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Atlas.PARS.Api.Repositorios;

public sealed class RepositorioRecursosProtegidos
    : IRepositorioRecursosProtegidos
{
    private readonly ContextoAtlas _contexto;

    public RepositorioRecursosProtegidos(
        ContextoAtlas contexto)
    {
        _contexto = contexto;
    }

    public Task<RecursoProtegido?> ObtenerPorOrganizacionYCodigoAsync(
        Guid idOrganizacion,
        string codigo,
        CancellationToken cancellationToken)
    {
        return _contexto.RecursosProtegidos
            .AsNoTracking()
            .SingleOrDefaultAsync(
                recurso =>
                    recurso.IdOrganizacion == idOrganizacion &&
                    recurso.Codigo == codigo,
                cancellationToken);
    }
}