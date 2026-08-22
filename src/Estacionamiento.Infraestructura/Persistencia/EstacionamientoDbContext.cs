using Estacionamiento.Dominio.Estancias;
using Estacionamiento.Dominio.Vehiculos;
using Microsoft.EntityFrameworkCore;

namespace Estacionamiento.Infraestructura.Persistencia;

/// <summary>
/// Contexto de Entity Framework Core. El mapeo vive en clases
/// <see cref="Microsoft.EntityFrameworkCore.IEntityTypeConfiguration{TEntity}"/> separadas,
/// una por agregado, para que este archivo no crezca con el modelo.
/// </summary>
public sealed class EstacionamientoDbContext : DbContext
{
    public EstacionamientoDbContext(DbContextOptions<EstacionamientoDbContext> opciones)
        : base(opciones)
    {
    }

    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();

    public DbSet<Estancia> Estancias => Set<Estancia>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.ApplyConfigurationsFromAssembly(typeof(EstacionamientoDbContext).Assembly);
        base.OnModelCreating(modelo);
    }
}
