using Estacionamiento.Aplicacion.Abstracciones;

namespace Estacionamiento.Infraestructura.Persistencia;

/// <inheritdoc cref="IUnidadDeTrabajo"/>
public sealed class UnidadDeTrabajo : IUnidadDeTrabajo
{
    private readonly EstacionamientoDbContext _contexto;

    public UnidadDeTrabajo(EstacionamientoDbContext contexto) => _contexto = contexto;

    public Task<int> GuardarCambiosAsync(CancellationToken cancelacion = default) =>
        _contexto.SaveChangesAsync(cancelacion);
}
