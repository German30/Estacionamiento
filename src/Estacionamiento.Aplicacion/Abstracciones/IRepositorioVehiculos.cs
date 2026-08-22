using Estacionamiento.Dominio.Vehiculos;

namespace Estacionamiento.Aplicacion.Abstracciones;

/// <summary>
/// Acceso a los vehículos y sus estancias. La capa de aplicación sólo conoce esta interfaz,
/// de modo que cambiar el manejador de base de datos no la afecta.
/// </summary>
public interface IRepositorioVehiculos
{
    /// <summary>Busca un vehículo por su placa, con sus estancias cargadas.</summary>
    Task<Vehiculo?> ObtenerPorPlacaAsync(Placa placa, CancellationToken cancelacion = default);

    Task<bool> ExisteAsync(Placa placa, CancellationToken cancelacion = default);

    Task AgregarAsync(Vehiculo vehiculo, CancellationToken cancelacion = default);

    /// <summary>Todos los vehículos de un tipo concreto. Las estancias sólo se cargan si se piden:
    /// el informe de pagos no las necesita y traerlas sería trabajo inútil.</summary>
    Task<IReadOnlyList<TVehiculo>> ObtenerPorTipoAsync<TVehiculo>(
        bool incluirEstancias = false, CancellationToken cancelacion = default)
        where TVehiculo : Vehiculo;

    /// <summary>Todos los vehículos, con sus estancias cargadas. Usado por "comienza mes".</summary>
    Task<IReadOnlyList<Vehiculo>> ObtenerTodosAsync(CancellationToken cancelacion = default);
}
