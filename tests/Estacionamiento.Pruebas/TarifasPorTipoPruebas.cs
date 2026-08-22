using Estacionamiento.Dominio.Vehiculos;

namespace Estacionamiento.Pruebas;

/// <summary>Las tres reglas de cobro del enunciado, una por tipo de vehículo.</summary>
public class TarifasPorTipoPruebas
{
    private static readonly DateTime Inicio = new(2026, 8, 21, 9, 0, 0);

    // ---- Oficial: no paga, pero se registran sus estancias -------------------------------

    [Fact]
    public void El_oficial_no_paga_y_su_estancia_queda_registrada()
    {
        var oficial = new VehiculoOficial(Placa.Crear("OFI001"), Inicio);
        oficial.RegistrarEntrada(Inicio);

        var salida = oficial.RegistrarSalida(Inicio.AddHours(3));

        Assert.Equal(MomentoDeCobro.Ninguno, salida.MomentoDeCobro);
        Assert.Equal(0m, salida.ImporteACobrarAhora);
        Assert.Equal(180, salida.MinutosFacturables);

        var estancia = Assert.Single(oficial.Estancias);
        Assert.Equal(Inicio, estancia.Entrada);
        Assert.Equal(Inicio.AddHours(3), estancia.Salida);
        Assert.Equal(0m, estancia.ImporteCobrado);
    }

    [Fact]
    public void Comenzar_mes_elimina_las_estancias_del_oficial()
    {
        var oficial = new VehiculoOficial(Placa.Crear("OFI001"), Inicio);

        oficial.RegistrarEntrada(Inicio);
        oficial.RegistrarSalida(Inicio.AddHours(1));
        oficial.RegistrarEntrada(Inicio.AddHours(2));
        oficial.RegistrarSalida(Inicio.AddHours(3));

        Assert.Equal(2, oficial.Estancias.Count);

        oficial.ComenzarMes();

        Assert.Empty(oficial.Estancias);
    }

    [Fact]
    public void Comenzar_mes_conserva_la_estancia_del_oficial_que_sigue_dentro()
    {
        var oficial = new VehiculoOficial(Placa.Crear("OFI001"), Inicio);

        oficial.RegistrarEntrada(Inicio);
        oficial.RegistrarSalida(Inicio.AddHours(1));
        oficial.RegistrarEntrada(Inicio.AddHours(2)); // sigue dentro al comenzar el mes

        oficial.ComenzarMes();

        var abierta = Assert.Single(oficial.Estancias);
        Assert.True(abierta.EstaAbierta);

        // Y su salida se puede registrar con normalidad.
        var salida = oficial.RegistrarSalida(Inicio.AddHours(4));
        Assert.Equal(120, salida.MinutosFacturables);
    }

    // ---- Residente: MXN$0.05 el minuto, acumulado y liquidado a fin de mes ----------------

    [Fact]
    public void El_residente_acumula_minutos_en_lugar_de_pagar_a_la_salida()
    {
        var residente = new VehiculoResidente(Placa.Crear("RES001"), Inicio);

        residente.RegistrarEntrada(Inicio);
        var salida = residente.RegistrarSalida(Inicio.AddMinutes(100));

        Assert.Equal(MomentoDeCobro.AFinDeMes, salida.MomentoDeCobro);
        Assert.Equal(0m, salida.ImporteACobrarAhora);
        Assert.Equal(100, residente.MinutosAcumulados);
        Assert.Equal(5.00m, residente.SaldoPendiente); // 100 x 0.05
    }

    [Fact]
    public void El_residente_suma_las_estancias_de_todo_el_mes()
    {
        var residente = new VehiculoResidente(Placa.Crear("RES001"), Inicio);

        residente.RegistrarEntrada(Inicio);
        residente.RegistrarSalida(Inicio.AddMinutes(30));
        residente.RegistrarEntrada(Inicio.AddMinutes(60));
        residente.RegistrarSalida(Inicio.AddMinutes(120));

        Assert.Equal(90, residente.MinutosAcumulados);
        Assert.Equal(4.50m, residente.SaldoPendiente);
    }

    [Fact]
    public void El_ejemplo_del_enunciado_da_el_importe_del_enunciado()
    {
        // "S1234A  20134  1006.70" y "4567ABC  4896  244.80"
        Assert.Equal(1006.70m, SaldoDeResidenteCon(20134));
        Assert.Equal(244.80m, SaldoDeResidenteCon(4896));
    }

    [Fact]
    public void Comenzar_mes_pone_a_cero_el_tiempo_del_residente()
    {
        var residente = new VehiculoResidente(Placa.Crear("RES001"), Inicio);
        residente.RegistrarEntrada(Inicio);
        residente.RegistrarSalida(Inicio.AddMinutes(500));

        Assert.Equal(500, residente.MinutosAcumulados);

        residente.ComenzarMes();

        Assert.Equal(0, residente.MinutosAcumulados);
        Assert.Equal(0m, residente.SaldoPendiente);
    }

    // ---- No residente: MXN$0.5 el minuto, cobrado a la salida -----------------------------

    [Fact]
    public void El_no_residente_paga_a_la_salida()
    {
        var noResidente = new VehiculoNoResidente(Placa.Crear("ABC1234"), Inicio);

        noResidente.RegistrarEntrada(Inicio);
        var salida = noResidente.RegistrarSalida(Inicio.AddMinutes(147));

        Assert.Equal(MomentoDeCobro.ALaSalida, salida.MomentoDeCobro);
        Assert.Equal(73.50m, salida.ImporteACobrarAhora); // 147 x 0.5
        Assert.Null(salida.SaldoPendiente);

        Assert.Equal(73.50m, Assert.Single(noResidente.Estancias).ImporteCobrado);
    }

    [Fact]
    public void El_no_residente_no_arrastra_saldo_entre_meses()
    {
        var noResidente = new VehiculoNoResidente(Placa.Crear("ABC1234"), Inicio);
        noResidente.RegistrarEntrada(Inicio);
        noResidente.RegistrarSalida(Inicio.AddMinutes(60));

        noResidente.ComenzarMes(); // no hay nada que reiniciar: ya pagó al salir

        Assert.Equal(30m, Assert.Single(noResidente.Estancias).ImporteCobrado);
    }

    [Fact]
    public void Las_tarifas_son_las_del_enunciado()
    {
        var momento = Inicio;

        Assert.Equal(0m, new VehiculoOficial(Placa.Crear("OFI001"), momento).TarifaPorMinuto);
        Assert.Equal(0.05m, new VehiculoResidente(Placa.Crear("RES001"), momento).TarifaPorMinuto);
        Assert.Equal(0.5m, new VehiculoNoResidente(Placa.Crear("ABC1234"), momento).TarifaPorMinuto);
    }

    private static decimal SaldoDeResidenteCon(int minutos)
    {
        var residente = new VehiculoResidente(Placa.Crear("S1234A"), Inicio);
        residente.RegistrarEntrada(Inicio);
        residente.RegistrarSalida(Inicio.AddMinutes(minutos));

        return residente.SaldoPendiente;
    }
}
