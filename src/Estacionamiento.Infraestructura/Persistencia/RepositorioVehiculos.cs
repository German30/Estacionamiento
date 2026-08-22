using Estacionamiento.Aplicacion.Abstracciones;
using Estacionamiento.Dominio.Vehiculos;
using Microsoft.EntityFrameworkCore;

namespace Estacionamiento.Infraestructura.Persistencia;

/// <inheritdoc cref="IRepositorioVehiculos"/>
public sealed class RepositorioVehiculos : IRepositorioVehiculos
{
    private readonly EstacionamientoDbContext _contexto;

    public RepositorioVehiculos(EstacionamientoDbContext contexto) => _contexto = contexto;

    public Task<Vehiculo?> ObtenerPorPlacaAsync(Placa placa, CancellationToken cancelacion = default) =>
        _contexto.Vehiculos
            .Include(vehiculo => vehiculo.Estancias)
            .SingleOrDefaultAsync(vehiculo => vehiculo.Placa == placa, cancelacion);

    public Task<bool> ExisteAsync(Placa placa, CancellationToken cancelacion = default) =>
        _contexto.Vehiculos.AnyAsync(vehiculo => vehiculo.Placa == placa, cancelacion);

    public async Task AgregarAsync(Vehiculo vehiculo, CancellationToken cancelacion = default) =>
        await _contexto.Vehiculos.AddAsync(vehiculo, cancelacion);

    public async Task<IReadOnlyList<TVehiculo>> ObtenerPorTipoAsync<TVehiculo>(
        bool incluirEstancias = false, CancellationToken cancelacion = default)
        where TVehiculo : Vehiculo
    {
        IQueryable<TVehiculo> consulta = _contexto.Vehiculos.OfType<TVehiculo>();

        if (incluirEstancias)
        {
            consulta = consulta.Include(vehiculo => vehiculo.Estancias);
        }

        return await consulta.ToListAsync(cancelacion);
    }

    public async Task<IReadOnlyList<Vehiculo>> ObtenerTodosAsync(CancellationToken cancelacion = default) =>
        await _contexto.Vehiculos
            .Include(vehiculo => vehiculo.Estancias)
            .ToListAsync(cancelacion);
}
