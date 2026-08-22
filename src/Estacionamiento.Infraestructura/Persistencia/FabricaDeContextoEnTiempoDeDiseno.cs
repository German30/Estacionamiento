using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Estacionamiento.Infraestructura.Persistencia;

/// <summary>
/// Contexto que usa <c>dotnet ef</c> al generar migraciones, sin arrancar la aplicación.
/// Reutiliza el mismo cableado que en ejecución, para que el SQL de las migraciones sea
/// exactamente el que producirá la aplicación.
/// </summary>
/// <remarks>
/// Las migraciones son específicas del proveedor. Por omisión se generan para MySQL; para
/// otro manejador se indica al final de la orden, tras el separador <c>--</c>:
/// <code>
/// dotnet ef migrations add Inicial --project src/Estacionamiento.Infraestructura ^
///     --output-dir Persistencia/Migraciones -- --proveedor Sqlite
/// </code>
/// </remarks>
public sealed class FabricaDeContextoEnTiempoDeDiseno : IDesignTimeDbContextFactory<EstacionamientoDbContext>
{
    public EstacionamientoDbContext CreateDbContext(string[] args)
    {
        var opciones = new OpcionesDePersistencia { Proveedor = LeerProveedor(args) };

        opciones.CadenaDeConexion = opciones.Proveedor switch
        {
            ProveedorDePersistencia.Sqlite => "Data Source=estacionamiento.db",
            ProveedorDePersistencia.SqlServer => @"Server=(localdb)\MSSQLLocalDB;Database=Estacionamiento",
            _ => opciones.CadenaDeConexion
        };

        var constructor = new DbContextOptionsBuilder<EstacionamientoDbContext>();
        ExtensionesDeServicios.ConfigurarProveedor(constructor, opciones);

        return new EstacionamientoDbContext(constructor.Options);
    }

    private static ProveedorDePersistencia LeerProveedor(string[] args)
    {
        var indice = Array.IndexOf(args, "--proveedor");

        return indice >= 0 && indice + 1 < args.Length
               && Enum.TryParse<ProveedorDePersistencia>(args[indice + 1], ignoreCase: true, out var proveedor)
            ? proveedor
            : ProveedorDePersistencia.MySql;
    }
}
