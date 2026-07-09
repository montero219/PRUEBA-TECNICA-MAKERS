using Atlas.PARS.Api.Modelos.Autorizacion;
using Atlas.PARS.Api.Modelos.Entidades;
using Atlas.PARS.Api.Repositorios.Interfaces;
using Atlas.PARS.Api.Servicios;
using Atlas.PARS.Api.Servicios.Interfaces;
using System.Text.Json;

namespace Atlas.PARS.PruebasUnitarias.Servicios;

public class ServicioAutorizacionPruebas
{
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
            new EvaluadorCondiciones());
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
            new EvaluadorCondicionesFake(false));
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
    public async Task AutorizarAsync_CuandoOperacionNoExisteEnRecursoProtegido_LanzaErrorClaro()
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
        var servicio = new ServicioAutorizacion(
            new RepositorioOrganizacionesFake(organizacion),
            new RepositorioRecursosProtegidosFake(recursoProtegido),
            repositorioOperaciones,
            new RepositorioReglasAutorizacionFake(Array.Empty<ReglaAutorizacionVigenteDto>()),
            new EvaluadorCondicionesFake(false));
        var solicitud = new SolicitudAutorizacion
        {
            CodigoRecurso = "TRANSFERENCIA",
            CodigoOperacion = "APROBAR"
        };

        var excepcion = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            servicio.AutorizarAsync("FINORA", solicitud, CancellationToken.None));

        Assert.Contains("APROBAR", excepcion.Message);
        Assert.Contains("TRANSFERENCIA", excepcion.Message);
        Assert.Equal(recursoProtegido.Id, repositorioOperaciones.IdRecursoProtegidoConsultado);
        Assert.Equal("APROBAR", repositorioOperaciones.CodigoConsultado);
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
