namespace Estacionamiento.Dominio.Vehiculos;

/// <summary>
/// Lo que el empleado necesita saber tras registrar una salida: a quién, cuánto tiempo,
/// y cuánto tiene que cobrar ahora mismo (si es que tiene que cobrar algo).
/// </summary>
public sealed record ResultadoSalida
{
    private ResultadoSalida() { }

    public required Placa Placa { get; init; }

    public required string TipoDeVehiculo { get; init; }

    public required DateTime Entrada { get; init; }

    public required DateTime Salida { get; init; }

    public required int MinutosFacturables { get; init; }

    public required MomentoDeCobro MomentoDeCobro { get; init; }

    /// <summary>Importe que el empleado debe cobrar en este momento. 0 si no procede cobrar ahora.</summary>
    public required decimal ImporteACobrarAhora { get; init; }

    /// <summary>Minutos acumulados en el mes en curso. Sólo aplica a quien liquida a fin de mes.</summary>
    public int? MinutosAcumulados { get; init; }

    /// <summary>Saldo pendiente acumulado en el mes en curso. Sólo aplica a quien liquida a fin de mes.</summary>
    public decimal? SaldoPendiente { get; init; }

    internal static ResultadoSalida SinCobro(Vehiculo vehiculo, DateTime entrada, DateTime salida, int minutos) =>
        new()
        {
            Placa = vehiculo.Placa,
            TipoDeVehiculo = vehiculo.Tipo,
            Entrada = entrada,
            Salida = salida,
            MinutosFacturables = minutos,
            MomentoDeCobro = MomentoDeCobro.Ninguno,
            ImporteACobrarAhora = 0m
        };

    internal static ResultadoSalida CobroInmediato(
        Vehiculo vehiculo, DateTime entrada, DateTime salida, int minutos, decimal importe) =>
        new()
        {
            Placa = vehiculo.Placa,
            TipoDeVehiculo = vehiculo.Tipo,
            Entrada = entrada,
            Salida = salida,
            MinutosFacturables = minutos,
            MomentoDeCobro = MomentoDeCobro.ALaSalida,
            ImporteACobrarAhora = importe
        };

    internal static ResultadoSalida CobroDiferido(
        Vehiculo vehiculo, DateTime entrada, DateTime salida, int minutos,
        int minutosAcumulados, decimal saldoPendiente) =>
        new()
        {
            Placa = vehiculo.Placa,
            TipoDeVehiculo = vehiculo.Tipo,
            Entrada = entrada,
            Salida = salida,
            MinutosFacturables = minutos,
            MomentoDeCobro = MomentoDeCobro.AFinDeMes,
            ImporteACobrarAhora = 0m,
            MinutosAcumulados = minutosAcumulados,
            SaldoPendiente = saldoPendiente
        };
}
