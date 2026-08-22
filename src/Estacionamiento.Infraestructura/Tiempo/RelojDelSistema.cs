using Estacionamiento.Dominio.Comun;

namespace Estacionamiento.Infraestructura.Tiempo;

/// <inheritdoc cref="IReloj"/>
public sealed class RelojDelSistema : IReloj
{
    /// <summary>Hora local de la máquina donde corre la aplicación, que es la del estacionamiento.</summary>
    public DateTime Ahora => DateTime.Now;
}
