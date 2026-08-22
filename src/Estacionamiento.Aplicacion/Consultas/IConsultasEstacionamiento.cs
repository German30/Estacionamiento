using Estacionamiento.Dominio.Vehiculos;

namespace Estacionamiento.Aplicacion.Consultas;

/// <summary>
/// Lado de lectura. Los seis casos de uso del enunciado son órdenes que cambian el estado; esto
/// es lo que hace falta para <em>mostrarlo</em>: un panel, listados y el detalle de un vehículo.
/// Va aparte de <c>IServicioEstacionamiento</c> para que consultar no pueda modificar nada.
/// </summary>
public interface IConsultasEstacionamiento
{
    Task<PanelDeControl> ObtenerPanelAsync(CancellationToken cancelacion = default);

    /// <summary>Vehículos que están dentro del estacionamiento ahora mismo, el que más tiempo lleva primero.</summary>
    Task<IReadOnlyList<VehiculoEnLista>> ListarDentroAsync(CancellationToken cancelacion = default);

    /// <summary>Vehículos registrados. <paramref name="filtro"/> busca por fragmento de placa.</summary>
    Task<IReadOnlyList<VehiculoEnLista>> ListarVehiculosAsync(
        string? filtro = null, string? tipo = null, CancellationToken cancelacion = default);

    Task<DetalleDeVehiculo?> ObtenerDetalleAsync(string placa, CancellationToken cancelacion = default);

    /// <summary>Residentes con su tiempo acumulado, tal como saldrían en el informe de pagos.</summary>
    Task<IReadOnlyList<VehiculoEnLista>> ListarResidentesAsync(CancellationToken cancelacion = default);
}

/// <summary>Estado del estacionamiento de un vistazo.</summary>
public sealed record PanelDeControl(
    int VehiculosDentro,
    int TotalDeVehiculos,
    int Oficiales,
    int Residentes,
    int NoResidentes,
    int MinutosAcumuladosDeResidentes,
    decimal SaldoPendienteDeResidentes,
    int SalidasDeHoy,
    decimal CobradoHoy);

/// <summary>Una fila de cualquiera de los listados de vehículos.</summary>
public sealed record VehiculoEnLista(
    Placa Placa,
    string Tipo,
    MomentoDeCobro MomentoDeCobro,
    bool EstaDentro,
    DateTime? DentroDesde,
    int MinutosDentro,
    int? MinutosAcumulados,
    decimal? SaldoPendiente,
    int TotalDeEstancias);

/// <summary>Ficha de un vehículo con su historial de estancias.</summary>
public sealed record DetalleDeVehiculo(
    Placa Placa,
    string Tipo,
    MomentoDeCobro MomentoDeCobro,
    decimal TarifaPorMinuto,
    DateTime FechaDeAlta,
    bool EstaDentro,
    int? MinutosAcumulados,
    decimal? SaldoPendiente,
    IReadOnlyList<EstanciaEnLista> Estancias)
{
    public int TotalDeMinutos => Estancias.Sum(estancia => estancia.Minutos);

    public decimal TotalCobrado => Estancias.Sum(estancia => estancia.ImporteCobrado);
}

/// <summary>Una estancia en el historial de un vehículo.</summary>
public sealed record EstanciaEnLista(
    DateTime Entrada,
    DateTime? Salida,
    int Minutos,
    decimal ImporteCobrado)
{
    public bool EstaAbierta => Salida is null;
}
