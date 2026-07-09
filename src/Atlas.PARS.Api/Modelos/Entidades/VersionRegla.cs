using System.Text.Json;

namespace Atlas.PARS.Api.Modelos.Entidades;

public class VersionRegla
{
    public Guid Id { get; set; }

    public Guid IdReglaAutorizacion { get; set; }

    public string NumeroVersion { get; set; } = string.Empty;

    public JsonDocument Condiciones { get; set; } = null!;

    public string DecisionSiCumple { get; set; } = string.Empty;

    public int Prioridad { get; set; }

    public DateTimeOffset VigenciaDesde { get; set; }

    public DateTimeOffset? VigenciaHasta { get; set; }

    public DateTimeOffset FechaCreacion { get; set; }

    public ReglaAutorizacion ReglaAutorizacion { get; set; } = null!;
}
