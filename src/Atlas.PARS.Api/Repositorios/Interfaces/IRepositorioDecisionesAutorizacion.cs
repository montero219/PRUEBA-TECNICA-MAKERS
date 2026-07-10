using Atlas.PARS.Api.Modelos.Entidades;

namespace Atlas.PARS.Api.Repositorios.Interfaces;

public interface IRepositorioDecisionesAutorizacion
{
    Task RegistrarAsync(
        DecisionAutorizacion decision,
        CancellationToken cancellationToken);
}
