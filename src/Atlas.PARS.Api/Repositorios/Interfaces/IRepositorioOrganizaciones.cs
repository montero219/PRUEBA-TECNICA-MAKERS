using Atlas.PARS.Api.Modelos.Entidades;

namespace Atlas.PARS.Api.Repositorios.Interfaces;

public interface IRepositorioOrganizaciones
{
    Task<Organizacion?> ObtenerPorOrganizacionYCodigoAsync(
        string codigo,
        CancellationToken cancellationToken);
}