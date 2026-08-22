using Estacionamiento.Dominio.Comun;
using Estacionamiento.Dominio.Estancias;

namespace Estacionamiento.Dominio.Vehiculos;

/// <summary>
/// Vehículo no residente: paga a la salida a MXN$0.5 el minuto. No se da de alta, se crea
/// solo la primera vez que entra una placa desconocida.
/// </summary>
public sealed class VehiculoNoResidente : Vehiculo
{
    /// <summary>Valor del discriminador en la base de datos.</summary>
    public const string Discriminador = "NoResidente";

    /// <summary>MXN por minuto estacionado.</summary>
    public const decimal Tarifa = 0.5m;

    private VehiculoNoResidente() { } // Requerido por Entity Framework Core.

    public VehiculoNoResidente(Placa placa, DateTime fechaDeAlta) : base(placa, fechaDeAlta) { }

    public override string Tipo => "No residente";

    public override decimal TarifaPorMinuto => Tarifa;

    public override MomentoDeCobro MomentoDeCobro => MomentoDeCobro.ALaSalida;

    protected override ResultadoSalida AlCerrarEstancia(Estancia estancia)
    {
        var importe = PoliticaDeImporte.Calcular(estancia.MinutosFacturables, TarifaPorMinuto);
        estancia.RegistrarImporte(importe);

        return ResultadoSalida.CobroInmediato(
            this, estancia.Entrada, estancia.Salida!.Value, estancia.MinutosFacturables, importe);
    }
}
