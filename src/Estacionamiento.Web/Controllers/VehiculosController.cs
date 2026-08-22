using IConsultasEstacionamiento = Estacionamiento.Aplicacion.Consultas.IConsultasEstacionamiento;
using Estacionamiento.Aplicacion.Servicios;
using Estacionamiento.Web.Contratos;
using Microsoft.AspNetCore.Mvc;

namespace Estacionamiento.Web.Controllers;

/// <summary>Padrón de vehículos: consulta, ficha y altas de oficiales y de residentes.</summary>
[ApiController]
[Route("api/vehiculos")]
[Produces("application/json")]
public sealed class VehiculosController : ControllerBase
{
    /// <summary>Nombre de la ruta de la ficha, para que otros controladores puedan apuntar a ella.</summary>
    internal const string RutaDeFicha = "FichaDeVehiculo";

    private readonly IServicioEstacionamiento _servicio;
    private readonly IConsultasEstacionamiento _consultas;

    public VehiculosController(IServicioEstacionamiento servicio, IConsultasEstacionamiento consultas)
    {
        _servicio = servicio;
        _consultas = consultas;
    }

    /// <summary>Lista los vehículos registrados.</summary>
    /// <param name="filtro">Fragmento de placa a buscar. Vacío devuelve todos.</param>
    /// <param name="tipo">Filtra por tipo: <c>Oficial</c>, <c>Residente</c> o <c>NoResidente</c>.</param>
    /// <response code="200">Listado, posiblemente vacío.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<VehiculoEnLista>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VehiculoEnLista>>> Listar(
        [FromQuery] string? filtro,
        [FromQuery] string? tipo,
        CancellationToken cancelacion)
    {
        var vehiculos = await _consultas.ListarVehiculosAsync(filtro, tipo, cancelacion);

        return Ok(vehiculos.Select(vehiculo => vehiculo.AContrato()).ToList());
    }

    /// <summary>Ficha de un vehículo con su historial de estancias.</summary>
    /// <param name="placa">Placa a consultar. Se normaliza igual que al registrarla.</param>
    /// <response code="200">Ficha del vehículo.</response>
    /// <response code="404">Esa placa no está registrada.</response>
    [HttpGet("{placa}", Name = RutaDeFicha)]
    [ProducesResponseType<DetalleDeVehiculo>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DetalleDeVehiculo>> Ficha(string placa, CancellationToken cancelacion)
    {
        var detalle = await _consultas.ObtenerDetalleAsync(placa, cancelacion);

        if (detalle is null)
        {
            return Problem(
                title: "Vehículo no encontrado",
                detail: $"No hay ningún vehículo registrado con la placa \"{placa}\".",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(detalle.AContrato());
    }

    /// <summary>Da de alta un vehículo oficial.</summary>
    /// <remarks>No paga nunca; sus estancias se registran sólo para llevar el control.</remarks>
    /// <response code="201">Vehículo dado de alta.</response>
    /// <response code="400">La placa no es válida.</response>
    /// <response code="409">Esa placa ya está registrada.</response>
    [HttpPost("oficiales")]
    [ProducesResponseType<VehiculoDadoDeAlta>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehiculoDadoDeAlta>> AltaDeOficial(
        PeticionConPlaca peticion, CancellationToken cancelacion)
    {
        var alta = await _servicio.DarDeAltaVehiculoOficialAsync(peticion.Placa, cancelacion);

        return CreatedAtRoute(RutaDeFicha, new { placa = alta.Placa.Valor }, alta.AContrato());
    }

    /// <summary>Da de alta un vehículo de residente.</summary>
    /// <remarks>
    /// Acumula minutos y liquida a fin de mes a MXN$0.05 el minuto, frente a los MXN$0.5 que
    /// paga un no residente al salir.
    /// </remarks>
    /// <response code="201">Vehículo dado de alta.</response>
    /// <response code="400">La placa no es válida.</response>
    /// <response code="409">Esa placa ya está registrada.</response>
    [HttpPost("residentes")]
    [ProducesResponseType<VehiculoDadoDeAlta>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehiculoDadoDeAlta>> AltaDeResidente(
        PeticionConPlaca peticion, CancellationToken cancelacion)
    {
        var alta = await _servicio.DarDeAltaVehiculoDeResidenteAsync(peticion.Placa, cancelacion);

        return CreatedAtRoute(RutaDeFicha, new { placa = alta.Placa.Valor }, alta.AContrato());
    }
}
