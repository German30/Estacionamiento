using Estacionamiento.Dominio.Vehiculos;

namespace Estacionamiento.Aplicacion.Contratos;

/// <summary>Resultado del caso de uso "registra entrada".</summary>
public sealed record RegistroDeEntrada(
    Placa Placa,
    string TipoDeVehiculo,
    DateTime Entrada,
    bool VehiculoCreadoEnEsteMomento);

/// <summary>Resultado de dar de alta un vehículo oficial o de residente.</summary>
public sealed record VehiculoDadoDeAlta(Placa Placa, string TipoDeVehiculo, DateTime FechaDeAlta);

/// <summary>Resultado del caso de uso "comienza mes".</summary>
public sealed record ResumenDeComienzoDeMes(
    int VehiculosOficialesAfectados,
    int EstanciasEliminadas,
    int ResidentesReiniciados,
    int MinutosPuestosACero,
    int VehiculosDentroConservados);

/// <summary>Una línea del informe de pagos de residentes.</summary>
public sealed record LineaDePagoDeResidente(Placa Placa, int MinutosEstacionado, decimal CantidadAPagar);

/// <summary>Resultado del caso de uso "pagos de residentes".</summary>
/// <param name="RutaDelArchivo">Ruta del archivo escrito, o <c>null</c> si sólo se preparó el contenido.</param>
public sealed record InformeDePagos(
    string? RutaDelArchivo,
    string Contenido,
    IReadOnlyList<LineaDePagoDeResidente> Lineas,
    int TotalDeMinutos,
    decimal TotalAPagar);
