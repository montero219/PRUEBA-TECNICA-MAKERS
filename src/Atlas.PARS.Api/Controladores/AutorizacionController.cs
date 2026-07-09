using Atlas.PARS.Api.Modelos.Autorizacion;
using Atlas.PARS.Api.Servicios.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.PARS.Api.Controladores;

[ApiController]
[Route("authorize")]
public sealed class AutorizacionController : ControllerBase
{
    private readonly IServicioAutorizacion _servicioAutorizacion;

    public AutorizacionController(
        IServicioAutorizacion servicioAutorizacion)
    {
        _servicioAutorizacion = servicioAutorizacion;
    }

    [HttpPost]
    public async Task<ActionResult<ResultadoAutorizacion>> AutorizarAsync(
        [FromHeader(Name = "X-Tenant-Code")] string? codigoOrganizacion,
        [FromBody] SolicitudAutorizacion solicitud,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codigoOrganizacion))
        {
            return BadRequest(new
            {
                codigo = "TENANT_REQUERIDO",
                mensaje = "El header X-Tenant-Code es obligatorio."
            });
        }

        var resultado = await _servicioAutorizacion.AutorizarAsync(
            codigoOrganizacion,
            solicitud,
            cancellationToken);

        return Ok(resultado);
    }
}