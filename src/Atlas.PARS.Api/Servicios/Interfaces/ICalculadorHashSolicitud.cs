using Atlas.PARS.Api.Modelos.Autorizacion;

namespace Atlas.PARS.Api.Servicios.Interfaces;

public interface ICalculadorHashSolicitud
{
    string Calcular(
        string codigoOrganizacion,
        SolicitudAutorizacion solicitud);
}
