using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Atlas.PARS.Api.Datos;
using Atlas.PARS.Api.Modelos.Autorizacion;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Atlas.PARS.PruebasIntegracion;

public class AutorizacionAuditoriaIntegracionPruebas
{
    private const string IdSecretosUsuarioAtlas = "c9646643-6976-4292-a0d8-190be16faf79";
    private const string KeyIdFirmaPrueba = "it-key";
    private const string ClaveFirmaPrueba = "clave-de-integracion-32-bytes-ok";

    [Fact]
    public async Task PostAuthorize_CuandoDecisionEsPermit_RegistraAuditoriaEnBaseDeDatos()
    {
        var nombreBaseDatos = $"atlas_pars_it_{Guid.NewGuid():N}";
        var cadenaBase = ObtenerCadenaConexionBase();
        var cadenaPrueba = CambiarBaseDatos(cadenaBase, nombreBaseDatos);

        await CrearBaseDatosAsync(cadenaBase, nombreBaseDatos);

        try
        {
            await AplicarMigracionesAsync(cadenaPrueba);

            using var factory = CrearFactory(cadenaPrueba);
            using var cliente = factory.CreateClient();
            var correlationId = $"it-{Guid.NewGuid():N}";
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
                    ["dispositivoConfiable"] = "true",
                    ["nivelRiesgo"] = "BAJO"
                }
            };
            cliente.DefaultRequestHeaders.Add("X-Tenant-Code", "FINORA");
            cliente.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);

            var respuesta = await cliente.PostAsJsonAsync(
                "/authorize",
                solicitud);

            respuesta.EnsureSuccessStatusCode();
            var resultado = await respuesta.Content
                .ReadFromJsonAsync<ResultadoAutorizacion>();

            Assert.NotNull(resultado);
            Assert.Equal("PERMIT", resultado.Decision);
            Assert.Equal("PERMITIR_TRANSFERENCIA_NORMAL", resultado.CodigoRegla);
            Assert.Equal(correlationId, resultado.CorrelationId);
            Assert.Matches("^[a-f0-9]{64}$", resultado.SolicitudHash);
            Assert.Equal(KeyIdFirmaPrueba, resultado.KeyId);
            Assert.False(string.IsNullOrWhiteSpace(resultado.Firma));

            await using var contexto = CrearContexto(cadenaPrueba);
            var auditoria = await contexto.DecisionesAutorizacion
                .AsNoTracking()
                .SingleOrDefaultAsync(decision => decision.Id == resultado.IdDecision);

            Assert.NotNull(auditoria);
            Assert.Equal(KeyIdFirmaPrueba, auditoria.KeyIdFirma);
            Assert.Equal("HMAC-SHA256", auditoria.AlgoritmoFirma);
            Assert.Equal(resultado.Firma, auditoria.Firma);
            Assert.Equal("FINORA", auditoria.CodigoOrganizacion);
            Assert.Equal("TRANSFERENCIA", auditoria.CodigoRecursoSolicitado);
            Assert.Equal("APROBAR", auditoria.CodigoOperacionSolicitada);
            Assert.Equal("PERMIT", auditoria.Decision);
            Assert.Equal("PERMITIR_TRANSFERENCIA_NORMAL", auditoria.CodigoRegla);
            Assert.Equal("1.0.0", auditoria.NumeroVersionRegla);
            Assert.Equal(correlationId, auditoria.CorrelationId);
            Assert.Equal(resultado.SolicitudHash, auditoria.SolicitudHash);
            Assert.Equal("SHA-256", auditoria.AlgoritmoHash);
            Assert.Equal("CLIENTE", auditoria.Actor.RootElement.GetProperty("rol").GetString());
            Assert.Equal("500000", auditoria.Recurso.RootElement.GetProperty("monto").GetString());
            Assert.Equal("BAJO", auditoria.Contexto.RootElement.GetProperty("nivelRiesgo").GetString());
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await EliminarBaseDatosAsync(cadenaBase, nombreBaseDatos);
        }
    }

    private static WebApplicationFactory<Program> CrearFactory(
        string cadenaConexion)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuracion) =>
                {
                    configuracion.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Atlas"] = cadenaConexion,
                        ["FirmaDecisiones:KeyId"] = KeyIdFirmaPrueba,
                        ["FirmaDecisiones:ClaveActivaBase64"] = Convert.ToBase64String(
                            Encoding.UTF8.GetBytes(ClaveFirmaPrueba))
                    });
                });
                builder.ConfigureServices(servicios =>
                {
                    servicios.RemoveAll<DbContextOptions<ContextoAtlas>>();
                    servicios.RemoveAll<ContextoAtlas>();
                    servicios.AddDbContext<ContextoAtlas>(opciones =>
                        opciones.UseNpgsql(cadenaConexion));
                });
            });
    }

    private static ContextoAtlas CrearContexto(
        string cadenaConexion)
    {
        var opciones = new DbContextOptionsBuilder<ContextoAtlas>()
            .UseNpgsql(cadenaConexion)
            .Options;

        return new ContextoAtlas(opciones);
    }

    private static async Task AplicarMigracionesAsync(
        string cadenaConexion)
    {
        await using var contexto = CrearContexto(cadenaConexion);

        await contexto.Database.MigrateAsync();
    }

    private static async Task CrearBaseDatosAsync(
        string cadenaBase,
        string nombreBaseDatos)
    {
        await using var conexion = new NpgsqlConnection(
            CambiarBaseDatos(cadenaBase, "postgres"));
        await conexion.OpenAsync();

        await using var comando = new NpgsqlCommand(
            $"""CREATE DATABASE "{nombreBaseDatos}";""",
            conexion);
        await comando.ExecuteNonQueryAsync();
    }

    private static async Task EliminarBaseDatosAsync(
        string cadenaBase,
        string nombreBaseDatos)
    {
        await using var conexion = new NpgsqlConnection(
            CambiarBaseDatos(cadenaBase, "postgres"));
        await conexion.OpenAsync();

        await using var terminarConexiones = new NpgsqlCommand(
            """
            SELECT pg_terminate_backend(pid)
            FROM pg_stat_activity
            WHERE datname = @nombre_base_datos;
            """,
            conexion);
        terminarConexiones.Parameters.AddWithValue(
            "nombre_base_datos",
            nombreBaseDatos);
        await terminarConexiones.ExecuteNonQueryAsync();

        await using var eliminarBase = new NpgsqlCommand(
            $"""DROP DATABASE IF EXISTS "{nombreBaseDatos}";""",
            conexion);
        await eliminarBase.ExecuteNonQueryAsync();
    }

    private static string CambiarBaseDatos(
        string cadenaConexion,
        string nombreBaseDatos)
    {
        var constructor = new NpgsqlConnectionStringBuilder(cadenaConexion)
        {
            Database = nombreBaseDatos
        };

        return constructor.ConnectionString;
    }

    private static string ObtenerCadenaConexionBase()
    {
        var variableEntorno = Environment.GetEnvironmentVariable(
            "ConnectionStrings__Atlas");

        if (!string.IsNullOrWhiteSpace(variableEntorno))
        {
            return variableEntorno;
        }

        var secretosUsuario = ObtenerCadenaConexionDesdeSecretosUsuario();

        if (!string.IsNullOrWhiteSpace(secretosUsuario))
        {
            return secretosUsuario;
        }

        var rutaAppSettings = Path.Combine(
            ObtenerRaizRepositorio(),
            "src",
            "Atlas.PARS.Api",
            "appsettings.json");
        using var documento = JsonDocument.Parse(
            File.ReadAllText(rutaAppSettings));

        var cadenaAppSettings = documento.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("Atlas")
            .GetString();

        if (!string.IsNullOrWhiteSpace(cadenaAppSettings))
        {
            return cadenaAppSettings;
        }

        throw new InvalidOperationException(
            "No se encontro ConnectionStrings:Atlas. Configure User Secrets o la variable ConnectionStrings__Atlas.");
    }

    private static string? ObtenerCadenaConexionDesdeSecretosUsuario()
    {
        foreach (var ruta in ObtenerRutasSecretosUsuario())
        {
            if (!File.Exists(ruta))
            {
                continue;
            }

            using var documento = JsonDocument.Parse(
                File.ReadAllText(ruta));
            var raiz = documento.RootElement;

            if (raiz.TryGetProperty("ConnectionStrings:Atlas", out var valorPlano))
            {
                return valorPlano.GetString();
            }

            if (raiz.TryGetProperty("ConnectionStrings", out var seccion) &&
                seccion.TryGetProperty("Atlas", out var valorAnidado))
            {
                return valorAnidado.GetString();
            }
        }

        return null;
    }

    private static IEnumerable<string> ObtenerRutasSecretosUsuario()
    {
        var appData = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData);
        var perfilUsuario = Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile);

        if (!string.IsNullOrWhiteSpace(appData))
        {
            yield return Path.Combine(
                appData,
                "Microsoft",
                "UserSecrets",
                IdSecretosUsuarioAtlas,
                "secrets.json");
        }

        if (!string.IsNullOrWhiteSpace(perfilUsuario))
        {
            yield return Path.Combine(
                perfilUsuario,
                ".microsoft",
                "usersecrets",
                IdSecretosUsuarioAtlas,
                "secrets.json");
        }
    }

    private static string ObtenerRaizRepositorio()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (directorio is not null &&
            !File.Exists(Path.Combine(directorio.FullName, "Atlas.PARS.sln")))
        {
            directorio = directorio.Parent;
        }

        return directorio?.FullName
            ?? throw new DirectoryNotFoundException(
                "No se encontro la raiz del repositorio.");
    }
}
