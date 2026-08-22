using System.Globalization;
using System.Text;
using Estacionamiento.Aplicacion.Contratos;

namespace Estacionamiento.Aplicacion.Informes;

/// <summary>
/// Da formato al informe de pagos de residentes. Es una función pura sobre las líneas del
/// informe: no toca el disco, de modo que el formato exigido por el enunciado se puede
/// verificar con pruebas.
/// </summary>
public static class GeneradorDeInformeDePagos
{
    private const string EncabezadoPlaca = "Núm. placa";
    private const string EncabezadoTiempo = "Tiempo estacionado (min.)";
    private const string EncabezadoImporte = "Cantidad a pagar";

    private const int AnchoPlaca = 12;
    private const int AnchoTiempo = 25;
    private const int AnchoImporte = 18;
    private const string Separador = "  ";

    public static string Formatear(IReadOnlyList<LineaDePagoDeResidente> lineas)
    {
        var informe = new StringBuilder();

        informe
            .Append(EncabezadoPlaca.PadRight(AnchoPlaca)).Append(Separador)
            .Append(EncabezadoTiempo.PadLeft(AnchoTiempo)).Append(Separador)
            .Append(EncabezadoImporte.PadLeft(AnchoImporte))
            .AppendLine();

        foreach (var linea in lineas)
        {
            informe
                .Append(linea.Placa.Valor.PadRight(AnchoPlaca)).Append(Separador)
                .Append(linea.MinutosEstacionado.ToString(CultureInfo.InvariantCulture).PadLeft(AnchoTiempo))
                .Append(Separador)
                .Append(linea.CantidadAPagar.ToString("F2", CultureInfo.InvariantCulture).PadLeft(AnchoImporte))
                .AppendLine();
        }

        return informe.ToString();
    }
}
