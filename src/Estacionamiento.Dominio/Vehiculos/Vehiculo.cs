using Estacionamiento.Dominio.Estancias;
using Estacionamiento.Dominio.Excepciones;

namespace Estacionamiento.Dominio.Vehiculos;

/// <summary>
/// Vehículo que usa el estacionamiento. Concentra lo común (placa, estancias, entrada y
/// salida) y delega en cada subclase lo único que cambia entre tipos: qué ocurre al cerrar
/// una estancia y qué significa "comienza mes" para ese tipo.
/// </summary>
/// <remarks>
/// Punto de extensión del enunciado. Para dar de alta un tipo nuevo de vehículo basta con:
/// <list type="number">
///   <item>heredar de esta clase e implementar <see cref="AlCerrarEstancia"/>;</item>
///   <item>declarar su discriminador en <c>VehiculoConfiguracion</c> (capa de infraestructura);</item>
///   <item>generar una migración.</item>
/// </list>
/// Ni el servicio de aplicación ni los repositorios necesitan cambio alguno.
/// </remarks>
public abstract class Vehiculo
{
    private readonly List<Estancia> _estancias = new();

    protected Vehiculo() { } // Requerido por Entity Framework Core.

    protected Vehiculo(Placa placa, DateTime fechaDeAlta)
    {
        Placa = placa;
        FechaDeAlta = fechaDeAlta;
    }

    public int Id { get; private set; }

    public Placa Placa { get; private set; }

    public DateTime FechaDeAlta { get; private set; }

    public IReadOnlyList<Estancia> Estancias => _estancias;

    /// <summary>Nombre legible del tipo, para mensajes e informes.</summary>
    public abstract string Tipo { get; }

    /// <summary>Tarifa aplicable, en MXN por minuto.</summary>
    public abstract decimal TarifaPorMinuto { get; }

    /// <summary>Cuándo se le cobra a este tipo de vehículo.</summary>
    public abstract MomentoDeCobro MomentoDeCobro { get; }

    /// <summary>Estancia en curso, o <c>null</c> si el vehículo no está dentro del estacionamiento.</summary>
    public Estancia? EstanciaAbierta => _estancias.SingleOrDefault(estancia => estancia.EstaAbierta);

    /// <summary>Caso de uso "registra entrada": apunta la hora de entrada del vehículo.</summary>
    public Estancia RegistrarEntrada(DateTime momento)
    {
        if (EstanciaAbierta is { } abierta)
        {
            throw new VehiculoYaEstacionadoException(Placa, abierta.Entrada);
        }

        var estancia = Estancia.Abrir(this, momento);
        _estancias.Add(estancia);
        return estancia;
    }

    /// <summary>Caso de uso "registra salida": cierra la estancia y aplica lo que corresponda al tipo.</summary>
    public ResultadoSalida RegistrarSalida(DateTime momento)
    {
        var estancia = EstanciaAbierta ?? throw new VehiculoNoEstacionadoException(Placa);
        estancia.Cerrar(momento);
        return AlCerrarEstancia(estancia);
    }

    /// <summary>
    /// Caso de uso "comienza mes". Por omisión no hay nada que reiniciar: un tipo que cobra
    /// a la salida cierra cuentas en cada estancia y no arrastra saldo entre meses.
    /// </summary>
    public virtual void ComenzarMes() { }

    /// <summary>Qué hace este tipo de vehículo cuando una de sus estancias se cierra.</summary>
    protected abstract ResultadoSalida AlCerrarEstancia(Estancia estancia);

    /// <summary>
    /// Descarta las estancias ya cerradas conservando la que esté abierta, si la hay:
    /// si el mes comienza con un vehículo dentro, borrar su entrada impediría cobrarle al salir.
    /// </summary>
    protected int EliminarEstanciasCerradas() => _estancias.RemoveAll(estancia => !estancia.EstaAbierta);
}
