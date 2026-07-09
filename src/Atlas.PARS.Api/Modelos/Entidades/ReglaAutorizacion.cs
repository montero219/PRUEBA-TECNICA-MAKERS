namespace Atlas.PARS.Api.Modelos.Entidades;

public class ReglaAutorizacion
{
    public Guid Id { get; set; }

    public Guid IdOperacion { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public Operacion Operacion { get; set; } = null!;

    public ICollection<VersionRegla> VersionesRegla { get; set; } = new List<VersionRegla>();
}
