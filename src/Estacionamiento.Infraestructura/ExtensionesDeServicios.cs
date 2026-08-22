using Estacionamiento.Aplicacion.Abstracciones;
using Estacionamiento.Aplicacion.Consultas;
using Estacionamiento.Dominio.Comun;
using Estacionamiento.Infraestructura.Archivos;
using Estacionamiento.Infraestructura.Persistencia;
using Estacionamiento.Infraestructura.Tiempo;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Estacionamiento.Infraestructura;

public static class ExtensionesDeServicios
{
    /// <summary>
    /// Registra la persistencia y los servicios de apoyo. El manejador de base de datos se
    /// elige por configuración: cambiarlo no obliga a recompilar nada más que este archivo.
    /// </summary>
    public static IServiceCollection AgregarInfraestructura(
        this IServiceCollection servicios, IConfiguration configuracion)
    {
        var opciones = configuracion.GetSection(OpcionesDePersistencia.Seccion)
                           .Get<OpcionesDePersistencia>()
                       ?? new OpcionesDePersistencia();

        opciones.Validar();

        servicios.AddSingleton(opciones);

        servicios.AddDbContext<EstacionamientoDbContext>(constructor =>
            ConfigurarProveedor(constructor, opciones));

        servicios.AddScoped<IRepositorioVehiculos, RepositorioVehiculos>();
        servicios.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
        servicios.AddScoped<IConsultasEstacionamiento, ConsultasEstacionamiento>();
        servicios.AddScoped<InicializadorBaseDeDatos>();

        servicios.AddSingleton<IReloj, RelojDelSistema>();
        servicios.AddSingleton<IAlmacenDeInformes, AlmacenDeInformesEnDisco>();

        return servicios;
    }

    /// <summary>Traduce la configuración al proveedor concreto de Entity Framework Core.</summary>
    internal static void ConfigurarProveedor(
        DbContextOptionsBuilder constructor, OpcionesDePersistencia opciones)
    {
        var ensamblado = typeof(EstacionamientoDbContext).Assembly.GetName().Name;

        switch (opciones.Proveedor)
        {
            case ProveedorDePersistencia.MySql:
                constructor.UseMySql(
                    opciones.CadenaDeConexion,
                    ResolverVersionDeMySql(opciones),
                    mysql => mysql.MigrationsAssembly(ensamblado));
                break;

            case ProveedorDePersistencia.SqlServer:
                constructor.UseSqlServer(
                    opciones.CadenaDeConexion,
                    sql => sql.MigrationsAssembly(ensamblado));
                break;

            case ProveedorDePersistencia.Sqlite:
            default:
                constructor.UseSqlite(
                    AnclarRutaDeSqlite(opciones.CadenaDeConexion),
                    sqlite => sqlite.MigrationsAssembly(ensamblado));
                break;
        }
    }

    /// <summary>
    /// El proveedor de MySQL necesita saber contra qué versión de servidor genera el SQL.
    /// Se declara en la configuración para no pagar una conexión de sondeo en cada arranque;
    /// dejarla vacía activa la detección automática.
    /// </summary>
    private static ServerVersion ResolverVersionDeMySql(OpcionesDePersistencia opciones)
    {
        if (string.IsNullOrWhiteSpace(opciones.VersionDelServidor)
            || opciones.VersionDelServidor.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            return ServerVersion.AutoDetect(opciones.CadenaDeConexion);
        }

        return ServerVersion.Parse(opciones.VersionDelServidor);
    }

    /// <summary>
    /// SQLite resuelve las rutas relativas contra el directorio de trabajo del proceso, así que
    /// lanzar la aplicación desde otra carpeta abriría una base de datos distinta —y vacía—.
    /// Se ancla al directorio del ejecutable para que se vean siempre los mismos datos.
    /// </summary>
    private static string AnclarRutaDeSqlite(string cadenaDeConexion)
    {
        var constructor = new SqliteConnectionStringBuilder(cadenaDeConexion);
        var origen = constructor.DataSource;

        var esEnMemoria = string.IsNullOrEmpty(origen)
                          || origen.Equals(":memory:", StringComparison.OrdinalIgnoreCase);

        if (esEnMemoria || Path.IsPathRooted(origen))
        {
            return cadenaDeConexion;
        }

        constructor.DataSource = Path.Combine(AppContext.BaseDirectory, origen);
        return constructor.ToString();
    }
}
