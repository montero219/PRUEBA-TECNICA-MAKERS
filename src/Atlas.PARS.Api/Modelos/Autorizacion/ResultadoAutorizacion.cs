namespace Atlas.PARS.Api.Modelos.Autorizacion;

public class ResultadoAutorizacion
{
    public string Decision { get; set; } = string.Empty;

    public string Motivo { get; set; } = string.Empty;

    public string? CodigoRegla { get; set; }
}
