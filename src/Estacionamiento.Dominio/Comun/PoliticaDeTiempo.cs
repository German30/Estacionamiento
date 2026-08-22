namespace Estacionamiento.Dominio.Comun;

/// <summary>
/// Regla única de conversión de una duración a minutos facturables.
/// Toda fracción de minuto se cobra como un minuto completo (redondeo al alza),
/// que es la convención habitual en estacionamientos de pago.
/// </summary>
/// <remarks>
/// Centralizada aquí a propósito: si el negocio decide cobrar por minutos exactos
/// o en bloques de 15, este es el único punto que hay que tocar.
/// </remarks>
public static class PoliticaDeTiempo
{
    public static int AMinutosFacturables(TimeSpan duracion) =>
        duracion <= TimeSpan.Zero ? 0 : (int)Math.Ceiling(duracion.TotalMinutes);
}
