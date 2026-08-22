using Estacionamiento.Aplicacion.Consultas;
using Estacionamiento.Dominio.Comun;
using Estacionamiento.Dominio.Vehiculos;
using Microsoft.EntityFrameworkCore;

namespace Estacionamiento.Infraestructura.Persistencia;

/// <summary>
/// Consultas de sólo lectura. Van contra el contexto directamente y sin seguimiento de cambios:
/// no hay nada que guardar y así no se paga el coste de rastrear entidades que sólo se pintan.
/// </summary>
public sealed class ConsultasEstacionamiento : IConsultasEstacionamiento
{
    private readonly EstacionamientoDbContext _contexto;
    private readonly IReloj _reloj;

    public ConsultasEstacionamiento(EstacionamientoDbContext contexto, IReloj reloj)
    {
        _contexto = contexto;
        _reloj = reloj;
    }

    public async Task<PanelDeControl> ObtenerPanelAsync(CancellationToken cancelacion = default)
    {
        var inicioDelDia = _reloj.Ahora.Date;

        var porTipo = await _contexto.Vehiculos
            .AsNoTracking()
            .GroupBy(vehiculo => EF.Property<string>(vehiculo, "TipoDeVehiculo"))
            .Select(grupo => new { Tipo = grupo.Key, Cantidad = grupo.Count() })
            .ToListAsync(cancelacion);

        var minutosDeResidentes = await _contexto.Vehiculos
            .AsNoTracking()
            .OfType<VehiculoResidente>()
            .SumAsync(residente => (int?)residente.MinutosAcumulados, cancelacion) ?? 0;

        var dentro = await _contexto.Estancias
            .AsNoTracking()
            .CountAsync(estancia => estancia.Salida == null, cancelacion);

        var salidasDeHoy = await _contexto.Estancias
            .AsNoTracking()
            .Where(estancia => estancia.Salida != null && estancia.Salida >= inicioDelDia)
            .ToListAsync(cancelacion);

        int Cantidad(string tipo) =>
            porTipo.FirstOrDefault(fila => fila.Tipo == tipo)?.Cantidad ?? 0;

        return new PanelDeControl(
            VehiculosDentro: dentro,
            TotalDeVehiculos: porTipo.Sum(fila => fila.Cantidad),
            Oficiales: Cantidad(VehiculoOficial.Discriminador),
            Residentes: Cantidad(VehiculoResidente.Discriminador),
            NoResidentes: Cantidad(VehiculoNoResidente.Discriminador),
            MinutosAcumuladosDeResidentes: minutosDeResidentes,
            SaldoPendienteDeResidentes:
                PoliticaDeImporte.Calcular(minutosDeResidentes, VehiculoResidente.Tarifa),
            SalidasDeHoy: salidasDeHoy.Count,
            CobradoHoy: salidasDeHoy.Sum(estancia => estancia.ImporteCobrado));
    }

    public async Task<IReadOnlyList<VehiculoEnLista>> ListarDentroAsync(
        CancellationToken cancelacion = default)
    {
        var vehiculos = await ConEstancias()
            .Where(vehiculo => vehiculo.Estancias.Any(estancia => estancia.Salida == null))
            .ToListAsync(cancelacion);

        return vehiculos
            .Select(AFila)
            .OrderByDescending(fila => fila.MinutosDentro) // el que más lleva dentro, arriba
            .ToList();
    }

    public async Task<IReadOnlyList<VehiculoEnLista>> ListarVehiculosAsync(
        string? filtro = null, string? tipo = null, CancellationToken cancelacion = default)
    {
        var consulta = ConEstancias();

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            consulta = consulta.Where(vehiculo =>
                EF.Property<string>(vehiculo, "TipoDeVehiculo") == tipo);
        }

        var vehiculos = await consulta.ToListAsync(cancelacion);

        // La placa pasa por un conversor de valor, así que el filtro por fragmento y el orden
        // se resuelven en memoria: SQL no puede mirar dentro del objeto de valor.
        var filas = vehiculos.Select(AFila);

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            var buscado = filtro.Trim().ToUpperInvariant();
            filas = filas.Where(fila => fila.Placa.Valor.Contains(buscado, StringComparison.Ordinal));
        }

        return filas
            .OrderByDescending(fila => fila.EstaDentro)
            .ThenBy(fila => fila.Placa.Valor, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<IReadOnlyList<VehiculoEnLista>> ListarResidentesAsync(
        CancellationToken cancelacion = default)
    {
        var residentes = await ConEstancias()
            .OfType<VehiculoResidente>()
            .ToListAsync(cancelacion);

        return residentes
            .Select(AFila)
            .OrderBy(fila => fila.Placa.Valor, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<DetalleDeVehiculo?> ObtenerDetalleAsync(
        string placa, CancellationToken cancelacion = default)
    {
        if (!Placa.TryCrear(placa, out var numeroDePlaca, out _))
        {
            return null;
        }

        var vehiculo = await ConEstancias()
            .SingleOrDefaultAsync(candidato => candidato.Placa == numeroDePlaca, cancelacion);

        if (vehiculo is null)
        {
            return null;
        }

        var residente = vehiculo as VehiculoResidente;

        return new DetalleDeVehiculo(
            vehiculo.Placa,
            vehiculo.Tipo,
            vehiculo.MomentoDeCobro,
            vehiculo.TarifaPorMinuto,
            vehiculo.FechaDeAlta,
            vehiculo.EstanciaAbierta is not null,
            residente?.MinutosAcumulados,
            residente?.SaldoPendiente,
            vehiculo.Estancias
                .OrderByDescending(estancia => estancia.Entrada)
                .Select(estancia => new EstanciaEnLista(
                    estancia.Entrada,
                    estancia.Salida,
                    estancia.MinutosFacturables,
                    estancia.ImporteCobrado))
                .ToList());
    }

    private IQueryable<Vehiculo> ConEstancias() =>
        _contexto.Vehiculos.AsNoTracking().Include(vehiculo => vehiculo.Estancias);

    private VehiculoEnLista AFila(Vehiculo vehiculo)
    {
        var abierta = vehiculo.EstanciaAbierta;
        var residente = vehiculo as VehiculoResidente;

        return new VehiculoEnLista(
            vehiculo.Placa,
            vehiculo.Tipo,
            vehiculo.MomentoDeCobro,
            abierta is not null,
            abierta?.Entrada,
            abierta is null ? 0 : PoliticaDeTiempo.AMinutosFacturables(_reloj.Ahora - abierta.Entrada),
            residente?.MinutosAcumulados,
            residente?.SaldoPendiente,
            vehiculo.Estancias.Count);
    }
}
