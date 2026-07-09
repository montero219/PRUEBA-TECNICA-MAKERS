using Atlas.PARS.Api.Modelos.Entidades;

namespace Atlas.PARS.Api.Repositorios.Interfaces;

public interface IRepositorioRecursosProtegidos
{
    Task<RecursoProtegido?> ObtenerPorOrganizacionYCodigoAsync(
        Guid idOrganizacion,
        string codigo,
        CancellationToken cancellationToken);
}