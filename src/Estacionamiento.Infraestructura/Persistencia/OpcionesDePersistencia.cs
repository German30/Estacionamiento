namespace Estacionamiento.Infraestructura.Persistencia;

/// <summary>Manejador de base de datos en uso. Añadir uno nuevo es añadir un caso en
/// <see cref="ExtensionesDeServicios"/>; ni el dominio ni la aplicación se enteran.</summary>
public enum ProveedorDePersistencia
{
    /// <summary>MySQL / MariaDB. Es el manejador de la aplicación.</summary>
    MySql,

    /// <summary>Archivo local. Cómodo para pruebas y para trabajar sin servidor.</summary>
    Sqlite,

    SqlServer
}

/// <summary>Cómo se pone al día el esquema de la base de datos al arrancar.</summary>
public enum EstrategiaDeEsquema
{
    /// <summary>Aplica las migraciones pendientes. Requiere migraciones generadas para el proveedor en uso.</summary>
    Migraciones,

    /// <summary>Crea el esquema desde el modelo si la base no existe. Útil al estrenar un proveedor.</summary>
    CrearSiNoExiste,

    /// <summary>No toca el esquema; lo gestiona un DBA por fuera.</summary>
    Ninguna
}

/// <summary>Configuración de conexión, leída de la sección "Persistencia" de appsettings.json.</summary>
public sealed class OpcionesDePersistencia
{
    public const string Seccion = "Persistencia";

    public ProveedorDePersistencia Proveedor { get; set; } = ProveedorDePersistencia.MySql;

    public string CadenaDeConexion { get; set; } =
        "Server=127.0.0.1;Port=3306;Database=estacionamiento;User Id=root;Password=root123;";

    /// <summary>
    /// Versión del servidor MySQL, necesaria para que el proveedor genere el SQL adecuado.
    /// Si se deja vacío se detecta sola, a costa de abrir una conexión al arrancar.
    /// </summary>
    public string VersionDelServidor { get; set; } = "8.1.0";

    public EstrategiaDeEsquema EstrategiaDeEsquema { get; set; } = EstrategiaDeEsquema.Migraciones;

    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(CadenaDeConexion))
        {
            throw new InvalidOperationException(
                $"Falta la cadena de conexión. Defina \"{Seccion}:{nameof(CadenaDeConexion)}\" en appsettings.json.");
        }

        if (!Enum.IsDefined(Proveedor))
        {
            throw new InvalidOperationException(
                "Proveedor de persistencia no reconocido. Valores admitidos: " +
                $"{string.Join(", ", Enum.GetNames<ProveedorDePersistencia>())}.");
        }
    }
}
