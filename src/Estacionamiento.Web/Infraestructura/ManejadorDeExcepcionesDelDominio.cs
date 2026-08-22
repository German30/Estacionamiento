using Estacionamiento.Dominio.Excepciones;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Estacionamiento.Web.Infraestructura;

/// <summary>
/// Convierte los errores de regla de negocio en respuestas <c>application/problem+json</c>.
/// </summary>
/// <remarks>
/// Sin esto, romper una regla del dominio saldría por el manejador genérico como un 500 con
/// una traza: el cliente no podría distinguir "esa placa ya está dentro" —que se arregla
/// tecleando otra cosa— de "la base de datos no responde", que no se arregla desde el cliente.
/// Un 500 además invita a reintentar, y aquí reintentar nunca va a funcionar.
/// </remarks>
public sealed class ManejadorDeExcepcionesDelDominio : IExceptionHandler
{
    private readonly IProblemDetailsService _problemas;
    private readonly ILogger<ManejadorDeExcepcionesDelDominio> _registro;

    public ManejadorDeExcepcionesDelDominio(
        IProblemDetailsService problemas, ILogger<ManejadorDeExcepcionesDelDominio> registro)
    {
        _problemas = problemas;
        _registro = registro;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext contexto, Exception excepcion, CancellationToken cancelacion)
    {
        if (excepcion is not ExcepcionDelDominio excepcionDelDominio)
        {
            // No es cosa nuestra: que siga hasta el manejador genérico, que sí registra la traza.
            return false;
        }

        var (estado, titulo) = Clasificar(excepcionDelDominio);

        // Nivel de aviso, no de error: el sistema hizo su trabajo rechazando la operación.
        _registro.LogWarning(
            "Regla de negocio rechazó {Metodo} {Ruta}: {Mensaje}",
            contexto.Request.Method, contexto.Request.Path, excepcionDelDominio.Message);

        contexto.Response.StatusCode = estado;

        return await _problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            Exception = excepcionDelDominio,
            ProblemDetails = new ProblemDetails
            {
                Status = estado,
                Title = titulo,
                Detail = excepcionDelDominio.Message,
                Type = $"https://estacionamiento.local/errores/{excepcionDelDominio.GetType().Name}"
            }
        });
    }

    /// <summary>
    /// 400 cuando lo que llegó no es una placa; 409 cuando la placa es buena pero el
    /// estacionamiento no está en el estado que la operación necesita.
    /// </summary>
    private static (int Estado, string Titulo) Clasificar(ExcepcionDelDominio excepcion) => excepcion switch
    {
        PlacaInvalidaException =>
            (StatusCodes.Status400BadRequest, "Placa inválida"),

        VehiculoYaRegistradoException =>
            (StatusCodes.Status409Conflict, "La placa ya está registrada"),

        VehiculoYaEstacionadoException =>
            (StatusCodes.Status409Conflict, "El vehículo ya está dentro"),

        VehiculoNoEstacionadoException =>
            (StatusCodes.Status409Conflict, "El vehículo no está dentro"),

        SalidaAnteriorALaEntradaException =>
            (StatusCodes.Status409Conflict, "La salida es anterior a la entrada"),

        _ => (StatusCodes.Status409Conflict, "Operación rechazada")
    };
}
