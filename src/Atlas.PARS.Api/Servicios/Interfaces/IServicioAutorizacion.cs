using Atlas.PARS.Api.Modelos.Autorizacion;

namespace Atlas.PARS.Api.Servicios.Interfaces;

public interface IServicioAutorizacion
{
    Task<ResultadoAutorizacion> AutorizarAsync(
        string codigoOrganizacion,
        SolicitudAutorizacion solicitud,
        CancellationToken cancellationToken,
        string? correlationId = null);
}
