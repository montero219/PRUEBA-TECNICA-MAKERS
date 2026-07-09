namespace Atlas.PARS.Api.Modelos.Entidades;

public class Organizacion
{
    public Guid Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string ZonaHoraria { get; set; } = string.Empty;

    public ICollection<RecursoProtegido> RecursosProtegidos { get; set; } = new List<RecursoProtegido>();
}
