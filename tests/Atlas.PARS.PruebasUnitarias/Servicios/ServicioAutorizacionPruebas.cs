using Atlas.PARS.Api.Configuracion;
using Atlas.PARS.Api.Modelos.Autorizacion;
using Atlas.PARS.Api.Modelos.Entidades;
using Atlas.PARS.Api.Repositorios.Interfaces;
using Atlas.PARS.Api.Servicios;
using Atlas.PARS.Api.Servicios.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Atlas.PARS.PruebasUnitarias.Servicios;

public class ServicioAutorizacionPruebas
{
    private static IFirmadorDecisionesAutorizacion CrearFirmadorDePrueba()
    {
        return new FirmadorDecisionesAutorizacionHmac(
            Options.Create(new OpcionesFirmaDecisiones
            {
                KeyId = "test-key",
                ClaveActivaBase64 = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes("clave-de-prueba-32-bytes-largo!"))
            }));
    }

    [Fact]
    public async Task AutorizarAsync_CuandoUnaReglaVigenteAplica_RetornaDecisionDeLaRegla()
    {
        var organizacion = new Organizacion
        {
            Id = Guid.NewGuid(),
            Codigo = "FINORA",
            Nombre = "Finora",
            ZonaHoraria = "America/Bogota"
        };
        var recursoProtegido = new RecursoProtegido
        {
            Id = Guid.NewGuid(),
            IdOrganizacion = organizacion.Id,
            Codigo = "TRANSFERENCIA",
            Nombre = "Transferencia"
        };
        var operacion = new Operacion
        {
            Id = Guid.NewGuid(),
            IdRecursoProtegido = recursoProtegido.Id,
            Codigo = "APROBAR",
            Nombre = "Aprobar transferencia"
        };
        var regla = new ReglaAutorizacionVigenteDto
        {
            CodigoRegla = "PERMITIR_TRANSFERENCIA_NORMAL",
            DecisionSiCumple = "PERMIT",
            Condiciones = JsonDocument.Parse("""
                {
                    "todas": [
                        {
                            "fuente": "actor",
                            "atributo": "rol",
                            "operador": "igual",
                            "valor": "CLIENTE"
                        }
                    ]
                }
                """)
        };
        var servicio = new ServicioAutorizacion(
            new RepositorioOrganizacionesFake(organizacion),
            new RepositorioRecursosProtegidosFake(recursoProtegido),
            new RepositorioOperacionesFake(operacion),
            new RepositorioReglasAutorizacionFake(new[] { regla }),
            new RepositorioDecisionesAutorizacionFake(),
            new CalculadorHashSolicitud(),
            new EvaluadorCondiciones(),
            CrearFirmadorDePrueba());
        var solicitud = new SolicitudAutorizacion
        {
            CodigoRecurso = "TRANSFERENCIA",
            CodigoOperacion = "APROBAR",
            AtributosActor =
            {
                ["rol"] = "CLIENTE"
            }
        };

        var resultado = await servicio.AutorizarAsync(
            "FINORA",
            solicitud,
            CancellationToken.None);

        Assert.Equal("PERMIT", resultado.Decision);
        Assert.Equal("PERMITIR_TRANSFERENCIA_NORMAL", resultado.CodigoRegla);
        Assert.Contains("PERMITIR_TRANSFERENCIA_NORMAL", resultado.Motivo);
        Assert.Equal("test-key", resultado.KeyId);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Firma));
    }

    [Fact]
    public async Task AutorizarAsync_CuandoUnaReglaVigenteAplica_RegistraDecisionAuditada()
    {
        var organizacion = new Organizacion
        {
            Id = Guid.NewGuid(),
            Codigo = "FINORA",
            Nombre = "Finora",
            ZonaHoraria = "America/Bogota"
        };
        var recursoProtegido = new RecursoProtegido
        {
            Id = Guid.NewGuid(),
            IdOrganizacion = organizacion.Id,
            Codigo = "TRANSFERENCIA",
            Nombre = "Transferencia"
        };
        var operacion = new Operacion
        {
            Id = Guid.NewGuid(),
            IdRecursoProtegido = recursoProtegido.Id,
            Codigo = "APROBAR",
            Nombre = "Aprobar transferencia"
        };
        var regla = new ReglaAutorizacionVigenteDto
        {
            IdReglaAutorizacion = Guid.NewGuid(),
            IdVersionRegla = Guid.NewGuid(),
            CodigoRegla = "PERMITIR_TRANSFERENCIA_NORMAL",
            NumeroVersion = "1.0.0",
            DecisionSiCumple = "PERMIT",
            Condiciones = JsonDocument.Parse("""
                {
                    "todas": [
                        {
                            "fuente": "actor",
                            "atributo": "rol",
                            "operador": "igual",
                            "valor": "CLIENTE"
                        }
                    ]
                }
                """)
        };
        var repositorioAuditoria = new RepositorioDecisionesAutorizacionFake();
        var calculadorHash = new CalculadorHashSolicitud();
        var servicio = new ServicioAutorizacion(
            new RepositorioOrganizacionesFake(organizacion),
            new RepositorioRecursosProtegidosFake(recursoProtegido),
            new RepositorioOperacionesFake(operacion),
            new RepositorioReglasAutorizacionFake(new[] { regla }),
            repositorioAuditoria,
            calculadorHash,
            new EvaluadorCondiciones(),
            CrearFirmadorDePrueba());
        var solicitud = new SolicitudAutorizacion
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
        var solicitudHashEsperado = calculadorHash.Calcular("FINORA", solicitud);

        var resultado = await servicio.AutorizarAsync(
            "FINORA",
            solicitud,
            CancellationToken.None,
            "corr-123");

        var auditoria = repositorioAuditoria.DecisionRegistrada;
        Assert.NotNull(auditoria);
        Assert.Equal(resultado.IdDecision, auditoria.Id);
        Assert.Equal("corr-123", resultado.CorrelationId);
        Assert.Equal(solicitudHashEsperado, resultado.SolicitudHash);
        Assert.Equal("FINORA", auditoria.CodigoOrganizacion);
        Assert.Equal(organizacion.Id, auditoria.IdOrganizacion);
        Assert.Equal(recursoProtegido.Id, auditoria.IdRecursoProtegido);
        Assert.Equal(operacion.Id, auditoria.IdOperacion);
        Assert.Equal(regla.IdReglaAutorizacion, auditoria.IdReglaAutorizacion);
        Assert.Equal(regla.IdVersionRegla, auditoria.IdVersionRegla);
        Assert.Equal("TRANSFERENCIA", auditoria.CodigoRecursoSolicitado);
        Assert.Equal("APROBAR", auditoria.CodigoOperacionSolicitada);
        Assert.Equal("PERMIT", auditoria.Decision);
        Assert.Equal("PERMITIR_TRANSFERENCIA_NORMAL", auditoria.CodigoRegla);
        Assert.Equal("1.0.0", auditoria.NumeroVersionRegla);
        Assert.Equal("SHA-256", auditoria.AlgoritmoHash);
        Assert.Equal(solicitudHashEsperado, auditoria.SolicitudHash);
        Assert.Equal("corr-123", auditoria.CorrelationId);
        Assert.Equal("CLIENTE", auditoria.Actor.RootElement.GetProperty("rol").GetString());
        Assert.Equal("500000", auditoria.Recurso.RootElement.GetProperty("monto").GetString());
        Assert.Equal("BAJO", auditoria.Contexto.RootElement.GetProperty("nivelRiesgo").GetString());
        Assert.Equal("test-key", auditoria.KeyIdFirma);
        Assert.Equal("HMAC-SHA256", auditoria.AlgoritmoFirma);
        Assert.Equal(resultado.Firma, auditoria.Firma);
    }

    [Fact]
    public async Task AutorizarAsync_CuandoUnaReglaExigeChallengePorMontoSensible_RetornaChallengeAuditado()
    {
        var organizacion = new Organizacion
        {
            Id = Guid.NewGuid(),
            Codigo = "FINORA",
            Nombre = "Finora",
            ZonaHoraria = "America/Bogota"
        };
        var recursoProtegido = new RecursoProtegido
        {
            Id = Guid.NewGuid(),
            IdOrganizacion = organizacion.Id,
            Codigo = "TRANSFERENCIA",
            Nombre = "Transferencia"
        };
        var operacion = new Operacion
        {
            Id = Guid.NewGuid(),
            IdRecursoProtegido = recursoProtegido.Id,
            Codigo = "APROBAR",
            Nombre = "Aprobar transferencia"
        };
        var regla = new ReglaAutorizacionVigenteDto
        {
            IdReglaAutorizacion = Guid.NewGuid(),
            IdVersionRegla = Guid.NewGuid(),
            CodigoRegla = "MONTO_SENSIBLE",
            NumeroVersion = "1.0.0",
            DecisionSiCumple = "CHALLENGE",
            Condiciones = JsonDocument.Parse("""
                {
                    "todas": [
                        {
                            "fuente": "recurso",
                            "atributo": "monto",
                            "operador": "mayor_o_igual",
                            "valor": 1000001
                        }
                    ]
                }
                """)
        };
        var repositorioAuditoria = new RepositorioDecisionesAutorizacionFake();
        var servicio = new ServicioAutorizacion(
            new RepositorioOrganizacionesFake(organizacion),
            new RepositorioRecursosProtegidosFake(recursoProtegido),
            new RepositorioOperacionesFake(operacion),
            new RepositorioReglasAutorizacionFake(new[] { regla }),
            repositorioAuditoria,
            new CalculadorHashSolicitud(),
            new EvaluadorCondiciones(),
            CrearFirmadorDePrueba());
        var solicitud = new SolicitudAutorizacion
        {
            CodigoRecurso = "TRANSFERENCIA",
            CodigoOperacion = "APROBAR",
            AtributosRecurso =
            {
                ["monto"] = "1500000"
            }
        };

        var resultado = await servicio.AutorizarAsync(
            "FINORA",
            solicitud,
            CancellationToken.None);

        Assert.Equal("CHALLENGE", resultado.Decision);
        Assert.Equal("MONTO_SENSIBLE", resultado.CodigoRegla);
        Assert.Equal("test-key", resultado.KeyId);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Firma));

        var auditoria = repositorioAuditoria.DecisionRegistrada;
        Assert.NotNull(auditoria);
        Assert.Equal("CHALLENGE", auditoria.Decision);
        Assert.Equal("MONTO_SENSIBLE", auditoria.CodigoRegla);
        Assert.Equal("test-key", auditoria.KeyIdFirma);
        Assert.Equal(resultado.Firma, auditoria.Firma);
    }

    [Fact]
    public async Task AutorizarAsync_CuandoActorYRecursoPertenecenAOrganizacionesDistintas_RetornaDenyPorAislamiento()
    {
        var organizacion = new Organizacion
        {
            Id = Guid.NewGuid(),
            Codigo = "FINORA",
            Nombre = "Finora",
            ZonaHoraria = "America/Bogota"
        };
        var recursoProtegido = new RecursoProtegido
        {
            Id = Guid.NewGuid(),
            IdOrganizacion = organizacion.Id,
            Codigo = "TRANSFERENCIA",
            Nombre = "Transferencia"
        };
        var operacion = new Operacion
        {
            Id = Guid.NewGuid(),
            IdRecursoProtegido = recursoProtegido.Id,
            Codigo = "APROBAR",
            Nombre = "Aprobar transferencia"
        };
        var reglaAislamiento = new ReglaAutorizacionVigenteDto
        {
            IdReglaAutorizacion = Guid.NewGuid(),
            IdVersionRegla = Guid.NewGuid(),
            CodigoRegla = "VALIDAR_AISLAMIENTO_TENANT",
            NumeroVersion = "1.0.0",
            DecisionSiCumple = "DENY",
            Condiciones = JsonDocument.Parse("""
                {
                    "todas": [
                        {
                            "fuente": "actor",
                            "atributo": "organizacion",
                            "operador": "distinto",
                            "compararCon": {
                                "fuente": "recurso",
                                "atributo": "organizacion"
                            }
                        }
                    ]
                }
                """)
        };
        var reglaPermitir = new ReglaAutorizacionVigenteDto
        {
            IdReglaAutorizacion = Guid.NewGuid(),
            IdVersionRegla = Guid.NewGuid(),
            CodigoRegla = "PERMITIR_TRANSFERENCIA_NORMAL",
            NumeroVersion = "1.0.0",
            DecisionSiCumple = "PERMIT",
            Condiciones = JsonDocument.Parse("""
                {
                    "todas": [
                        {
                            "fuente": "actor",
                            "atributo": "rol",
                            "operador": "igual",
                            "valor": "CLIENTE"
                        }
                    ]
                }
                """)
        };
        var repositorioAuditoria = new RepositorioDecisionesAutorizacionFake();
        var servicio = new ServicioAutorizacion(
            new RepositorioOrganizacionesFake(organizacion),
            new RepositorioRecursosProtegidosFake(recursoProtegido),
            new RepositorioOperacionesFake(operacion),
            new RepositorioReglasAutorizacionFake(new[] { reglaAislamiento, reglaPermitir }),
            repositorioAuditoria,
            new CalculadorHashSolicitud(),
            new EvaluadorCondiciones(),
            CrearFirmadorDePrueba());
        var solicitud = new SolicitudAutorizacion
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
                ["organizacion"] = "OTRA_ORG"
            }
        };

        var resultado = await servicio.AutorizarAsync(
            "FINORA",
            solicitud,
            CancellationToken.None);

        Assert.Equal("DENY", resultado.Decision);
        Assert.Equal("VALIDAR_AISLAMIENTO_TENANT", resultado.CodigoRegla);

        var auditoria = repositorioAuditoria.DecisionRegistrada;
        Assert.NotNull(auditoria);
        Assert.Equal("DENY", auditoria.Decision);
        Assert.Equal("VALIDAR_AISLAMIENTO_TENANT", auditoria.CodigoRegla);
        Assert.Equal(resultado.Firma, auditoria.Firma);
    }

    [Fact]
    public async Task AutorizarAsync_CuandoNoHayReglasVigentes_RetornaDenyControlado()
    {
        var organizacion = new Organizacion
        {
            Id = Guid.NewGuid(),
            Codigo = "FINORA",
            Nombre = "Finora",
            ZonaHoraria = "America/Bogota"
        };
        var recursoProtegido = new RecursoProtegido
        {
            Id = Guid.NewGuid(),
            IdOrganizacion = organizacion.Id,
            Codigo = "TRANSFERENCIA",
            Nombre = "Transferencia"
        };
        var operacion = new Operacion
        {
            Id = Guid.NewGuid(),
            IdRecursoProtegido = recursoProtegido.Id,
            Codigo = "APROBAR",
            Nombre = "Aprobar transferencia"
        };
        var repositorioReglas = new RepositorioReglasAutorizacionFake(
            Array.Empty<ReglaAutorizacionVigenteDto>());
        var servicio = new ServicioAutorizacion(
            new RepositorioOrganizacionesFake(organizacion),
            new RepositorioRecursosProtegidosFake(recursoProtegido),
            new RepositorioOperacionesFake(operacion),
            repositorioReglas,
            new RepositorioDecisionesAutorizacionFake(),
            new CalculadorHashSolicitud(),
            new EvaluadorCondicionesFake(false),
            CrearFirmadorDePrueba());
        var solicitud = new SolicitudAutorizacion
        {
            CodigoRecurso = "TRANSFERENCIA",
            CodigoOperacion = "APROBAR"
        };

        var resultado = await servicio.AutorizarAsync(
            "FINORA",
            solicitud,
            CancellationToken.None);

        Assert.Equal("DENY", resultado.Decision);
        Assert.Equal("SIN_REGLAS_VIGENTES", resultado.CodigoRegla);
        Assert.Contains("No hay reglas vigentes", resultado.Motivo);
        Assert.Equal(operacion.Id, repositorioReglas.IdOperacionConsultada);
    }

    [Fact]
    public async Task AutorizarAsync_CuandoOrganizacionNoExiste_RegistraDenyAuditado()
    {
        var repositorioAuditoria = new RepositorioDecisionesAutorizacionFake();
        var servicio = new ServicioAutorizacion(
            new RepositorioOrganizacionesFake(null),
            new RepositorioRecursosProtegidosFake(null),
            new RepositorioOperacionesFake(null),
            new RepositorioReglasAutorizacionFake(Array.Empty<ReglaAutorizacionVigenteDto>()),
            repositorioAuditoria,
            new CalculadorHashSolicitud(),
            new EvaluadorCondicionesFake(false),
            CrearFirmadorDePrueba());
        var solicitud = new SolicitudAutorizacion
        {
            CodigoRecurso = "TRANSFERENCIA",
            CodigoOperacion = "APROBAR"
        };

        var resultado = await servicio.AutorizarAsync(
            "FINORA_INEXISTENTE",
            solicitud,
            CancellationToken.None,
            "corr-tenant");

        Assert.Equal("DENY", resultado.Decision);
        Assert.Equal("TENANT_NO_CONFIGURADO", resultado.CodigoRegla);
        Assert.Equal("corr-tenant", resultado.CorrelationId);
        Assert.NotNull(repositorioAuditoria.DecisionRegistrada);
        Assert.Null(repositorioAuditoria.DecisionRegistrada.IdOrganizacion);
        Assert.Null(repositorioAuditoria.DecisionRegistrada.IdRecursoProtegido);
        Assert.Null(repositorioAuditoria.DecisionRegistrada.IdOperacion);
        Assert.Equal("FINORA_INEXISTENTE", repositorioAuditoria.DecisionRegistrada.CodigoOrganizacion);
        Assert.Equal("TENANT_NO_CONFIGURADO", repositorioAuditoria.DecisionRegistrada.CodigoRegla);
    }

    [Fact]
    public async Task AutorizarAsync_CuandoRecursoNoExiste_RegistraDenyAuditado()
    {
        var organizacion = new Organizacion
        {
            Id = Guid.NewGuid(),
            Codigo = "FINORA",
            Nombre = "Finora",
            ZonaHoraria = "America/Bogota"
        };
        var repositorioAuditoria = new RepositorioDecisionesAutorizacionFake();
        var servicio = new ServicioAutorizacion(
            new RepositorioOrganizacionesFake(organizacion),
            new RepositorioRecursosProtegidosFake(null),
            new RepositorioOperacionesFake(null),
            new RepositorioReglasAutorizacionFake(Array.Empty<ReglaAutorizacionVigenteDto>()),
            repositorioAuditoria,
            new CalculadorHashSolicitud(),
            new EvaluadorCondicionesFake(false),
            CrearFirmadorDePrueba());
        var solicitud = new SolicitudAutorizacion
        {
            CodigoRecurso = "TRANSFERENCIA_INEXISTENTE",
            CodigoOperacion = "APROBAR"
        };

        var resultado = await servicio.AutorizarAsync(
            "FINORA",
            solicitud,
            CancellationToken.None,
            "corr-recurso");

        Assert.Equal("DENY", resultado.Decision);
        Assert.Equal("RECURSO_NO_CONFIGURADO", resultado.CodigoRegla);
        Assert.Equal("corr-recurso", resultado.CorrelationId);
        Assert.NotNull(repositorioAuditoria.DecisionRegistrada);
        Assert.Equal(organizacion.Id, repositorioAuditoria.DecisionRegistrada.IdOrganizacion);
        Assert.Null(repositorioAuditoria.DecisionRegistrada.IdRecursoProtegido);
        Assert.Null(repositorioAuditoria.DecisionRegistrada.IdOperacion);
        Assert.Equal("TRANSFERENCIA_INEXISTENTE", repositorioAuditoria.DecisionRegistrada.CodigoRecursoSolicitado);
        Assert.Equal("RECURSO_NO_CONFIGURADO", repositorioAuditoria.DecisionRegistrada.CodigoRegla);
    }

    [Fact]
    public async Task AutorizarAsync_CuandoOperacionNoExisteEnRecursoProtegido_RegistraDenyAuditado()
    {
        var organizacion = new Organizacion
        {
            Id = Guid.NewGuid(),
            Codigo = "FINORA",
            Nombre = "Finora",
            ZonaHoraria = "America/Bogota"
        };
        var recursoProtegido = new RecursoProtegido
        {
            Id = Guid.NewGuid(),
            IdOrganizacion = organizacion.Id,
            Codigo = "TRANSFERENCIA",
            Nombre = "Transferencia"
        };
        var repositorioOperaciones = new RepositorioOperacionesFake(null);
        var repositorioAuditoria = new RepositorioDecisionesAutorizacionFake();
        var servicio = new ServicioAutorizacion(
            new RepositorioOrganizacionesFake(organizacion),
            new RepositorioRecursosProtegidosFake(recursoProtegido),
            repositorioOperaciones,
            new RepositorioReglasAutorizacionFake(Array.Empty<ReglaAutorizacionVigenteDto>()),
            repositorioAuditoria,
            new CalculadorHashSolicitud(),
            new EvaluadorCondicionesFake(false),
            CrearFirmadorDePrueba());
        var solicitud = new SolicitudAutorizacion
        {
            CodigoRecurso = "TRANSFERENCIA",
            CodigoOperacion = "APROBAR"
        };

        var resultado = await servicio.AutorizarAsync(
            "FINORA",
            solicitud,
            CancellationToken.None,
            "corr-operacion");

        Assert.Equal("DENY", resultado.Decision);
        Assert.Equal("OPERACION_NO_CONFIGURADA", resultado.CodigoRegla);
        Assert.Equal("corr-operacion", resultado.CorrelationId);
        Assert.Equal(recursoProtegido.Id, repositorioOperaciones.IdRecursoProtegidoConsultado);
        Assert.Equal("APROBAR", repositorioOperaciones.CodigoConsultado);
        Assert.NotNull(repositorioAuditoria.DecisionRegistrada);
        Assert.Equal(organizacion.Id, repositorioAuditoria.DecisionRegistrada.IdOrganizacion);
        Assert.Equal(recursoProtegido.Id, repositorioAuditoria.DecisionRegistrada.IdRecursoProtegido);
        Assert.Null(repositorioAuditoria.DecisionRegistrada.IdOperacion);
        Assert.Equal("OPERACION_NO_CONFIGURADA", repositorioAuditoria.DecisionRegistrada.CodigoRegla);
    }

    private sealed class RepositorioOrganizacionesFake : IRepositorioOrganizaciones
    {
        private readonly Organizacion? _organizacion;

        public RepositorioOrganizacionesFake(Organizacion? organizacion)
        {
            _organizacion = organizacion;
        }

        public Task<Organizacion?> ObtenerPorOrganizacionYCodigoAsync(
            string codigo,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_organizacion);
        }
    }

    private sealed class RepositorioRecursosProtegidosFake : IRepositorioRecursosProtegidos
    {
        private readonly RecursoProtegido? _recursoProtegido;

        public RepositorioRecursosProtegidosFake(RecursoProtegido? recursoProtegido)
        {
            _recursoProtegido = recursoProtegido;
        }

        public Task<RecursoProtegido?> ObtenerPorOrganizacionYCodigoAsync(
            Guid idOrganizacion,
            string codigo,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_recursoProtegido);
        }
    }

    private sealed class RepositorioOperacionesFake : IRepositorioOperaciones
    {
        private readonly Operacion? _operacion;

        public RepositorioOperacionesFake(Operacion? operacion)
        {
            _operacion = operacion;
        }

        public Guid? IdRecursoProtegidoConsultado { get; private set; }

        public string? CodigoConsultado { get; private set; }

        public Task<Operacion?> ObtenerPorRecursoYCodigoAsync(
            Guid idRecursoProtegido,
            string codigo,
            CancellationToken cancellationToken)
        {
            IdRecursoProtegidoConsultado = idRecursoProtegido;
            CodigoConsultado = codigo;

            return Task.FromResult(_operacion);
        }
    }

    private sealed class RepositorioReglasAutorizacionFake : IRepositorioReglasAutorizacion
    {
        private readonly IReadOnlyCollection<ReglaAutorizacionVigenteDto> _reglasVigentes;

        public RepositorioReglasAutorizacionFake(
            IReadOnlyCollection<ReglaAutorizacionVigenteDto> reglasVigentes)
        {
            _reglasVigentes = reglasVigentes;
        }

        public Guid? IdOperacionConsultada { get; private set; }

        public DateTimeOffset? FechaEvaluacionConsultada { get; private set; }

        public Task<IReadOnlyCollection<ReglaAutorizacionVigenteDto>> ObtenerVigentesPorOperacionAsync(
            Guid idOperacion,
            DateTimeOffset fechaEvaluacion,
            CancellationToken cancellationToken)
        {
            IdOperacionConsultada = idOperacion;
            FechaEvaluacionConsultada = fechaEvaluacion;

            return Task.FromResult(_reglasVigentes);
        }
    }

    private sealed class RepositorioDecisionesAutorizacionFake : IRepositorioDecisionesAutorizacion
    {
        public DecisionAutorizacion? DecisionRegistrada { get; private set; }

        public Task RegistrarAsync(
            DecisionAutorizacion decision,
            CancellationToken cancellationToken)
        {
            DecisionRegistrada = decision;

            return Task.CompletedTask;
        }
    }

    private sealed class EvaluadorCondicionesFake : IEvaluadorCondiciones
    {
        private readonly bool _resultado;

        public EvaluadorCondicionesFake(bool resultado)
        {
            _resultado = resultado;
        }

        public bool Evaluar(
            JsonDocument condiciones,
            SolicitudAutorizacion solicitud)
        {
            return _resultado;
        }
    }
}
