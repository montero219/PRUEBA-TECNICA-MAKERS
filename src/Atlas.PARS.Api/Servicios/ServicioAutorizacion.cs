using System.Text.Json;
using Atlas.PARS.Api.Modelos.Autorizacion;
using Atlas.PARS.Api.Modelos.Entidades;
using Atlas.PARS.Api.Repositorios.Interfaces;
using Atlas.PARS.Api.Servicios.Interfaces;

namespace Atlas.PARS.Api.Servicios;

public sealed class ServicioAutorizacion : IServicioAutorizacion
{
    private readonly IRepositorioOrganizaciones _repositorioOrganizaciones;
    private readonly IRepositorioRecursosProtegidos _repositorioRecursosProtegidos;
    private readonly IRepositorioOperaciones _repositorioOperaciones;
    private readonly IRepositorioReglasAutorizacion _repositorioReglasAutorizacion;
    private readonly IRepositorioDecisionesAutorizacion _repositorioDecisionesAutorizacion;
    private readonly ICalculadorHashSolicitud _calculadorHashSolicitud;
    private readonly IEvaluadorCondiciones _evaluadorCondiciones;
    private readonly IFirmadorDecisionesAutorizacion _firmadorDecisionesAutorizacion;

    public ServicioAutorizacion(
        IRepositorioOrganizaciones repositorioOrganizaciones,
        IRepositorioRecursosProtegidos repositorioRecursosProtegidos,
        IRepositorioOperaciones repositorioOperaciones,
        IRepositorioReglasAutorizacion repositorioReglasAutorizacion,
        IRepositorioDecisionesAutorizacion repositorioDecisionesAutorizacion,
        ICalculadorHashSolicitud calculadorHashSolicitud,
        IEvaluadorCondiciones evaluadorCondiciones,
        IFirmadorDecisionesAutorizacion firmadorDecisionesAutorizacion)
    {
        _repositorioOrganizaciones = repositorioOrganizaciones;
        _repositorioRecursosProtegidos = repositorioRecursosProtegidos;
        _repositorioOperaciones = repositorioOperaciones;
        _repositorioReglasAutorizacion = repositorioReglasAutorizacion;
        _repositorioDecisionesAutorizacion = repositorioDecisionesAutorizacion;
        _calculadorHashSolicitud = calculadorHashSolicitud;
        _evaluadorCondiciones = evaluadorCondiciones;
        _firmadorDecisionesAutorizacion = firmadorDecisionesAutorizacion;
    }

    public async Task<ResultadoAutorizacion> AutorizarAsync(
        string codigoOrganizacion,
        SolicitudAutorizacion solicitud,
        CancellationToken cancellationToken,
        string? correlationId = null)
    {
        var correlationIdEfectivo = NormalizarCorrelationId(correlationId);
        var solicitudHash = _calculadorHashSolicitud.Calcular(
            codigoOrganizacion,
            solicitud);

        var organizacion = await _repositorioOrganizaciones
            .ObtenerPorOrganizacionYCodigoAsync(
                codigoOrganizacion,
                cancellationToken);

        if (organizacion is null)
        {
            return await RegistrarResultadoAsync(
                codigoOrganizacion,
                solicitud,
                organizacion: null,
                recursoProtegido: null,
                operacion: null,
                regla: null,
                decision: "DENY",
                motivo: $"La organizacion '{codigoOrganizacion}' no esta configurada.",
                codigoRegla: "TENANT_NO_CONFIGURADO",
                solicitudHash,
                correlationIdEfectivo,
                cancellationToken);
        }

        var recursoProtegido = await _repositorioRecursosProtegidos
            .ObtenerPorOrganizacionYCodigoAsync(
                organizacion.Id,
                solicitud.CodigoRecurso,
                cancellationToken);

        if (recursoProtegido is null)
        {
            return await RegistrarResultadoAsync(
                codigoOrganizacion,
                solicitud,
                organizacion,
                recursoProtegido: null,
                operacion: null,
                regla: null,
                decision: "DENY",
                motivo: $"El recurso '{solicitud.CodigoRecurso}' no esta configurado para la organizacion '{codigoOrganizacion}'.",
                codigoRegla: "RECURSO_NO_CONFIGURADO",
                solicitudHash,
                correlationIdEfectivo,
                cancellationToken);
        }

        var operacion = await _repositorioOperaciones
            .ObtenerPorRecursoYCodigoAsync(
                recursoProtegido.Id,
                solicitud.CodigoOperacion,
                cancellationToken);

        if (operacion is null)
        {
            return await RegistrarResultadoAsync(
                codigoOrganizacion,
                solicitud,
                organizacion,
                recursoProtegido,
                operacion: null,
                regla: null,
                decision: "DENY",
                motivo: $"La operacion '{solicitud.CodigoOperacion}' no esta configurada para el recurso '{solicitud.CodigoRecurso}' de la organizacion actual.",
                codigoRegla: "OPERACION_NO_CONFIGURADA",
                solicitudHash,
                correlationIdEfectivo,
                cancellationToken);
        }

        var reglasVigentes = await _repositorioReglasAutorizacion
            .ObtenerVigentesPorOperacionAsync(
                operacion.Id,
                DateTimeOffset.UtcNow,
                cancellationToken);

        if (reglasVigentes.Count == 0)
        {
            return await RegistrarResultadoAsync(
                codigoOrganizacion,
                solicitud,
                organizacion,
                recursoProtegido,
                operacion,
                regla: null,
                decision: "DENY",
                motivo: "No hay reglas vigentes configuradas para la operacion solicitada.",
                codigoRegla: "SIN_REGLAS_VIGENTES",
                solicitudHash,
                correlationIdEfectivo,
                cancellationToken);
        }

        ReglaAutorizacionVigenteDto? reglaEvaluada = null;

        try
        {
            foreach (var regla in reglasVigentes)
            {
                reglaEvaluada = regla;

                if (!_evaluadorCondiciones.Evaluar(regla.Condiciones, solicitud))
                {
                    continue;
                }

                return await RegistrarResultadoAsync(
                    codigoOrganizacion,
                    solicitud,
                    organizacion,
                    recursoProtegido,
                    operacion,
                    regla,
                    regla.DecisionSiCumple,
                    $"Aplico la regla '{regla.CodigoRegla}'.",
                    regla.CodigoRegla,
                    solicitudHash,
                    correlationIdEfectivo,
                    cancellationToken);
            }
        }
        catch (ReglaAutorizacionInvalidaException excepcion)
        {
            return await RegistrarResultadoAsync(
                codigoOrganizacion,
                solicitud,
                organizacion,
                recursoProtegido,
                operacion,
                reglaEvaluada,
                decision: "DENY",
                motivo: $"Regla de autorizacion invalida: {excepcion.Message}",
                codigoRegla: "REGLA_INVALIDA",
                solicitudHash,
                correlationIdEfectivo,
                cancellationToken);
        }

        return await RegistrarResultadoAsync(
            codigoOrganizacion,
            solicitud,
            organizacion,
            recursoProtegido,
            operacion,
            regla: null,
            decision: "DENY",
            motivo: "Ninguna regla vigente aplico para la solicitud.",
            codigoRegla: "SIN_REGLA_APLICABLE",
            solicitudHash,
            correlationIdEfectivo,
            cancellationToken);
    }

    private async Task<ResultadoAutorizacion> RegistrarResultadoAsync(
        string codigoOrganizacion,
        SolicitudAutorizacion solicitud,
        Organizacion? organizacion,
        RecursoProtegido? recursoProtegido,
        Operacion? operacion,
        ReglaAutorizacionVigenteDto? regla,
        string decision,
        string motivo,
        string codigoRegla,
        string solicitudHash,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var idDecision = Guid.NewGuid();
        var fechaDecision = DateTimeOffset.UtcNow;
        var firma = _firmadorDecisionesAutorizacion.Firmar(new PayloadFirmaDecision(
            idDecision,
            codigoOrganizacion,
            decision,
            codigoRegla,
            solicitudHash,
            correlationId,
            fechaDecision));

        var decisionAutorizacion = new DecisionAutorizacion
        {
            Id = idDecision,
            CodigoOrganizacion = codigoOrganizacion,
            IdOrganizacion = organizacion?.Id,
            IdRecursoProtegido = recursoProtegido?.Id,
            IdOperacion = operacion?.Id,
            IdReglaAutorizacion = regla?.IdReglaAutorizacion,
            IdVersionRegla = regla?.IdVersionRegla,
            CodigoRecursoSolicitado = solicitud.CodigoRecurso,
            CodigoOperacionSolicitada = solicitud.CodigoOperacion,
            Decision = decision,
            Motivo = motivo,
            CodigoRegla = codigoRegla,
            NumeroVersionRegla = regla?.NumeroVersion,
            Actor = CrearJsonCanonico(solicitud.AtributosActor),
            Recurso = CrearJsonCanonico(solicitud.AtributosRecurso),
            Contexto = CrearJsonCanonico(solicitud.Contexto),
            AlgoritmoHash = "SHA-256",
            SolicitudHash = solicitudHash,
            CorrelationId = correlationId,
            FechaDecision = fechaDecision,
            KeyIdFirma = firma.KeyId,
            AlgoritmoFirma = firma.Algoritmo,
            Firma = firma.Valor
        };

        await _repositorioDecisionesAutorizacion.RegistrarAsync(
            decisionAutorizacion,
            cancellationToken);

        return new ResultadoAutorizacion
        {
            IdDecision = idDecision,
            Decision = decision,
            Motivo = motivo,
            CodigoRegla = codigoRegla,
            CorrelationId = correlationId,
            SolicitudHash = solicitudHash,
            KeyId = firma.KeyId,
            Firma = firma.Valor
        };
    }

    private static JsonDocument CrearJsonCanonico(
        IReadOnlyDictionary<string, string> valores)
    {
        var valoresOrdenados = new SortedDictionary<string, string>(
            valores.ToDictionary(
                valor => valor.Key,
                valor => valor.Value),
            StringComparer.Ordinal);
        var json = JsonSerializer.Serialize(valoresOrdenados);

        return JsonDocument.Parse(json);
    }

    private static string NormalizarCorrelationId(string? correlationId)
    {
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.Trim();
        }

        return Guid.NewGuid().ToString("N");
    }
}
