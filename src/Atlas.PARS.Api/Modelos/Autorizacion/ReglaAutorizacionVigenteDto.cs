using System.Text.Json;

namespace Atlas.PARS.Api.Modelos.Autorizacion;

public sealed class ReglaAutorizacionVigenteDto
{
    public Guid IdReglaAutorizacion { get; set; }

    public Guid IdVersionRegla { get; set; }

    public string CodigoRegla { get; set; } = string.Empty;

    public string NombreRegla { get; set; } = string.Empty;

    public string NumeroVersion { get; set; } = string.Empty;

    public JsonDocument Condiciones { get; set; } = null!;

    public string DecisionSiCumple { get; set; } = string.Empty;

    public int Prioridad { get; set; }
}
