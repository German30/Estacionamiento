using Estacionamiento.Dominio.Excepciones;
using Estacionamiento.Dominio.Vehiculos;
using Microsoft.EntityFrameworkCore;

namespace Estacionamiento.Pruebas;

/// <summary>Los casos de uso del enunciado, de extremo a extremo y contra la base de datos.</summary>
public class CasosDeUsoPruebas
{
    // ---- Registra entrada ----------------------------------------------------------------

    [Fact]
    public async Task Una_placa_desconocida_entra_como_no_residente()
    {
        using var entorno = new EntornoDePruebas();

        var registro = await entorno.EjecutarAsync(s => s.RegistrarEntradaAsync("abc-1234"));

        Assert.Equal("ABC1234", registro.Placa.Valor);
        Assert.Equal("No residente", registro.TipoDeVehiculo);
        Assert.True(registro.VehiculoCreadoEnEsteMomento);

        using var contexto = entorno.NuevoContexto();
        var vehiculo = await contexto.Vehiculos.Include(v => v.Estancias).SingleAsync();

        Assert.IsType<VehiculoNoResidente>(vehiculo);
        Assert.True(Assert.Single(vehiculo.Estancias).EstaAbierta);
    }

    [Fact]
    public async Task Una_placa_ya_dada_de_alta_conserva_su_tipo_al_entrar()
    {
        using var entorno = new EntornoDePruebas();
        await entorno.EjecutarAsync(s => s.DarDeAltaVehiculoDeResidenteAsync("RES001"));

        var registro = await entorno.EjecutarAsync(s => s.RegistrarEntradaAsync("res001"));

        Assert.Equal("Residente", registro.TipoDeVehiculo);
        Assert.False(registro.VehiculoCreadoEnEsteMomento);
    }

    [Fact]
    public async Task No_se_puede_registrar_dos_veces_la_entrada_del_mismo_vehiculo()
    {
        using var entorno = new EntornoDePruebas();
        await entorno.EjecutarAsync(s => s.RegistrarEntradaAsync("ABC1234"));

        await Assert.ThrowsAsync<VehiculoYaEstacionadoException>(
            () => entorno.EjecutarAsync(s => s.RegistrarEntradaAsync("ABC1234")));
    }

    // ---- Registra salida -----------------------------------------------------------------

    [Fact]
    public async Task El_no_residente_paga_al_salir_y_la_estancia_queda_cerrada()
    {
        using var entorno = new EntornoDePruebas();
        await entorno.EjecutarAsync(s => s.RegistrarEntradaAsync("ABC1234"));

        entorno.Reloj.AvanzarMinutos(147);
        var salida = await entorno.EjecutarAsync(s => s.RegistrarSalidaAsync("ABC1234"));

        Assert.Equal(MomentoDeCobro.ALaSalida, salida.MomentoDeCobro);
        Assert.Equal(147, salida.MinutosFacturables);
        Assert.Equal(73.50m, salida.ImporteACobrarAhora);

        using var contexto = entorno.NuevoContexto();
        var estancia = await contexto.Estancias.SingleAsync();

        Assert.False(estancia.EstaAbierta);
        Assert.Equal(73.50m, estancia.ImporteCobrado);
    }

    [Fact]
    public async Task El_residente_acumula_su_tiempo_en_la_base_de_datos()
    {
        using var entorno = new EntornoDePruebas();
        await entorno.EjecutarAsync(s => s.DarDeAltaVehiculoDeResidenteAsync("RES001"));

        await entorno.EjecutarAsync(s => s.RegistrarEntradaAsync("RES001"));
        entorno.Reloj.AvanzarMinutos(60);
        await entorno.EjecutarAsync(s => s.RegistrarSalidaAsync("RES001"));

        entorno.Reloj.AvanzarMinutos(30);
        await entorno.EjecutarAsync(s => s.RegistrarEntradaAsync("RES001"));
        entorno.Reloj.AvanzarMinutos(40);
        var segunda = await entorno.EjecutarAsync(s => s.RegistrarSalidaAsync("RES001"));

        Assert.Equal(100, segunda.MinutosAcumulados);
        Assert.Equal(5.00m, segunda.SaldoPendiente);
        Assert.Equal(0m, segunda.ImporteACobrarAhora);

        using var contexto = entorno.NuevoContexto();
        var residente = await contexto.Vehiculos.OfType<VehiculoResidente>().SingleAsync();
        Assert.Equal(100, residente.MinutosAcumulados);
    }

    [Fact]
    public async Task El_oficial_no_paga_pero_su_estancia_se_guarda()
    {
        using var entorno = new EntornoDePruebas();
        await entorno.EjecutarAsync(s => s.DarDeAltaVehiculoOficialAsync("OFI001"));

        await entorno.EjecutarAsync(s => s.RegistrarEntradaAsync("OFI001"));
        entorno.Reloj.AvanzarMinutos(200);
        var salida = await entorno.EjecutarAsync(s => s.RegistrarSalidaAsync("OFI001"));

        Assert.Equal(MomentoDeCobro.Ninguno, salida.MomentoDeCobro);
        Assert.Equal(0m, salida.ImporteACobrarAhora);

        using var contexto = entorno.NuevoContexto();
        Assert.Equal(1, await contexto.Estancias.CountAsync());
    }

    [Fact]
    public async Task No_se_puede_registrar_la_salida_de_una_placa_que_no_entro()
    {
        using var entorno = new EntornoDePruebas();

        await Assert.ThrowsAsync<VehiculoNoEstacionadoException>(
            () => entorno.EjecutarAsync(s => s.RegistrarSalidaAsync("ZZZ9999")));
    }

    // ---- Altas ---------------------------------------------------------------------------

    [Fact]
    public async Task Dar_de_alta_dos_veces_la_misma_placa_es_un_error_que_dice_el_tipo_existente()
    {
        using var entorno = new EntornoDePruebas();
        await entorno.EjecutarAsync(s => s.DarDeAltaVehiculoOficialAsync("OFI001"));

        var excepcion = await Assert.ThrowsAsync<VehiculoYaRegistradoException>(
            () => entorno.EjecutarAsync(s => s.DarDeAltaVehiculoDeResidenteAsync("ofi-001")));

        Assert.Contains("oficial", excepcion.Message);
    }

    [Fact]
    public async Task Cada_alta_guarda_el_tipo_correcto()
    {
        using var entorno = new EntornoDePruebas();

        await entorno.EjecutarAsync(s => s.DarDeAltaVehiculoOficialAsync("OFI001"));
        await entorno.EjecutarAsync(s => s.DarDeAltaVehiculoDeResidenteAsync("RES001"));

        using var contexto = entorno.NuevoContexto();

        Assert.Equal(1, await contexto.Vehiculos.OfType<VehiculoOficial>().CountAsync());
        Assert.Equal(1, await contexto.Vehiculos.OfType<VehiculoResidente>().CountAsync());
    }

    // ---- Comienza mes ---------------------------------------------------------------------

    [Fact]
    public async Task Comenzar_mes_borra_estancias_de_oficiales_y_reinicia_residentes()
    {
        using var entorno = new EntornoDePruebas();

        await entorno.EjecutarAsync(s => s.DarDeAltaVehiculoOficialAsync("OFI001"));
        await entorno.EjecutarAsync(s => s.DarDeAltaVehiculoDeResidenteAsync("RES001"));

        await CompletarEstanciaAsync(entorno, "OFI001", 120);
        await CompletarEstanciaAsync(entorno, "RES001", 300);
        await CompletarEstanciaAsync(entorno, "ABC1234", 60); // no residente, ya pagó

        var resumen = await entorno.EjecutarAsync(s => s.ComenzarMesAsync());

        Assert.Equal(1, resumen.VehiculosOficialesAfectados);
        Assert.Equal(1, resumen.EstanciasEliminadas);
        Assert.Equal(1, resumen.ResidentesReiniciados);
        Assert.Equal(300, resumen.MinutosPuestosACero);

        using var contexto = entorno.NuevoContexto();

        var oficial = await contexto.Vehiculos.OfType<VehiculoOficial>()
            .Include(v => v.Estancias).SingleAsync();
        Assert.Empty(oficial.Estancias);

        var residente = await contexto.Vehiculos.OfType<VehiculoResidente>().SingleAsync();
        Assert.Equal(0, residente.MinutosAcumulados);

        // El histórico del no residente no se toca: es el registro de lo ya cobrado.
        var noResidente = await contexto.Vehiculos.OfType<VehiculoNoResidente>()
            .Include(v => v.Estancias).SingleAsync();
        Assert.Single(noResidente.Estancias);
    }

    [Fact]
    public async Task Comenzar_mes_no_deja_dentro_a_nadie_sin_su_entrada()
    {
        using var entorno = new EntornoDePruebas();
        await entorno.EjecutarAsync(s => s.DarDeAltaVehiculoOficialAsync("OFI001"));

        await CompletarEstanciaAsync(entorno, "OFI001", 60);
        await entorno.EjecutarAsync(s => s.RegistrarEntradaAsync("OFI001")); // sigue dentro

        var resumen = await entorno.EjecutarAsync(s => s.ComenzarMesAsync());

        Assert.Equal(1, resumen.EstanciasEliminadas);
        Assert.Equal(1, resumen.VehiculosDentroConservados);

        // Y puede salir con normalidad después del cambio de mes.
        entorno.Reloj.AvanzarMinutos(45);
        var salida = await entorno.EjecutarAsync(s => s.RegistrarSalidaAsync("OFI001"));
        Assert.Equal(45, salida.MinutosFacturables);
    }

    // ---- Pagos de residentes ---------------------------------------------------------------

    [Fact]
    public async Task El_informe_lista_a_todos_los_residentes_ordenados_por_placa()
    {
        using var entorno = new EntornoDePruebas();

        await entorno.EjecutarAsync(s => s.DarDeAltaVehiculoDeResidenteAsync("S1234A"));
        await entorno.EjecutarAsync(s => s.DarDeAltaVehiculoDeResidenteAsync("4567ABC"));
        await entorno.EjecutarAsync(s => s.DarDeAltaVehiculoOficialAsync("OFI001")); // no debe aparecer

        await CompletarEstanciaAsync(entorno, "S1234A", 20134);
        await CompletarEstanciaAsync(entorno, "4567ABC", 4896);

        var informe = await entorno.EjecutarAsync(s =>
            s.GenerarInformeDePagosDeResidentesAsync("pagos.txt"));

        Assert.Equal(2, informe.Lineas.Count);
        Assert.Equal("4567ABC", informe.Lineas[0].Placa.Valor); // orden alfabético
        Assert.Equal("S1234A", informe.Lineas[1].Placa.Valor);

        Assert.Equal(244.80m, informe.Lineas[0].CantidadAPagar);
        Assert.Equal(1006.70m, informe.Lineas[1].CantidadAPagar);

        Assert.Equal(25030, informe.TotalDeMinutos);
        Assert.Equal(1251.50m, informe.TotalAPagar);

        Assert.NotNull(entorno.Almacen.Contenido);
        Assert.Contains("Núm. placa", entorno.Almacen.Contenido);
        Assert.Contains("1006.70", entorno.Almacen.Contenido);
    }

    [Fact]
    public async Task El_informe_incluye_a_los_residentes_que_no_usaron_el_estacionamiento()
    {
        using var entorno = new EntornoDePruebas();
        await entorno.EjecutarAsync(s => s.DarDeAltaVehiculoDeResidenteAsync("RES001"));

        var informe = await entorno.EjecutarAsync(s =>
            s.GenerarInformeDePagosDeResidentesAsync("pagos.txt"));

        var linea = Assert.Single(informe.Lineas);
        Assert.Equal(0, linea.MinutosEstacionado);
        Assert.Equal(0m, linea.CantidadAPagar);
    }

    [Fact]
    public async Task Sin_nombre_de_archivo_el_informe_no_se_genera()
    {
        using var entorno = new EntornoDePruebas();

        await Assert.ThrowsAsync<ArgumentException>(
            () => entorno.EjecutarAsync(s => s.GenerarInformeDePagosDeResidentesAsync("   ")));
    }

    private static async Task CompletarEstanciaAsync(EntornoDePruebas entorno, string placa, int minutos)
    {
        await entorno.EjecutarAsync(s => s.RegistrarEntradaAsync(placa));
        entorno.Reloj.AvanzarMinutos(minutos);
        await entorno.EjecutarAsync(s => s.RegistrarSalidaAsync(placa));
    }
}
