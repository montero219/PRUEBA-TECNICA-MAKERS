using Atlas.PARS.Api.Datos;
using Atlas.PARS.Api.Modelos.Entidades;
using Atlas.PARS.Api.Repositorios.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Atlas.PARS.Api.Repositorios;

public sealed class RepositorioOrganizaciones : IRepositorioOrganizaciones
{
    private readonly ContextoAtlas _contexto;

    public RepositorioOrganizaciones(ContextoAtlas contexto)
    {
        _contexto = contexto;
    }

    public Task<Organizacion?> ObtenerPorOrganizacionYCodigoAsync(
        string codigo,
        CancellationToken cancellationToken)
    {
        return _contexto.Organizaciones
            .AsNoTracking()
            .SingleOrDefaultAsync(
                organizacion => organizacion.Codigo == codigo,
                cancellationToken);
    }
}