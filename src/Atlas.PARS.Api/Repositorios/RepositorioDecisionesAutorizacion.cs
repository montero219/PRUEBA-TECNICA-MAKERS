using Atlas.PARS.Api.Datos;
using Atlas.PARS.Api.Modelos.Entidades;
using Atlas.PARS.Api.Repositorios.Interfaces;

namespace Atlas.PARS.Api.Repositorios;

public sealed class RepositorioDecisionesAutorizacion
    : IRepositorioDecisionesAutorizacion
{
    private readonly ContextoAtlas _contexto;

    public RepositorioDecisionesAutorizacion(ContextoAtlas contexto)
    {
        _contexto = contexto;
    }

    public async Task RegistrarAsync(
        DecisionAutorizacion decision,
        CancellationToken cancellationToken)
    {
        _contexto.DecisionesAutorizacion.Add(decision);

        await _contexto.SaveChangesAsync(cancellationToken);
    }
}
