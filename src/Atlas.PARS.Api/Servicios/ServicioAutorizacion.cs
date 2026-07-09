using Atlas.PARS.Api.Modelos.Autorizacion;
using Atlas.PARS.Api.Repositorios.Interfaces;
using Atlas.PARS.Api.Servicios.Interfaces;

namespace Atlas.PARS.Api.Servicios;

public sealed class ServicioAutorizacion : IServicioAutorizacion
{
    private readonly IRepositorioOrganizaciones _repositorioOrganizaciones;
    private readonly IRepositorioRecursosProtegidos _repositorioRecursosProtegidos;
    private readonly IRepositorioOperaciones _repositorioOperaciones;
    private readonly IRepositorioReglasAutorizacion _repositorioReglasAutorizacion;
    private readonly IEvaluadorCondiciones _evaluadorCondiciones;

    public ServicioAutorizacion(
        IRepositorioOrganizaciones repositorioOrganizaciones,
        IRepositorioRecursosProtegidos repositorioRecursosProtegidos,
        IRepositorioOperaciones repositorioOperaciones,
        IRepositorioReglasAutorizacion repositorioReglasAutorizacion,
        IEvaluadorCondiciones evaluadorCondiciones)
    {
        _repositorioOrganizaciones = repositorioOrganizaciones;
        _repositorioRecursosProtegidos = repositorioRecursosProtegidos;
        _repositorioOperaciones = repositorioOperaciones;
        _repositorioReglasAutorizacion = repositorioReglasAutorizacion;
        _evaluadorCondiciones = evaluadorCondiciones;
    }

    public async Task<ResultadoAutorizacion> AutorizarAsync(
        string codigoOrganizacion,
        SolicitudAutorizacion solicitud,
        CancellationToken cancellationToken)
    {
        var organizacion = await _repositorioOrganizaciones
            .ObtenerPorOrganizacionYCodigoAsync(
                codigoOrganizacion,
                cancellationToken);

        if (organizacion is null)
        {
            throw new KeyNotFoundException(
                $"No existe una organización con código '{codigoOrganizacion}'.");
        }

        var recursoProtegido = await _repositorioRecursosProtegidos
            .ObtenerPorOrganizacionYCodigoAsync(
                organizacion.Id,
                solicitud.CodigoRecurso,
                cancellationToken);

        if (recursoProtegido is null)
        {
            throw new KeyNotFoundException(
                $"El recurso '{solicitud.CodigoRecurso}' no está configurado " +
                $"para la organización '{codigoOrganizacion}'.");
        }

        var operacion = await _repositorioOperaciones
            .ObtenerPorRecursoYCodigoAsync(
                recursoProtegido.Id,
                solicitud.CodigoOperacion,
                cancellationToken);

        if (operacion is null)
        {
            throw new KeyNotFoundException(
                $"La operación '{solicitud.CodigoOperacion}' no está configurada " +
                $"para el recurso '{solicitud.CodigoRecurso}' de la organización actual.");
        }

        var reglasVigentes = await _repositorioReglasAutorizacion
            .ObtenerVigentesPorOperacionAsync(
                operacion.Id,
                DateTimeOffset.UtcNow,
                cancellationToken);

        if (reglasVigentes.Count == 0)
        {
            return new ResultadoAutorizacion
            {
                Decision = "DENY",
                Motivo = "No hay reglas vigentes configuradas para la operacion solicitada.",
                CodigoRegla = "SIN_REGLAS_VIGENTES"
            };
        }

        try
        {
            foreach (var regla in reglasVigentes)
            {
                if (!_evaluadorCondiciones.Evaluar(regla.Condiciones, solicitud))
                {
                    continue;
                }

                return new ResultadoAutorizacion
                {
                    Decision = regla.DecisionSiCumple,
                    Motivo = $"Aplico la regla '{regla.CodigoRegla}'.",
                    CodigoRegla = regla.CodigoRegla
                };
            }
        }
        catch (ReglaAutorizacionInvalidaException excepcion)
        {
            return new ResultadoAutorizacion
            {
                Decision = "DENY",
                Motivo = $"Regla de autorizacion invalida: {excepcion.Message}",
                CodigoRegla = "REGLA_INVALIDA"
            };
        }

        return new ResultadoAutorizacion
        {
            Decision = "DENY",
            Motivo = "Ninguna regla vigente aplico para la solicitud.",
            CodigoRegla = "SIN_REGLA_APLICABLE"
        };
    }
}
