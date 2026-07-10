namespace Atlas.PARS.Api.Modelos.Autorizacion;

public class ResultadoAutorizacion
{
    public Guid IdDecision { get; set; }

    public string Decision { get; set; } = string.Empty;

    public string Motivo { get; set; } = string.Empty;

    public string? CodigoRegla { get; set; }

    public string CorrelationId { get; set; } = string.Empty;

    public string SolicitudHash { get; set; } = string.Empty;

    public string KeyId { get; set; } = string.Empty;

    public string Firma { get; set; } = string.Empty;
}
