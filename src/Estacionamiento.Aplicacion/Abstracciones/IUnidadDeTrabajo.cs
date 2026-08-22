namespace Estacionamiento.Aplicacion.Abstracciones;

/// <summary>Confirma en la base de datos los cambios acumulados por un caso de uso.</summary>
public interface IUnidadDeTrabajo
{
    Task<int> GuardarCambiosAsync(CancellationToken cancelacion = default);
}
