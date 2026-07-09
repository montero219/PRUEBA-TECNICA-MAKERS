namespace Atlas.PARS.Api.Modelos.Autorizacion;

public class SolicitudAutorizacion
{
    public string CodigoRecurso { get; set; } = string.Empty;

    public string CodigoOperacion { get; set; } = string.Empty;

    public Dictionary<string, string> AtributosActor { get; set; } = new();

    public Dictionary<string, string> AtributosRecurso { get; set; } = new();

    public Dictionary<string, string> Contexto { get; set; } = new();
}
