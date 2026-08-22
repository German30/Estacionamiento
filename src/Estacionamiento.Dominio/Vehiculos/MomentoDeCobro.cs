namespace Estacionamiento.Dominio.Vehiculos;

/// <summary>Cuándo se le cobra a un tipo de vehículo.</summary>
public enum MomentoDeCobro
{
    /// <summary>No paga nunca; la estancia sólo se registra para control.</summary>
    Ninguno,

    /// <summary>Paga en el momento de salir del estacionamiento.</summary>
    ALaSalida,

    /// <summary>Acumula tiempo y liquida a fin de mes.</summary>
    AFinDeMes
}
