using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.PARS.Api.Modelos.Autorizacion;
using Atlas.PARS.Api.Servicios.Interfaces;

namespace Atlas.PARS.Api.Servicios;

public sealed class CalculadorHashSolicitud : ICalculadorHashSolicitud
{
    private static readonly JsonSerializerOptions OpcionesJsonCanonico = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string Calcular(
        string codigoOrganizacion,
        SolicitudAutorizacion solicitud)
    {
        var payload = new PayloadSolicitud(
            codigoOrganizacion,
            solicitud.CodigoRecurso,
            solicitud.CodigoOperacion,
            Ordenar(solicitud.AtributosActor),
            Ordenar(solicitud.AtributosRecurso),
            Ordenar(solicitud.Contexto));

        var json = JsonSerializer.Serialize(
            payload,
            OpcionesJsonCanonico);
        var bytesHash = SHA256.HashData(
            Encoding.UTF8.GetBytes(json));

        return Convert
            .ToHexString(bytesHash)
            .ToLowerInvariant();
    }

    private static SortedDictionary<string, string> Ordenar(
        IReadOnlyDictionary<string, string> valores)
    {
        return new SortedDictionary<string, string>(
            valores.ToDictionary(
                valor => valor.Key,
                valor => valor.Value),
            StringComparer.Ordinal);
    }

    private sealed record PayloadSolicitud(
        string CodigoOrganizacion,
        string CodigoRecurso,
        string CodigoOperacion,
        SortedDictionary<string, string> Actor,
        SortedDictionary<string, string> Recurso,
        SortedDictionary<string, string> Contexto);
}
