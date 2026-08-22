using Estacionamiento.Dominio.Estancias;

namespace Estacionamiento.Dominio.Vehiculos;

/// <summary>
/// Vehículo oficial: no paga, pero se guardan sus estancias para llevar el control.
/// Al comenzar mes esas estancias se eliminan.
/// </summary>
public sealed class VehiculoOficial : Vehiculo
{
    /// <summary>Valor del discriminador en la base de datos.</summary>
    public const string Discriminador = "Oficial";

    private VehiculoOficial() { } // Requerido por Entity Framework Core.

    public VehiculoOficial(Placa placa, DateTime fechaDeAlta) : base(placa, fechaDeAlta) { }

    public override string Tipo => "Oficial";

    public override decimal TarifaPorMinuto => 0m;

    public override MomentoDeCobro MomentoDeCobro => MomentoDeCobro.Ninguno;

    protected override ResultadoSalida AlCerrarEstancia(Estancia estancia)
    {
        // La estancia ya quedó asociada al vehículo al abrirla; aquí sólo se deja constancia
        // de que no generó importe.
        estancia.RegistrarImporte(0m);

        return ResultadoSalida.SinCobro(
            this, estancia.Entrada, estancia.Salida!.Value, estancia.MinutosFacturables);
    }

    public override void ComenzarMes() => EliminarEstanciasCerradas();
}
