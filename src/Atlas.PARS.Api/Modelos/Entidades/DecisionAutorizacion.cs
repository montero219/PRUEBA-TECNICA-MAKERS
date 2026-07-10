using System.Text.Json;

namespace Atlas.PARS.Api.Modelos.Entidades;

public class DecisionAutorizacion
{
    public Guid Id { get; set; }

    public string CodigoOrganizacion { get; set; } = string.Empty;

    public Guid? IdOrganizacion { get; set; }

    public Guid? IdRecursoProtegido { get; set; }

    public Guid? IdOperacion { get; set; }

    public Guid? IdReglaAutorizacion { get; set; }

    public Guid? IdVersionRegla { get; set; }

    public string CodigoRecursoSolicitado { get; set; } = string.Empty;

    public string CodigoOperacionSolicitada { get; set; } = string.Empty;

    public string Decision { get; set; } = string.Empty;

    public string Motivo { get; set; } = string.Empty;

    public string? CodigoRegla { get; set; }

    public string? NumeroVersionRegla { get; set; }

    public JsonDocument Actor { get; set; } = null!;

    public JsonDocument Recurso { get; set; } = null!;

    public JsonDocument Contexto { get; set; } = null!;

    public string AlgoritmoHash { get; set; } = "SHA-256";

    public string SolicitudHash { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public DateTimeOffset FechaDecision { get; set; }

    public string? KeyIdFirma { get; set; }

    public string? AlgoritmoFirma { get; set; }

    public string? Firma { get; set; }

    public Organizacion? Organizacion { get; set; }

    public RecursoProtegido? RecursoProtegido { get; set; }

    public Operacion? Operacion { get; set; }

    public ReglaAutorizacion? ReglaAutorizacion { get; set; }

    public VersionRegla? VersionRegla { get; set; }
}
