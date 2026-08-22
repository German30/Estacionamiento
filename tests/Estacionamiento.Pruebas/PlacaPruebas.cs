using Estacionamiento.Dominio.Excepciones;
using Estacionamiento.Dominio.Vehiculos;

namespace Estacionamiento.Pruebas;

public class PlacaPruebas
{
    [Theory]
    [InlineData("abc1234", "ABC1234")]
    [InlineData("  ABC1234  ", "ABC1234")]
    [InlineData("abc-1234", "ABC1234")]
    [InlineData("a b c 1 2 3 4", "ABC1234")]
    [InlineData("S1234A", "S1234A")]
    public void Normaliza_mayusculas_espacios_y_guiones(string entrada, string esperada)
    {
        Assert.Equal(esperada, Placa.Crear(entrada).Valor);
    }

    [Fact]
    public void Dos_formas_de_escribir_la_misma_placa_son_el_mismo_vehiculo()
    {
        Assert.Equal(Placa.Crear("abc-1234"), Placa.Crear("ABC 1234"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("AB1")]                 // demasiado corta
    [InlineData("ABC12345678")]         // demasiado larga
    [InlineData("ABC*123")]             // carácter no alfanumérico
    public void Rechaza_placas_invalidas(string? entrada)
    {
        Assert.Throws<PlacaInvalidaException>(() => Placa.Crear(entrada));

        Assert.False(Placa.TryCrear(entrada, out _, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void El_mensaje_de_error_explica_que_hacer()
    {
        Placa.TryCrear("AB1", out _, out var error);

        Assert.Contains("entre 5 y 10", error);
    }

    [Fact]
    public void Se_ordena_alfabeticamente_para_el_informe()
    {
        var placas = new[] { Placa.Crear("Z9999"), Placa.Crear("4567ABC"), Placa.Crear("S1234A") };

        Assert.Equal(
            new[] { "4567ABC", "S1234A", "Z9999" },
            placas.OrderBy(placa => placa).Select(placa => placa.Valor));
    }
}
