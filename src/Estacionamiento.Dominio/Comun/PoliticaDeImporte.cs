namespace Estacionamiento.Dominio.Comun;

/// <summary>
/// Regla única de redondeo monetario: dos decimales, mitad hacia arriba (MXN).
/// </summary>
public static class PoliticaDeImporte
{
    public const int Decimales = 2;

    public static decimal Redondear(decimal importe) =>
        Math.Round(importe, Decimales, MidpointRounding.AwayFromZero);

    public static decimal Calcular(int minutos, decimal tarifaPorMinuto) =>
        Redondear(minutos * tarifaPorMinuto);
}
