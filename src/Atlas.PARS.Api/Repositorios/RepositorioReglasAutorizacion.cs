using Atlas.PARS.Api.Datos;
using Atlas.PARS.Api.Modelos.Autorizacion;
using Atlas.PARS.Api.Repositorios.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Atlas.PARS.Api.Repositorios;

public sealed class RepositorioReglasAutorizacion
    : IRepositorioReglasAutorizacion
{
    private readonly ContextoAtlas _contexto;

    public RepositorioReglasAutorizacion(ContextoAtlas contexto)
    {
        _contexto = contexto;
    }

    public async Task<IReadOnlyCollection<ReglaAutorizacionVigenteDto>> ObtenerVigentesPorOperacionAsync(
        Guid idOperacion,
        DateTimeOffset fechaEvaluacion,
        CancellationToken cancellationToken)
    {
        return await _contexto.VersionesRegla
            .AsNoTracking()
            .Where(version =>
                version.ReglaAutorizacion.IdOperacion == idOperacion &&
                version.VigenciaDesde <= fechaEvaluacion &&
                (version.VigenciaHasta == null || version.VigenciaHasta > fechaEvaluacion))
            .OrderBy(version => version.Prioridad)
            .Select(version => new ReglaAutorizacionVigenteDto
            {
                IdReglaAutorizacion = version.IdReglaAutorizacion,
                IdVersionRegla = version.Id,
                CodigoRegla = version.ReglaAutorizacion.Codigo,
                NombreRegla = version.ReglaAutorizacion.Nombre,
                NumeroVersion = version.NumeroVersion,
                Condiciones = version.Condiciones,
                DecisionSiCumple = version.DecisionSiCumple,
                Prioridad = version.Prioridad
            })
            .ToListAsync(cancellationToken);
    }
}
