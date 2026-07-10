using Atlas.PARS.Api.Modelos.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Atlas.PARS.Api.Datos;

public class ContextoAtlas : DbContext
{
    public ContextoAtlas(DbContextOptions<ContextoAtlas> opciones)
        : base(opciones)
    {
    }

    public DbSet<Organizacion> Organizaciones => Set<Organizacion>();

    public DbSet<RecursoProtegido> RecursosProtegidos => Set<RecursoProtegido>();

    public DbSet<Operacion> Operaciones => Set<Operacion>();

    public DbSet<ReglaAutorizacion> ReglasAutorizacion => Set<ReglaAutorizacion>();

    public DbSet<VersionRegla> VersionesRegla => Set<VersionRegla>();

    public DbSet<DecisionAutorizacion> DecisionesAutorizacion => Set<DecisionAutorizacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContextoAtlas).Assembly);
    }
}
