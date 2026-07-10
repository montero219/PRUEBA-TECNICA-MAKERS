using System.Text;
using Atlas.PARS.Api.Configuracion;
using Atlas.PARS.Api.Modelos.Autorizacion;
using Atlas.PARS.Api.Servicios;
using Microsoft.Extensions.Options;

namespace Atlas.PARS.PruebasUnitarias.Servicios;

public class FirmadorDecisionesAutorizacionHmacPruebas
{
    private static readonly PayloadFirmaDecision PayloadBase = new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "FINORA",
        "PERMIT",
        "PERMITIR_TRANSFERENCIA_NORMAL",
        "abc123",
        "corr-1",
        DateTimeOffset.Parse("2026-07-09T10:00:00+00:00"));

    [Fact]
    public void Firmar_ConElMismoPayloadYLaMismaClave_RetornaLaMismaFirma()
    {
        var firmador = CrearFirmador("clave-de-prueba-numero-uno-abc");

        var firmaA = firmador.Firmar(PayloadBase);
        var firmaB = firmador.Firmar(PayloadBase);

        Assert.Equal(firmaA.Valor, firmaB.Valor);
    }

    [Fact]
    public void Firmar_CuandoCambiaUnCampoDelPayload_RetornaUnaFirmaDiferente()
    {
        var firmador = CrearFirmador("clave-de-prueba-numero-uno-abc");
        var payloadAlterado = PayloadBase with { Decision = "DENY" };

        var firmaOriginal = firmador.Firmar(PayloadBase);
        var firmaAlterada = firmador.Firmar(payloadAlterado);

        Assert.NotEqual(firmaOriginal.Valor, firmaAlterada.Valor);
    }

    [Fact]
    public void Verificar_ConUnaFirmaValida_RetornaTrue()
    {
        var firmador = CrearFirmador("clave-de-prueba-numero-uno-abc");
        var firma = firmador.Firmar(PayloadBase);

        Assert.True(firmador.Verificar(PayloadBase, firma));
    }

    [Fact]
    public void Verificar_ConElPayloadAlterado_RetornaFalse()
    {
        var firmador = CrearFirmador("clave-de-prueba-numero-uno-abc");
        var firma = firmador.Firmar(PayloadBase);
        var payloadAlterado = PayloadBase with { SolicitudHash = "hash-manipulado" };

        Assert.False(firmador.Verificar(payloadAlterado, firma));
    }

    [Fact]
    public void Verificar_ConUnaFirmaCalculadaConOtraClave_RetornaFalse()
    {
        var firmadorOriginal = CrearFirmador("clave-de-prueba-numero-uno-abc");
        var firmadorConOtraClave = CrearFirmador("clave-de-prueba-numero-dos-xyz");
        var firma = firmadorOriginal.Firmar(PayloadBase);

        Assert.False(firmadorConOtraClave.Verificar(PayloadBase, firma));
    }

    [Fact]
    public void Constructor_SinKeyIdConfigurado_LanzaExcepcion()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new FirmadorDecisionesAutorizacionHmac(Options.Create(new OpcionesFirmaDecisiones
            {
                KeyId = string.Empty,
                ClaveActivaBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("clave-de-prueba-numero-uno-abc"))
            })));
    }

    [Fact]
    public void Constructor_SinClaveConfigurada_LanzaExcepcion()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new FirmadorDecisionesAutorizacionHmac(Options.Create(new OpcionesFirmaDecisiones
            {
                KeyId = "test-key",
                ClaveActivaBase64 = string.Empty
            })));
    }

    private static FirmadorDecisionesAutorizacionHmac CrearFirmador(string clave)
    {
        return new FirmadorDecisionesAutorizacionHmac(
            Options.Create(new OpcionesFirmaDecisiones
            {
                KeyId = "test-key",
                ClaveActivaBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(clave))
            }));
    }
}
