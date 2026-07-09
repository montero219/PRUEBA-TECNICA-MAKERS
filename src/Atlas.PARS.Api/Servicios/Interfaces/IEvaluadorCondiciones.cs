using System.Text.Json;
using Atlas.PARS.Api.Modelos.Autorizacion;

namespace Atlas.PARS.Api.Servicios.Interfaces;

public interface IEvaluadorCondiciones
{
    bool Evaluar(
        JsonDocument condiciones,
        SolicitudAutorizacion solicitud);
}
