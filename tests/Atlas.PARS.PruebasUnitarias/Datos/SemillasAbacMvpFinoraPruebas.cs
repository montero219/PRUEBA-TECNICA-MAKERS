using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Atlas.PARS.PruebasUnitarias.Datos;

public class SemillasAbacMvpFinoraPruebas
{
    [Fact]
    public void Migraciones_CuandoSiembranReglasMvp_UsanMontoSensibleSinHuecosDeMonto()
    {
        var raizRepositorio = ObtenerRaizRepositorio();
        var catalogo = File.ReadAllText(Path.Combine(
            raizRepositorio,
            "src",
            "Atlas.PARS.Api",
            "Datos",
            "Migraciones",
            "20260709120000_SembrarCatalogoAbacMvpFinora.cs"));
        var versiones = File.ReadAllText(Path.Combine(
            raizRepositorio,
            "src",
            "Atlas.PARS.Api",
            "Datos",
            "Migraciones",
            "20260709123000_SembrarVersionesReglasAbacMvpFinora.cs"));

        Assert.Contains("MONTO_SENSIBLE", catalogo);
        Assert.Contains("MONTO_SENSIBLE", versiones);
        Assert.DoesNotContain("RETAR_ALTO_MONTO", catalogo);
        Assert.DoesNotContain("RETAR_ALTO_MONTO", versiones);
        Assert.DoesNotContain("RETAR_CONTEXTO_SOSPECHOSO", catalogo);
        Assert.DoesNotContain("RETAR_CONTEXTO_SOSPECHOSO", versiones);
        Assert.Equal(2, Regex.Matches(versiones, "\"valor\": 1000001").Count);
        AssertPrioridad(versiones, "MONTO_SENSIBLE", 40);
        AssertPrioridad(versiones, "CONTEXTO_SOSPECHOSO", 30);
    }

    private static string ObtenerRaizRepositorio(
        [CallerFilePath] string archivoFuente = "")
    {
        var directorio = new FileInfo(archivoFuente).Directory;

        while (directorio is not null &&
            !File.Exists(Path.Combine(directorio.FullName, "Atlas.PARS.sln")))
        {
            directorio = directorio.Parent;
        }

        return directorio?.FullName
            ?? throw new DirectoryNotFoundException("No se encontro la raiz del repositorio.");
    }

    private static void AssertPrioridad(
        string migracion,
        string codigoRegla,
        int prioridadEsperada)
    {
        var patron = $@"'{codigoRegla}',\s*'[^']+',\s*'CHALLENGE',\s*{prioridadEsperada},";

        Assert.Matches(patron, migracion);
    }
}
