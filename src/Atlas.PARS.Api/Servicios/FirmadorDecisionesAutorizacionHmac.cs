using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.PARS.Api.Configuracion;
using Atlas.PARS.Api.Modelos.Autorizacion;
using Atlas.PARS.Api.Servicios.Interfaces;
using Microsoft.Extensions.Options;

namespace Atlas.PARS.Api.Servicios;

public sealed class FirmadorDecisionesAutorizacionHmac : IFirmadorDecisionesAutorizacion
{
    private const string AlgoritmoHmac = "HMAC-SHA256";

    private static readonly JsonSerializerOptions OpcionesJsonCanonico = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _keyId;
    private readonly byte[] _claveActiva;

    public FirmadorDecisionesAutorizacionHmac(
        IOptions<OpcionesFirmaDecisiones> opciones)
    {
        var valor = opciones.Value;

        if (string.IsNullOrWhiteSpace(valor.KeyId))
        {
            throw new InvalidOperationException(
                "FirmaDecisiones:KeyId no esta configurado. Configure user-secrets o variables de entorno.");
        }

        if (string.IsNullOrWhiteSpace(valor.ClaveActivaBase64))
        {
            throw new InvalidOperationException(
                "FirmaDecisiones:ClaveActivaBase64 no esta configurado. Configure user-secrets o variables de entorno.");
        }

        try
        {
            _claveActiva = Convert.FromBase64String(valor.ClaveActivaBase64);
        }
        catch (FormatException excepcion)
        {
            throw new InvalidOperationException(
                "FirmaDecisiones:ClaveActivaBase64 no es Base64 valido.",
                excepcion);
        }

        _keyId = valor.KeyId;
    }

    public FirmaDecision Firmar(PayloadFirmaDecision payload)
    {
        var valorFirma = CalcularFirma(payload);

        return new FirmaDecision(_keyId, AlgoritmoHmac, valorFirma);
    }

    public bool Verificar(PayloadFirmaDecision payload, FirmaDecision firma)
    {
        if (!string.Equals(firma.KeyId, _keyId, StringComparison.Ordinal) ||
            !string.Equals(firma.Algoritmo, AlgoritmoHmac, StringComparison.Ordinal))
        {
            return false;
        }

        var valorEsperado = CalcularFirma(payload);
        var bytesEsperados = Convert.FromBase64String(valorEsperado);

        byte[] bytesRecibidos;

        try
        {
            bytesRecibidos = Convert.FromBase64String(firma.Valor);
        }
        catch (FormatException)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(bytesEsperados, bytesRecibidos);
    }

    private string CalcularFirma(PayloadFirmaDecision payload)
    {
        var json = JsonSerializer.Serialize(payload, OpcionesJsonCanonico);
        var bytesFirma = HMACSHA256.HashData(
            _claveActiva,
            Encoding.UTF8.GetBytes(json));

        return Convert.ToBase64String(bytesFirma);
    }
}
