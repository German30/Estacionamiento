namespace Estacionamiento.Dominio.Comun;

/// <summary>
/// Fuente de tiempo de la aplicación. Se abstrae para poder fijar el reloj en las pruebas.
/// </summary>
public interface IReloj
{
    /// <summary>Momento actual, en hora local del estacionamiento.</summary>
    DateTime Ahora { get; }
}
