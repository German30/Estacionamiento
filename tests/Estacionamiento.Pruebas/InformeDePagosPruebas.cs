using Estacionamiento.Aplicacion.Contratos;
using Estacionamiento.Aplicacion.Informes;
using Estacionamiento.Dominio.Vehiculos;

namespace Estacionamiento.Pruebas;

/// <summary>El enunciado fija el formato del archivo, así que el formato es parte del contrato.</summary>
public class InformeDePagosPruebas
{
    private const int AnchoPlaca = 12;
    private const int InicioTiempo = 14;   // 12 + 2 de separación
    private const int AnchoTiempo = 25;
    private const int AnchoImporte = 18;
    private const int AnchoTotal = 59;     // 12 + 2 + 25 + 2 + 18

    private static readonly IReadOnlyList<LineaDePagoDeResidente> LineasDelEnunciado = new[]
    {
        new LineaDePagoDeResidente(Placa.Crear("S1234A"), 20134, 1006.70m),
        new LineaDePagoDeResidente(Placa.Crear("4567ABC"), 4896, 244.80m)
    };

    [Fact]
    public void El_encabezado_es_el_del_enunciado()
    {
        var lineas = Lineas(GeneradorDeInformeDePagos.Formatear(LineasDelEnunciado));

        Assert.Equal(
            "Núm. placa    Tiempo estacionado (min.)    Cantidad a pagar",
            lineas[0]);
    }

    [Fact]
    public void Cada_residente_ocupa_una_linea_bajo_el_encabezado()
    {
        var lineas = Lineas(GeneradorDeInformeDePagos.Formatear(LineasDelEnunciado));

        Assert.Equal(3, lineas.Length); // encabezado + dos residentes
    }

    [Fact]
    public void Las_columnas_quedan_alineadas()
    {
        var lineas = Lineas(GeneradorDeInformeDePagos.Formatear(LineasDelEnunciado));

        Assert.All(lineas, linea => Assert.Equal(AnchoTotal, linea.Length));

        // Placa a la izquierda, números a la derecha, en sus columnas.
        Assert.Equal("S1234A", lineas[1][..AnchoPlaca].Trim());
        Assert.Equal("20134", lineas[1].Substring(InicioTiempo, AnchoTiempo).Trim());
        Assert.Equal("1006.70", lineas[1][^AnchoImporte..].Trim());

        Assert.Equal("4567ABC", lineas[2][..AnchoPlaca].Trim());
        Assert.Equal("4896", lineas[2].Substring(InicioTiempo, AnchoTiempo).Trim());
        Assert.Equal("244.80", lineas[2][^AnchoImporte..].Trim());
    }

    [Fact]
    public void El_importe_lleva_siempre_dos_decimales_con_punto()
    {
        var lineas = Lineas(GeneradorDeInformeDePagos.Formatear(new[]
        {
            new LineaDePagoDeResidente(Placa.Crear("RES001"), 0, 0m),
            new LineaDePagoDeResidente(Placa.Crear("RES002"), 20, 1m),
            new LineaDePagoDeResidente(Placa.Crear("RES003"), 100000, 5000m)
        }));

        Assert.Equal("0.00", lineas[1][^AnchoImporte..].Trim());
        Assert.Equal("1.00", lineas[2][^AnchoImporte..].Trim());
        Assert.Equal("5000.00", lineas[3][^AnchoImporte..].Trim());
    }

    [Fact]
    public void Sin_residentes_el_archivo_lleva_solo_el_encabezado()
    {
        var lineas = Lineas(GeneradorDeInformeDePagos.Formatear(Array.Empty<LineaDePagoDeResidente>()));

        Assert.Single(lineas);
    }

    private static string[] Lineas(string informe) =>
        informe.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
}
