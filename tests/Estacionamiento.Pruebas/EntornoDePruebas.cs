using Estacionamiento.Aplicacion.Abstracciones;
using Estacionamiento.Aplicacion.Servicios;
using Estacionamiento.Infraestructura.Persistencia;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Estacionamiento.Pruebas;

/// <summary>
/// Base de datos SQLite en memoria con el esquema real y el mismo cableado que usa el menú:
/// un contexto nuevo por operación. Así las pruebas comprueban también el mapeo de Entity
/// Framework Core, no sólo la lógica en memoria.
/// </summary>
internal sealed class EntornoDePruebas : IDisposable
{
    private readonly SqliteConnection _conexion;
    private readonly DbContextOptions<EstacionamientoDbContext> _opciones;

    public EntornoDePruebas(DateTime? inicio = null)
    {
        // "Filename=:memory:" vive mientras la conexión siga abierta.
        _conexion = new SqliteConnection("Filename=:memory:");
        _conexion.Open();

        _opciones = new DbContextOptionsBuilder<EstacionamientoDbContext>()
            .UseSqlite(_conexion)
            .Options;

        using var contexto = NuevoContexto();
        contexto.Database.EnsureCreated();

        Reloj = new RelojFijo(inicio ?? new DateTime(2026, 8, 1, 8, 0, 0));
    }

    public RelojFijo Reloj { get; }

    public AlmacenDeInformesEnMemoria Almacen { get; } = new();

    public EstacionamientoDbContext NuevoContexto() => new(_opciones);

    /// <summary>Ejecuta un caso de uso con su propio contexto, como hace cada opción del menú.</summary>
    public async Task<T> EjecutarAsync<T>(Func<IServicioEstacionamiento, Task<T>> operacion)
    {
        using var contexto = NuevoContexto();

        var servicio = new ServicioEstacionamiento(
            new RepositorioVehiculos(contexto),
            new UnidadDeTrabajo(contexto),
            Almacen,
            Reloj);

        return await operacion(servicio);
    }

    /// <summary>Siembra datos de demostración con su propio contexto.</summary>
    public async Task<ResumenDeSiembra> SembrarAsync(int cantidadDeVehiculos)
    {
        using var contexto = NuevoContexto();
        return await NuevoSembrador(contexto).SembrarAsync(cantidadDeVehiculos);
    }

    public SembradorDeDatos NuevoSembrador(EstacionamientoDbContext contexto) =>
        new(contexto, Reloj, NullLogger<SembradorDeDatos>.Instance);

    public void Dispose() => _conexion.Dispose();
}

/// <summary>Guarda el informe en memoria para poder inspeccionarlo sin tocar el disco.</summary>
internal sealed class AlmacenDeInformesEnMemoria : IAlmacenDeInformes
{
    public string? Contenido { get; private set; }

    public Task<string> GuardarAsync(string ruta, string contenido, CancellationToken cancelacion = default)
    {
        Contenido = contenido;
        return Task.FromResult(Path.GetFullPath(ruta));
    }
}
