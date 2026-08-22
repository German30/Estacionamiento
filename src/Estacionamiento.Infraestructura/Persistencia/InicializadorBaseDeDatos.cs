using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Estacionamiento.Infraestructura.Persistencia;

/// <summary>Qué hizo falta hacer para dejar la base de datos lista.</summary>
/// <param name="MigracionesAplicadas">Migraciones que se acaban de aplicar; vacío si no había pendientes.</param>
/// <param name="EsquemaCreado">Cierto si el esquema se creó desde el modelo.</param>
public sealed record ResultadoDeInicializacion(
    IReadOnlyList<string> MigracionesAplicadas,
    bool EsquemaCreado)
{
    /// <summary>Cierto si la base ya estaba al día y no hubo que tocar nada.</summary>
    public bool NoHuboCambios => MigracionesAplicadas.Count == 0 && !EsquemaCreado;

    public static ResultadoDeInicializacion SinCambios { get; } =
        new(Array.Empty<string>(), EsquemaCreado: false);
}

/// <summary>Deja la base de datos lista para usarse al arrancar la aplicación.</summary>
public sealed class InicializadorBaseDeDatos
{
    private readonly EstacionamientoDbContext _contexto;
    private readonly OpcionesDePersistencia _opciones;
    private readonly ILogger<InicializadorBaseDeDatos> _registro;

    public InicializadorBaseDeDatos(
        EstacionamientoDbContext contexto,
        OpcionesDePersistencia opciones,
        ILogger<InicializadorBaseDeDatos> registro)
    {
        _contexto = contexto;
        _opciones = opciones;
        _registro = registro;
    }

    public async Task<ResultadoDeInicializacion> InicializarAsync(CancellationToken cancelacion = default)
    {
        switch (_opciones.EstrategiaDeEsquema)
        {
            case EstrategiaDeEsquema.Migraciones:
                var pendientes = (await _contexto.Database.GetPendingMigrationsAsync(cancelacion)).ToList();

                if (pendientes.Count == 0)
                {
                    return ResultadoDeInicializacion.SinCambios;
                }

                _registro.LogInformation(
                    "Aplicando {Cantidad} migración(es) pendiente(s): {Migraciones}",
                    pendientes.Count, string.Join(", ", pendientes));

                await _contexto.Database.MigrateAsync(cancelacion);

                return new ResultadoDeInicializacion(pendientes, EsquemaCreado: false);

            case EstrategiaDeEsquema.CrearSiNoExiste:
                var creado = await _contexto.Database.EnsureCreatedAsync(cancelacion);

                if (creado)
                {
                    _registro.LogInformation("Esquema creado a partir del modelo.");
                }

                return new ResultadoDeInicializacion(Array.Empty<string>(), creado);

            case EstrategiaDeEsquema.Ninguna:
            default:
                _registro.LogInformation("Se omite la puesta al día del esquema por configuración.");
                return ResultadoDeInicializacion.SinCambios;
        }
    }
}
