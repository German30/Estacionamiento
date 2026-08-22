using System.Text;
using Estacionamiento.Aplicacion.Servicios;
using Estacionamiento.Web.Contratos;
using Microsoft.AspNetCore.Mvc;

namespace Estacionamiento.Web.Controllers;

/// <summary>Operaciones de fin de mes: el informe de pagos de residentes y el cierre.</summary>
[ApiController]
[Route("api/cierre")]
[Produces("application/json")]
public sealed class CierreController : ControllerBase
{
    // Con marca de orden de bytes, para que los acentos se vean bien también en editores
    // que no detectan UTF-8 por su cuenta.
    private static readonly UTF8Encoding CodificacionDelInforme = new(encoderShouldEmitUTF8Identifier: true);

    private readonly IServicioEstacionamiento _servicio;

    public CierreController(IServicioEstacionamiento servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Calcula el informe de pagos de residentes sin escribir nada.</summary>
    /// <remarks>
    /// Es de sólo lectura y se puede pedir las veces que haga falta: no toca los contadores,
    /// eso sólo lo hace el cierre de mes.
    /// </remarks>
    /// <response code="200">Informe calculado, con sus líneas y el texto ya formateado.</response>
    [HttpGet("informe")]
    [ProducesResponseType<InformeDePagos>(StatusCodes.Status200OK)]
    public async Task<ActionResult<InformeDePagos>> Informe(CancellationToken cancelacion)
    {
        var informe = await _servicio.PrepararInformeDePagosDeResidentesAsync(cancelacion);

        return Ok(informe.AContrato());
    }

    /// <summary>Descarga el informe como archivo de texto.</summary>
    /// <remarks>Mismo contenido que <c>GET /api/cierre/informe</c>, servido como adjunto en UTF-8.</remarks>
    /// <response code="200">Archivo de texto plano.</response>
    [HttpGet("informe/descargar")]
    [Produces("text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Descargar(CancellationToken cancelacion)
    {
        var informe = await _servicio.PrepararInformeDePagosDeResidentesAsync(cancelacion);

        var bytes = CodificacionDelInforme.GetPreamble()
            .Concat(CodificacionDelInforme.GetBytes(informe.Contenido))
            .ToArray();

        return File(bytes, "text/plain; charset=utf-8", $"pagos-{DateTime.Now:yyyy-MM}.txt");
    }

    /// <summary>Escribe el informe en el disco del servidor.</summary>
    /// <remarks>
    /// En el contenedor, <c>/informes</c> está montado contra <c>./informes</c> del anfitrión,
    /// así que <c>/informes/pagos-agosto.txt</c> aparece ahí fuera al terminar.
    /// </remarks>
    /// <response code="201">Informe escrito. <c>rutaDelArchivo</c> trae la ruta absoluta.</response>
    /// <response code="400">La ruta está vacía o no se puede escribir en ella.</response>
    [HttpPost("informe")]
    [ProducesResponseType<InformeDePagos>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InformeDePagos>> Guardar(
        PeticionDeInformeEnDisco peticion, CancellationToken cancelacion)
    {
        try
        {
            var informe = await _servicio.GenerarInformeDePagosDeResidentesAsync(peticion.Ruta, cancelacion);

            // 201 sin Location: lo creado es un archivo en el disco del servidor, no un recurso
            // que se pueda pedir por HTTP. La ruta va en el cuerpo, en "rutaDelArchivo".
            return StatusCode(StatusCodes.Status201Created, informe.AContrato());
        }
        catch (Exception excepcion) when (excepcion is IOException or UnauthorizedAccessException
                                              or ArgumentException or NotSupportedException)
        {
            // La ruta la elige quien llama, así que un fallo al escribir es culpa de la petición,
            // no del servidor: 400 con el motivo, no un 500 con la traza.
            return Problem(
                title: "No se pudo escribir el informe",
                detail: $"{excepcion.Message} (ruta solicitada: \"{peticion.Ruta}\").",
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>Comienza un mes nuevo. Es irreversible.</summary>
    /// <remarks>
    /// Borra las estancias de los vehículos oficiales y pone a cero el tiempo acumulado de los
    /// residentes. Los vehículos que estén dentro conservan su entrada abierta y saldrán con
    /// normalidad. Para confirmar hay que enviar <c>confirmacion: "COMENZAR"</c>: acertar la
    /// ruta no basta para algo que no se puede deshacer.
    /// Descargue antes el informe de pagos, o lo que se cobra este mes se pierde.
    /// </remarks>
    /// <response code="200">Mes iniciado. El resumen dice qué se reinició.</response>
    /// <response code="400">Falta la confirmación. No se modificó nada.</response>
    [HttpPost("mes")]
    [ProducesResponseType<ResumenDeComienzoDeMes>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResumenDeComienzoDeMes>> ComenzarMes(
        PeticionDeCierreDeMes peticion, CancellationToken cancelacion)
    {
        if (!peticion.EstaConfirmado)
        {
            return Problem(
                title: "No se comenzó el mes",
                detail: $"Para confirmar, envíe confirmacion: \"{PeticionDeCierreDeMes.PalabraDeConfirmacion}\". " +
                        "No se modificó nada.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var resumen = await _servicio.ComenzarMesAsync(cancelacion);

        return Ok(resumen.AContrato());
    }
}
