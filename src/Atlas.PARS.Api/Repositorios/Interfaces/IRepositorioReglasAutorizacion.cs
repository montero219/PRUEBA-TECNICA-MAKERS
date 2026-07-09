using Atlas.PARS.Api.Modelos.Autorizacion;

namespace Atlas.PARS.Api.Repositorios.Interfaces;

public interface IRepositorioReglasAutorizacion
{
    Task<IReadOnlyCollection<ReglaAutorizacionVigenteDto>> ObtenerVigentesPorOperacionAsync(
        Guid idOperacion,
        DateTimeOffset fechaEvaluacion,
        CancellationToken cancellationToken);
}
