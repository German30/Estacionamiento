using Estacionamiento.Aplicacion.Abstracciones;
using Estacionamiento.Aplicacion.Contratos;
using Estacionamiento.Aplicacion.Informes;
using Estacionamiento.Dominio.Comun;
using Estacionamiento.Dominio.Excepciones;
using Estacionamiento.Dominio.Vehiculos;

namespace Estacionamiento.Aplicacion.Servicios;

/// <summary>
/// Orquesta los casos de uso: carga el vehículo, deja que el dominio decida qué ocurre y
/// confirma los cambios. Las reglas de cobro no viven aquí, viven en cada tipo de vehículo.
/// </summary>
public sealed class ServicioEstacionamiento : IServicioEstacionamiento
{
    private readonly IRepositorioVehiculos _repositorio;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IAlmacenDeInformes _almacenDeInformes;
    private readonly IReloj _reloj;

    public ServicioEstacionamiento(
        IRepositorioVehiculos repositorio,
        IUnidadDeTrabajo unidadDeTrabajo,
        IAlmacenDeInformes almacenDeInformes,
        IReloj reloj)
    {
        _repositorio = repositorio;
        _unidadDeTrabajo = unidadDeTrabajo;
        _almacenDeInformes = almacenDeInformes;
        _reloj = reloj;
    }

    public async Task<RegistroDeEntrada> RegistrarEntradaAsync(
        string placa, CancellationToken cancelacion = default)
    {
        var numeroDePlaca = Placa.Crear(placa);
        var ahora = _reloj.Ahora;

        var vehiculo = await _repositorio.ObtenerPorPlacaAsync(numeroDePlaca, cancelacion);
        var esNuevo = vehiculo is null;

        if (vehiculo is null)
        {
            // Sólo se dan de alta oficiales y residentes: cualquier otra placa que entra
            // es, por definición, un no residente.
            vehiculo = new VehiculoNoResidente(numeroDePlaca, ahora);
            await _repositorio.AgregarAsync(vehiculo, cancelacion);
        }

        var estancia = vehiculo.RegistrarEntrada(ahora);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        return new RegistroDeEntrada(vehiculo.Placa, vehiculo.Tipo, estancia.Entrada, esNuevo);
    }

    public async Task<ResultadoSalida> RegistrarSalidaAsync(
        string placa, CancellationToken cancelacion = default)
    {
        var numeroDePlaca = Placa.Crear(placa);

        var vehiculo = await _repositorio.ObtenerPorPlacaAsync(numeroDePlaca, cancelacion)
                       ?? throw new VehiculoNoEstacionadoException(numeroDePlaca);

        var resultado = vehiculo.RegistrarSalida(_reloj.Ahora);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        return resultado;
    }

    public Task<VehiculoDadoDeAlta> DarDeAltaVehiculoOficialAsync(
        string placa, CancellationToken cancelacion = default) =>
        DarDeAltaAsync(placa, (numero, fecha) => new VehiculoOficial(numero, fecha), cancelacion);

    public Task<VehiculoDadoDeAlta> DarDeAltaVehiculoDeResidenteAsync(
        string placa, CancellationToken cancelacion = default) =>
        DarDeAltaAsync(placa, (numero, fecha) => new VehiculoResidente(numero, fecha), cancelacion);

    public async Task<ResumenDeComienzoDeMes> ComenzarMesAsync(CancellationToken cancelacion = default)
    {
        var vehiculos = await _repositorio.ObtenerTodosAsync(cancelacion);

        // Los contadores miran los dos tipos que el enunciado nombra, pero el reinicio es
        // polimórfico: un tipo nuevo se reinicia solo, sin tocar este método.
        var oficiales = vehiculos.OfType<VehiculoOficial>().ToList();
        var residentes = vehiculos.OfType<VehiculoResidente>().ToList();

        var estanciasEliminadas = oficiales.Sum(oficial => oficial.Estancias.Count(e => !e.EstaAbierta));
        var oficialesAfectados = oficiales.Count(oficial => oficial.Estancias.Any(e => !e.EstaAbierta));
        var minutosPuestosACero = residentes.Sum(residente => residente.MinutosAcumulados);
        var residentesReiniciados = residentes.Count(residente => residente.MinutosAcumulados > 0);
        var dentroConservados = vehiculos.Count(vehiculo => vehiculo.EstanciaAbierta is not null);

        foreach (var vehiculo in vehiculos)
        {
            vehiculo.ComenzarMes();
        }

        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        return new ResumenDeComienzoDeMes(
            oficialesAfectados, estanciasEliminadas, residentesReiniciados,
            minutosPuestosACero, dentroConservados);
    }

    public async Task<InformeDePagos> GenerarInformeDePagosDeResidentesAsync(
        string rutaDelArchivo, CancellationToken cancelacion = default)
    {
        if (string.IsNullOrWhiteSpace(rutaDelArchivo))
        {
            throw new ArgumentException("Debe indicar el nombre del archivo del informe.", nameof(rutaDelArchivo));
        }

        var informe = await PrepararInformeDePagosDeResidentesAsync(cancelacion);
        var rutaEscrita = await _almacenDeInformes.GuardarAsync(rutaDelArchivo, informe.Contenido, cancelacion);

        return informe with { RutaDelArchivo = rutaEscrita };
    }

    public async Task<InformeDePagos> PrepararInformeDePagosDeResidentesAsync(
        CancellationToken cancelacion = default)
    {
        var residentes = await _repositorio.ObtenerPorTipoAsync<VehiculoResidente>(cancelacion: cancelacion);

        var lineas = residentes
            .OrderBy(residente => residente.Placa)
            .Select(residente => new LineaDePagoDeResidente(
                residente.Placa, residente.MinutosAcumulados, residente.SaldoPendiente))
            .ToList();

        return new InformeDePagos(
            RutaDelArchivo: null,
            Contenido: GeneradorDeInformeDePagos.Formatear(lineas),
            Lineas: lineas,
            TotalDeMinutos: lineas.Sum(linea => linea.MinutosEstacionado),
            TotalAPagar: PoliticaDeImporte.Redondear(lineas.Sum(linea => linea.CantidadAPagar)));
    }

    private async Task<VehiculoDadoDeAlta> DarDeAltaAsync(
        string placa, Func<Placa, DateTime, Vehiculo> crear, CancellationToken cancelacion)
    {
        var numeroDePlaca = Placa.Crear(placa);

        if (await _repositorio.ObtenerPorPlacaAsync(numeroDePlaca, cancelacion) is { } existente)
        {
            throw new VehiculoYaRegistradoException(numeroDePlaca, existente.Tipo);
        }

        var vehiculo = crear(numeroDePlaca, _reloj.Ahora);
        await _repositorio.AgregarAsync(vehiculo, cancelacion);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        return new VehiculoDadoDeAlta(vehiculo.Placa, vehiculo.Tipo, vehiculo.FechaDeAlta);
    }
}
