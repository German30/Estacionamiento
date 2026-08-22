using Estacionamiento.Aplicacion.Contratos;
using Estacionamiento.Dominio.Vehiculos;

namespace Estacionamiento.Aplicacion.Servicios;

/// <summary>Los seis casos de uso del enunciado, en el orden en que los describe.</summary>
public interface IServicioEstacionamiento
{
    /// <summary>Registra la entrada de una placa. Si la placa no está dada de alta como
    /// oficial ni como residente, se registra como vehículo no residente.</summary>
    Task<RegistroDeEntrada> RegistrarEntradaAsync(string placa, CancellationToken cancelacion = default);

    /// <summary>Registra la salida de una placa y devuelve qué debe cobrar el empleado.</summary>
    Task<ResultadoSalida> RegistrarSalidaAsync(string placa, CancellationToken cancelacion = default);

    Task<VehiculoDadoDeAlta> DarDeAltaVehiculoOficialAsync(string placa, CancellationToken cancelacion = default);

    Task<VehiculoDadoDeAlta> DarDeAltaVehiculoDeResidenteAsync(string placa, CancellationToken cancelacion = default);

    /// <summary>Elimina las estancias de los vehículos oficiales y pone a cero el tiempo
    /// acumulado por los vehículos de residentes.</summary>
    Task<ResumenDeComienzoDeMes> ComenzarMesAsync(CancellationToken cancelacion = default);

    /// <summary>Genera el informe de pagos de residentes en el archivo indicado.</summary>
    Task<InformeDePagos> GenerarInformeDePagosDeResidentesAsync(
        string rutaDelArchivo, CancellationToken cancelacion = default);

    /// <summary>
    /// Calcula el informe y devuelve su contenido sin escribirlo. Lo usa la web, que sirve el
    /// archivo como descarga en lugar de dejarlo en el disco del servidor.
    /// </summary>
    Task<InformeDePagos> PrepararInformeDePagosDeResidentesAsync(CancellationToken cancelacion = default);
}
