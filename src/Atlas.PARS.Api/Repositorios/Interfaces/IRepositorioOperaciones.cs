using Atlas.PARS.Api.Modelos.Entidades;

namespace Atlas.PARS.Api.Repositorios.Interfaces;

public interface IRepositorioOperaciones
{
    Task<Operacion?> ObtenerPorRecursoYCodigoAsync(
        Guid idRecursoProtegido,
        string codigo,
        CancellationToken cancellationToken);
}
