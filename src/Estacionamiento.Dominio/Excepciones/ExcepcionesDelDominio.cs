using Estacionamiento.Dominio.Vehiculos;

namespace Estacionamiento.Dominio.Excepciones;

/// <summary>Base de todos los errores de regla de negocio. La capa de presentación
/// los captura para mostrarlos al empleado sin volcar una traza.</summary>
public abstract class ExcepcionDelDominio : Exception
{
    protected ExcepcionDelDominio(string mensaje) : base(mensaje) { }
}

public sealed class PlacaInvalidaException : ExcepcionDelDominio
{
    public PlacaInvalidaException(string mensaje) : base(mensaje) { }
}

public sealed class VehiculoYaRegistradoException : ExcepcionDelDominio
{
    public VehiculoYaRegistradoException(Placa placa, string tipoExistente)
        : base($"La placa {placa} ya está registrada como vehículo {tipoExistente.ToLowerInvariant()}.")
    {
        Placa = placa;
        TipoExistente = tipoExistente;
    }

    public Placa Placa { get; }
    public string TipoExistente { get; }
}

public sealed class VehiculoYaEstacionadoException : ExcepcionDelDominio
{
    public VehiculoYaEstacionadoException(Placa placa, DateTime desde)
        : base($"La placa {placa} ya tiene una entrada abierta desde el {desde:dd/MM/yyyy HH:mm}. " +
               "Registre primero su salida.")
    {
        Placa = placa;
        Desde = desde;
    }

    public Placa Placa { get; }
    public DateTime Desde { get; }
}

public sealed class VehiculoNoEstacionadoException : ExcepcionDelDominio
{
    public VehiculoNoEstacionadoException(Placa placa)
        : base($"La placa {placa} no tiene ninguna entrada abierta, no se puede registrar su salida.")
    {
        Placa = placa;
    }

    public Placa Placa { get; }
}

public sealed class SalidaAnteriorALaEntradaException : ExcepcionDelDominio
{
    public SalidaAnteriorALaEntradaException(DateTime entrada, DateTime salida)
        : base($"La hora de salida ({salida:dd/MM/yyyy HH:mm:ss}) es anterior a la de entrada " +
               $"({entrada:dd/MM/yyyy HH:mm:ss}).")
    {
    }
}
