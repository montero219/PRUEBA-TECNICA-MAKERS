namespace Atlas.PARS.Api.Modelos.Entidades;

public class Operacion
{
    public Guid Id { get; set; }

    public Guid IdRecursoProtegido { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public RecursoProtegido RecursoProtegido { get; set; } = null!;

    public ICollection<ReglaAutorizacion> ReglasAutorizacion { get; set; } = new List<ReglaAutorizacion>();
}
