using Atlas.PARS.Api.Modelos.Autorizacion;
using Atlas.PARS.Api.Servicios;

namespace Atlas.PARS.PruebasUnitarias.Servicios;

public class CalculadorHashSolicitudPruebas
{
    [Fact]
    public void Calcular_CuandoLosAtributosTienenDistintoOrden_RetornaElMismoHash()
    {
        var calculador = new CalculadorHashSolicitud();
        var solicitudA = new SolicitudAutorizacion
        {
            CodigoRecurso = "TRANSFERENCIA",
            CodigoOperacion = "APROBAR",
            AtributosActor =
            {
                ["rol"] = "CLIENTE",
                ["organizacion"] = "FINORA"
            },
            AtributosRecurso =
            {
                ["monto"] = "500000",
                ["organizacion"] = "FINORA"
            },
            Contexto =
            {
                ["hora"] = "10:00",
                ["nivelRiesgo"] = "BAJO"
            }
        };
        var solicitudB = new SolicitudAutorizacion
        {
            CodigoRecurso = "TRANSFERENCIA",
            CodigoOperacion = "APROBAR",
            AtributosActor =
            {
                ["organizacion"] = "FINORA",
                ["rol"] = "CLIENTE"
            },
            AtributosRecurso =
            {
                ["organizacion"] = "FINORA",
                ["monto"] = "500000"
            },
            Contexto =
            {
                ["nivelRiesgo"] = "BAJO",
                ["hora"] = "10:00"
            }
        };

        var hashA = calculador.Calcular("FINORA", solicitudA);
        var hashB = calculador.Calcular("FINORA", solicitudB);

        Assert.Equal(hashA, hashB);
        Assert.Matches("^[a-f0-9]{64}$", hashA);
    }

    [Fact]
    public void Calcular_CuandoCambiaUnAtributo_RetornaUnHashDiferente()
    {
        var calculador = new CalculadorHashSolicitud();
        var solicitudPermitida = CrearSolicitud("BAJO");
        var solicitudRiesgosa = CrearSolicitud("CRITICO");

        var hashPermitido = calculador.Calcular("FINORA", solicitudPermitida);
        var hashRiesgoso = calculador.Calcular("FINORA", solicitudRiesgosa);

        Assert.NotEqual(hashPermitido, hashRiesgoso);
    }

    private static SolicitudAutorizacion CrearSolicitud(string nivelRiesgo)
    {
        return new SolicitudAutorizacion
        {
            CodigoRecurso = "TRANSFERENCIA",
            CodigoOperacion = "APROBAR",
            AtributosActor =
            {
                ["rol"] = "CLIENTE",
                ["organizacion"] = "FINORA"
            },
            AtributosRecurso =
            {
                ["monto"] = "500000",
                ["organizacion"] = "FINORA"
            },
            Contexto =
            {
                ["hora"] = "10:00",
                ["nivelRiesgo"] = nivelRiesgo
            }
        };
    }
}
