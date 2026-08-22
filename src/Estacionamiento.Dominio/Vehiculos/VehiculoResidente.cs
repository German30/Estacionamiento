using Estacionamiento.Dominio.Comun;
using Estacionamiento.Dominio.Estancias;

namespace Estacionamiento.Dominio.Vehiculos;

/// <summary>
/// Vehículo de residente: paga a fin de mes a MXN$0.05 el minuto. Cada salida suma la
/// duración de la estancia al tiempo acumulado, que se pone a cero al comenzar mes.
/// </summary>
public sealed class VehiculoResidente : Vehiculo
{
    /// <summary>Valor del discriminador en la base de datos.</summary>
    public const string Discriminador = "Residente";

    /// <summary>MXN por minuto estacionado.</summary>
    public const decimal Tarifa = 0.05m;

    private VehiculoResidente() { } // Requerido por Entity Framework Core.

    public VehiculoResidente(Placa placa, DateTime fechaDeAlta) : base(placa, fechaDeAlta) { }

    public override string Tipo => "Residente";

    public override decimal TarifaPorMinuto => Tarifa;

    public override MomentoDeCobro MomentoDeCobro => MomentoDeCobro.AFinDeMes;

    /// <summary>Minutos estacionados en el mes en curso.</summary>
    public int MinutosAcumulados { get; private set; }

    /// <summary>Importe que deberá liquidar a fin de mes por los minutos acumulados.</summary>
    public decimal SaldoPendiente => PoliticaDeImporte.Calcular(MinutosAcumulados, TarifaPorMinuto);

    protected override ResultadoSalida AlCerrarEstancia(Estancia estancia)
    {
        MinutosAcumulados += estancia.MinutosFacturables;

        return ResultadoSalida.CobroDiferido(
            this, estancia.Entrada, estancia.Salida!.Value, estancia.MinutosFacturables,
            MinutosAcumulados, SaldoPendiente);
    }

    public override void ComenzarMes() => MinutosAcumulados = 0;
}
