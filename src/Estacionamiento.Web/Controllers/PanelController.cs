using IConsultasEstacionamiento = Estacionamiento.Aplicacion.Consultas.IConsultasEstacionamiento;
using Estacionamiento.Web.Contratos;
using Microsoft.AspNetCore.Mvc;

namespace Estacionamiento.Web.Controllers;

/// <summary>Estado del estacionamiento de un vistazo.</summary>
[ApiController]
[Route("api/panel")]
[Produces("application/json")]
public sealed class PanelController : ControllerBase
{
    private readonly IConsultasEstacionamiento _consultas;

    public PanelController(IConsultasEstacionamiento consultas)
    {
        _consultas = consultas;
    }

    /// <summary>Cuántos hay dentro, cuánto se lleva cobrado hoy y qué deben los residentes.</summary>
    /// <response code="200">Panel de control.</response>
    [HttpGet]
    [ProducesResponseType<PanelDeControl>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PanelDeControl>> Obtener(CancellationToken cancelacion)
    {
        var panel = await _consultas.ObtenerPanelAsync(cancelacion);

        return Ok(panel.AContrato());
    }
}
