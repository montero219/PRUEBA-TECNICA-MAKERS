using System.Text.Json;
using Atlas.PARS.Api.Modelos.Autorizacion;
using Atlas.PARS.Api.Servicios;

namespace Atlas.PARS.PruebasUnitarias.Servicios;

public class EvaluadorCondicionesPruebas
{
    [Fact]
    public void Evaluar_CuandoTodasLasCondicionesSeCumplen_RetornaTrue()
    {
        using var condiciones = JsonDocument.Parse("""
            {
                "todas": [
                    {
                        "fuente": "actor",
                        "atributo": "rol",
                        "operador": "igual",
                        "valor": "CLIENTE"
                    },
                    {
                        "fuente": "actor",
                        "atributo": "organizacion",
                        "operador": "igual",
                        "compararCon": {
                            "fuente": "recurso",
                            "atributo": "organizacion"
                        }
                    },
                    {
                        "fuente": "recurso",
                        "atributo": "monto",
                        "operador": "menor_o_igual",
                        "valor": 1000000
                    },
                    {
                        "fuente": "contexto",
                        "atributo": "hora",
                        "operador": "entre_horas",
                        "desde": "06:00",
                        "hasta": "22:00"
                    },
                    {
                        "fuente": "contexto",
                        "atributo": "dispositivoConfiable",
                        "operador": "igual",
                        "valor": true
                    },
                    {
                        "fuente": "contexto",
                        "atributo": "nivelRiesgo",
                        "operador": "igual",
                        "valor": "BAJO"
                    }
                ]
            }
            """);
        var solicitud = CrearSolicitudNormal();
        var evaluador = new EvaluadorCondiciones();

        var resultado = evaluador.Evaluar(condiciones, solicitud);

        Assert.True(resultado);
    }

    [Fact]
    public void Evaluar_CuandoUnaCondicionNoSeCumple_RetornaFalse()
    {
        using var condiciones = JsonDocument.Parse("""
            {
                "todas": [
                    {
                        "fuente": "contexto",
                        "atributo": "nivelRiesgo",
                        "operador": "igual",
                        "valor": "CRITICO"
                    }
                ]
            }
            """);
        var solicitud = CrearSolicitudNormal();
        var evaluador = new EvaluadorCondiciones();

        var resultado = evaluador.Evaluar(condiciones, solicitud);

        Assert.False(resultado);
    }

    [Fact]
    public void Evaluar_CuandoCompararConNoExiste_RetornaFalse()
    {
        using var condiciones = JsonDocument.Parse("""
            {
                "todas": [
                    {
                        "fuente": "actor",
                        "atributo": "organizacion",
                        "operador": "distinto",
                        "compararCon": {
                            "fuente": "recurso",
                            "atributo": "organizacionInexistente"
                        }
                    }
                ]
            }
            """);
        var solicitud = CrearSolicitudNormal();
        var evaluador = new EvaluadorCondiciones();

        var resultado = evaluador.Evaluar(condiciones, solicitud);

        Assert.False(resultado);
    }

    [Fact]
    public void Evaluar_CuandoElOperadorEsDesconocido_LanzaErrorDeConfiguracion()
    {
        using var condiciones = JsonDocument.Parse("""
            {
                "todas": [
                    {
                        "fuente": "contexto",
                        "atributo": "nivelRiesgo",
                        "operador": "operador_inexistente",
                        "valor": "BAJO"
                    }
                ]
            }
            """);
        var solicitud = CrearSolicitudNormal();
        var evaluador = new EvaluadorCondiciones();

        Assert.Throws<ReglaAutorizacionInvalidaException>(() =>
            evaluador.Evaluar(condiciones, solicitud));
    }

    private static SolicitudAutorizacion CrearSolicitudNormal()
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
                ["dispositivoConfiable"] = "true",
                ["nivelRiesgo"] = "BAJO"
            }
        };
    }
}
