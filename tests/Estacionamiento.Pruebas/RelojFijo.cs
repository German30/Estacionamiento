using Estacionamiento.Dominio.Comun;

namespace Estacionamiento.Pruebas;

/// <summary>Reloj controlado por la prueba: permite simular el paso del tiempo sin esperar.</summary>
internal sealed class RelojFijo : IReloj
{
    public RelojFijo(DateTime inicio) => Ahora = inicio;

    public DateTime Ahora { get; private set; }

    public void Avanzar(TimeSpan cuanto) => Ahora = Ahora.Add(cuanto);

    public void AvanzarMinutos(double minutos) => Avanzar(TimeSpan.FromMinutes(minutos));
}
