namespace Atlas.PARS.Api.Modelos.Entidades;

public class RecursoProtegido
{
    public Guid Id { get; set; }

    public Guid IdOrganizacion { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public Organizacion Organizacion { get; set; } = null!;

    public ICollection<Operacion> Operaciones { get; set; } = new List<Operacion>();
}
