using Estacionamiento.Dominio.Comun;
using Estacionamiento.Dominio.Vehiculos;
using Estacionamiento.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Estacionamiento.Pruebas;

/// <summary>
/// Los datos de demostración se generan pasando por el dominio, así que deben cumplir las
/// mismas reglas que los datos reales. Estas pruebas lo comprueban leyendo de la base.
/// </summary>
public class SembradorDePruebas
{
    [Fact]
    public async Task Siembra_la_cantidad_pedida_con_los_tres_tipos_de_vehiculo()
    {
        using var entorno = new EntornoDePruebas();

        var resumen = await entorno.SembrarAsync(100);

        Assert.Equal(100, resumen.Vehiculos);
        Assert.Equal(15, resumen.Oficiales);
        Assert.Equal(35, resumen.Residentes);
        Assert.Equal(50, resumen.NoResidentes);

        using var contexto = entorno.NuevoContexto();
        Assert.Equal(100, await contexto.Vehiculos.CountAsync());
        Assert.Equal(resumen.Estancias, await contexto.Estancias.CountAsync());
    }

    [Fact]
    public async Task Todas_las_placas_son_distintas_y_validas()
    {
        using var entorno = new EntornoDePruebas();
        await entorno.SembrarAsync(100);

        using var contexto = entorno.NuevoContexto();
        var placas = await contexto.Vehiculos.Select(vehiculo => vehiculo.Placa).ToListAsync();

        Assert.Equal(100, placas.Distinct().Count());
        Assert.All(placas, placa => Assert.Equal(placa, Placa.Crear(placa.Valor)));
    }

    [Fact]
    public async Task El_importe_cobrado_a_los_no_residentes_corresponde_a_su_tarifa()
    {
        using var entorno = new EntornoDePruebas();
        await entorno.SembrarAsync(100);

        using var contexto = entorno.NuevoContexto();
        var noResidentes = await contexto.Vehiculos.OfType<VehiculoNoResidente>()
            .Include(vehiculo => vehiculo.Estancias).ToListAsync();

        var cerradas = noResidentes.SelectMany(v => v.Estancias).Where(e => !e.EstaAbierta).ToList();

        Assert.NotEmpty(cerradas);
        Assert.All(cerradas, estancia => Assert.Equal(
            PoliticaDeImporte.Calcular(estancia.MinutosFacturables, VehiculoNoResidente.Tarifa),
            estancia.ImporteCobrado));
    }

    [Fact]
    public async Task Los_minutos_de_cada_residente_son_la_suma_de_sus_estancias()
    {
        using var entorno = new EntornoDePruebas();
        await entorno.SembrarAsync(100);

        using var contexto = entorno.NuevoContexto();
        var residentes = await contexto.Vehiculos.OfType<VehiculoResidente>()
            .Include(vehiculo => vehiculo.Estancias).ToListAsync();

        Assert.All(residentes, residente => Assert.Equal(
            residente.Estancias.Where(e => !e.EstaAbierta).Sum(e => e.MinutosFacturables),
            residente.MinutosAcumulados));
    }

    [Fact]
    public async Task Oficiales_y_residentes_no_pagan_por_estancia()
    {
        using var entorno = new EntornoDePruebas();
        await entorno.SembrarAsync(100);

        using var contexto = entorno.NuevoContexto();
        var estanciasSinCobro = await contexto.Estancias
            .Where(estancia => estancia.Vehiculo is VehiculoOficial || estancia.Vehiculo is VehiculoResidente)
            .ToListAsync();

        Assert.NotEmpty(estanciasSinCobro);
        Assert.All(estanciasSinCobro, estancia => Assert.Equal(0m, estancia.ImporteCobrado));
    }

    [Fact]
    public async Task Ningun_vehiculo_queda_con_dos_estancias_abiertas()
    {
        using var entorno = new EntornoDePruebas();
        await entorno.SembrarAsync(100);

        using var contexto = entorno.NuevoContexto();
        var abiertasPorVehiculo = await contexto.Estancias
            .Where(estancia => estancia.Salida == null)
            .GroupBy(estancia => estancia.VehiculoId)
            .Select(grupo => grupo.Count())
            .ToListAsync();

        Assert.All(abiertasPorVehiculo, cantidad => Assert.Equal(1, cantidad));
    }

    [Fact]
    public async Task Toda_estancia_cerrada_sale_despues_de_entrar()
    {
        using var entorno = new EntornoDePruebas();
        await entorno.SembrarAsync(100);

        using var contexto = entorno.NuevoContexto();
        var cerradas = await contexto.Estancias.Where(estancia => estancia.Salida != null).ToListAsync();

        Assert.All(cerradas, estancia => Assert.True(estancia.Salida >= estancia.Entrada));
    }

    [Fact]
    public async Task Sembrar_dos_veces_produce_exactamente_los_mismos_datos()
    {
        var momento = new DateTime(2026, 8, 21, 12, 0, 0);

        using var primero = new EntornoDePruebas(momento);
        using var segundo = new EntornoDePruebas(momento);

        await primero.SembrarAsync(50);
        await segundo.SembrarAsync(50);

        using var contextoA = primero.NuevoContexto();
        using var contextoB = segundo.NuevoContexto();

        // La placa pasa por un conversor de valor, así que se ordena en memoria: SQL no puede
        // mirar dentro del objeto de valor.
        Assert.Equal(
            await PlacasOrdenadasAsync(contextoA),
            await PlacasOrdenadasAsync(contextoB));

        Assert.Equal(await contextoA.Estancias.CountAsync(), await contextoB.Estancias.CountAsync());
    }

    [Fact]
    public async Task Vaciar_borra_los_vehiculos_y_sus_estancias_en_cascada()
    {
        using var entorno = new EntornoDePruebas();
        await entorno.SembrarAsync(20);

        using (var contexto = entorno.NuevoContexto())
        {
            Assert.True(await entorno.NuevoSembrador(contexto).HayDatosAsync());
            await entorno.NuevoSembrador(contexto).VaciarAsync();
        }

        using var comprobacion = entorno.NuevoContexto();
        Assert.Equal(0, await comprobacion.Vehiculos.CountAsync());
        Assert.Equal(0, await comprobacion.Estancias.CountAsync());
    }

    [Fact]
    public async Task Sembrar_una_cantidad_no_positiva_es_un_error()
    {
        using var entorno = new EntornoDePruebas();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => entorno.SembrarAsync(0));
    }

    private static async Task<List<string>> PlacasOrdenadasAsync(EstacionamientoDbContext contexto)
    {
        var placas = await contexto.Vehiculos.Select(vehiculo => vehiculo.Placa).ToListAsync();

        return placas.Select(placa => placa.Valor).Order(StringComparer.Ordinal).ToList();
    }
}
