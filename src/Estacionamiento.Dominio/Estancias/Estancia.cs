using Estacionamiento.Dominio.Comun;
using Estacionamiento.Dominio.Excepciones;
using Estacionamiento.Dominio.Vehiculos;

namespace Estacionamiento.Dominio.Estancias;

/// <summary>
/// Una estancia es un par (hora de entrada, hora de salida) de un vehículo concreto.
/// Mientras la salida no se registra la estancia está abierta y el vehículo está dentro.
/// </summary>
public sealed class Estancia
{
    private Estancia() { } // Requerido por Entity Framework Core.

    private Estancia(Vehiculo vehiculo, DateTime entrada)
    {
        Vehiculo = vehiculo;
        Entrada = entrada;
    }

    public int Id { get; private set; }

    public int VehiculoId { get; private set; }

    public Vehiculo Vehiculo { get; private set; } = null!;

    public DateTime Entrada { get; private set; }

    public DateTime? Salida { get; private set; }

    /// <summary>Importe efectivamente cobrado al cerrar la estancia.
    /// Es 0 en oficiales y residentes (estos pagan a fin de mes, no por estancia).</summary>
    public decimal ImporteCobrado { get; private set; }

    public bool EstaAbierta => Salida is null;

    public TimeSpan? Duracion => Salida is null ? null : Salida.Value - Entrada;

    /// <summary>Minutos que se facturan por esta estancia. 0 mientras siga abierta.</summary>
    public int MinutosFacturables =>
        Duracion is { } duracion ? PoliticaDeTiempo.AMinutosFacturables(duracion) : 0;

    internal static Estancia Abrir(Vehiculo vehiculo, DateTime entrada) => new(vehiculo, entrada);

    internal void Cerrar(DateTime salida)
    {
        if (salida < Entrada)
        {
            throw new SalidaAnteriorALaEntradaException(Entrada, salida);
        }

        Salida = salida;
    }

    internal void RegistrarImporte(decimal importe) => ImporteCobrado = importe;
}
