using Estacionamiento.Dominio.Excepciones;
using Estacionamiento.Dominio.Vehiculos;

namespace Estacionamiento.Pruebas;

/// <summary>Reglas comunes a todo vehículo: no se puede entrar dos veces ni salir sin haber entrado.</summary>
public class VehiculoPruebas
{
    private static readonly DateTime Inicio = new(2026, 8, 21, 9, 0, 0);

    [Fact]
    public void No_admite_una_segunda_entrada_sin_haber_salido()
    {
        var vehiculo = new VehiculoNoResidente(Placa.Crear("ABC1234"), Inicio);
        vehiculo.RegistrarEntrada(Inicio);

        var excepcion = Assert.Throws<VehiculoYaEstacionadoException>(
            () => vehiculo.RegistrarEntrada(Inicio.AddMinutes(5)));

        Assert.Contains("ABC1234", excepcion.Message);
    }

    [Fact]
    public void No_admite_una_salida_sin_entrada_previa()
    {
        var vehiculo = new VehiculoNoResidente(Placa.Crear("ABC1234"), Inicio);

        Assert.Throws<VehiculoNoEstacionadoException>(() => vehiculo.RegistrarSalida(Inicio));
    }

    [Fact]
    public void Tras_salir_puede_volver_a_entrar()
    {
        var vehiculo = new VehiculoNoResidente(Placa.Crear("ABC1234"), Inicio);

        vehiculo.RegistrarEntrada(Inicio);
        vehiculo.RegistrarSalida(Inicio.AddMinutes(30));
        vehiculo.RegistrarEntrada(Inicio.AddMinutes(60));

        Assert.Equal(2, vehiculo.Estancias.Count);
        Assert.NotNull(vehiculo.EstanciaAbierta);
    }

    [Fact]
    public void Estar_dentro_se_refleja_en_la_estancia_abierta()
    {
        var vehiculo = new VehiculoNoResidente(Placa.Crear("ABC1234"), Inicio);
        Assert.Null(vehiculo.EstanciaAbierta);

        vehiculo.RegistrarEntrada(Inicio);
        Assert.NotNull(vehiculo.EstanciaAbierta);

        vehiculo.RegistrarSalida(Inicio.AddMinutes(10));
        Assert.Null(vehiculo.EstanciaAbierta);
    }

    [Fact]
    public void Una_salida_anterior_a_la_entrada_es_un_error()
    {
        var vehiculo = new VehiculoNoResidente(Placa.Crear("ABC1234"), Inicio);
        vehiculo.RegistrarEntrada(Inicio);

        Assert.Throws<SalidaAnteriorALaEntradaException>(
            () => vehiculo.RegistrarSalida(Inicio.AddMinutes(-1)));
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.1, 1)]     // toda fracción de minuto se cobra completa
    [InlineData(1.0, 1)]
    [InlineData(1.2, 2)]
    [InlineData(59.9, 60)]
    [InlineData(120.0, 120)]
    public void La_fraccion_de_minuto_se_cobra_como_minuto_completo(double minutosReales, int esperados)
    {
        var vehiculo = new VehiculoNoResidente(Placa.Crear("ABC1234"), Inicio);
        vehiculo.RegistrarEntrada(Inicio);

        var salida = vehiculo.RegistrarSalida(Inicio.AddMinutes(minutosReales));

        Assert.Equal(esperados, salida.MinutosFacturables);
    }
}
