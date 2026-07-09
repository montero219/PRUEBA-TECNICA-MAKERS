using Atlas.PARS.Api.Datos;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var cadenaConexionAtlas = builder.Configuration.GetConnectionString("Atlas") ?? string.Empty;

builder.Services.AddDbContext<ContextoAtlas>(opciones =>
    opciones.UseNpgsql(cadenaConexionAtlas));

var app = builder.Build();

app.Run();
