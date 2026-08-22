using IConsultasEstacionamiento = Estacionamiento.Aplicacion.Consultas.IConsultasEstacionamiento;
using Estacionamiento.Aplicacion.Servicios;
using Estacionamiento.Web.Contratos;
using Microsoft.AspNetCore.Mvc;

namespace Estacionamiento.Web.Controllers;

/// <summary>
/// Entradas y salidas. Es lo que se llama cientos de veces al día; todo lo demás de la API
/// se usa una vez al mes.
/// </summary>
[ApiController]
[Route("api/accesos")]
[Produces("application/json")]
public sealed class AccesosController : ControllerBase
{
    private readonly IServicioEstacionamiento _servicio;
    private readonly IConsultasEstacionamiento _consultas;

    public AccesosController(IServicioEstacionamiento servicio, IConsultasEstacionamiento consultas)
    {
        _servicio = servicio;
        _consultas = consultas;
    }

    /// <summary>Registra la entrada de una placa.</summary>
    /// <remarks>
    /// Si la placa no está dada de alta como oficial ni como residente, se crea en este momento
    /// como no residente: el empleado no tiene que dar de alta a nadie para dejarlo entrar.
    /// </remarks>
    /// <response code="201">Entrada registrada.</response>
    /// <response code="400">La placa no es válida.</response>
    /// <response code="409">Esa placa ya tiene una entrada abierta.</response>
    [HttpPost("entradas")]
    [ProducesResponseType<EntradaRegistrada>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EntradaRegistrada>> RegistrarEntrada(
        PeticionConPlaca peticion, CancellationToken cancelacion)
    {
        var registro = await _servicio.RegistrarEntradaAsync(peticion.Placa, cancelacion);

        // 201 con Location a la ficha: quien acaba de meter un coche suele querer verla.
        return CreatedAtRoute(
            VehiculosController.RutaDeFicha,
            new { placa = registro.Placa.Valor },
            registro.AContrato());
    }

    /// <summary>Registra la salida de una placa y devuelve qué debe cobrar el empleado.</summary>
    /// <response code="200">Salida registrada. Revise <c>importeACobrarAhora</c>.</response>
    /// <response code="400">La placa no es válida.</response>
    /// <response code="409">Esa placa no tiene ninguna entrada abierta.</response>
    [HttpPost("salidas")]
    [ProducesResponseType<SalidaRegistrada>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SalidaRegistrada>> RegistrarSalida(
        PeticionConPlaca peticion, CancellationToken cancelacion)
    {
        var salida = await _servicio.RegistrarSalidaAsync(peticion.Placa, cancelacion);

        return Ok(salida.AContrato());
    }

    /// <summary>Vehículos que están dentro ahora mismo, el que más tiempo lleva primero.</summary>
    /// <remarks>
    /// Cuelga de <c>accesos</c> y no de <c>vehiculos</c> a propósito: bajo <c>/api/vehiculos/dentro</c>
    /// chocaría con la ficha de una placa que se llamara literalmente DENTRO, que es una placa válida.
    /// </remarks>
    /// <response code="200">Listado, posiblemente vacío.</response>
    [HttpGet("dentro")]
    [ProducesResponseType<IReadOnlyList<VehiculoEnLista>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VehiculoEnLista>>> ListarDentro(CancellationToken cancelacion)
    {
        var dentro = await _consultas.ListarDentroAsync(cancelacion);

        return Ok(dentro.Select(vehiculo => vehiculo.AContrato()).ToList());
    }
}
