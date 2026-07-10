using Atlas.PARS.Api.Configuracion;
using Atlas.PARS.Api.Datos;
using Microsoft.EntityFrameworkCore;
using Atlas.PARS.Api.Repositorios;
using Atlas.PARS.Api.Repositorios.Interfaces;
using Atlas.PARS.Api.Servicios;
using Atlas.PARS.Api.Servicios.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var cadenaConexionAtlas = builder.Configuration.GetConnectionString("Atlas") ?? string.Empty;

builder.Services.AddDbContext<ContextoAtlas>(opciones =>
    opciones.UseNpgsql(cadenaConexionAtlas));
builder.Services.Configure<OpcionesFirmaDecisiones>(
    builder.Configuration.GetSection("FirmaDecisiones"));
builder.Services.AddScoped<IRepositorioOrganizaciones, RepositorioOrganizaciones>();
builder.Services.AddScoped<IRepositorioRecursosProtegidos, RepositorioRecursosProtegidos>();
builder.Services.AddScoped<IRepositorioOperaciones, RepositorioOperaciones>();
builder.Services.AddScoped<IRepositorioReglasAutorizacion, RepositorioReglasAutorizacion>();
builder.Services.AddScoped<IRepositorioDecisionesAutorizacion, RepositorioDecisionesAutorizacion>();
builder.Services.AddSingleton<ICalculadorHashSolicitud, CalculadorHashSolicitud>();
builder.Services.AddSingleton<IFirmadorDecisionesAutorizacion, FirmadorDecisionesAutorizacionHmac>();
builder.Services.AddScoped<IEvaluadorCondiciones, EvaluadorCondiciones>();
builder.Services.AddScoped<IServicioAutorizacion, ServicioAutorizacion>();
builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();
app.Run();

public partial class Program
{
}
