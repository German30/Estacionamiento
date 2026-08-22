using Estacionamiento.Infraestructura.Persistencia;

namespace Estacionamiento.Web.Infraestructura;

/// <summary>Configuración de la siembra de datos de demostración, sección "Siembra".</summary>
public sealed class OpcionesDeSiembra
{
    public const string Seccion = "Siembra";

    /// <summary>Vehículos a generar. <c>0</c> —lo normal— desactiva la siembra.</summary>
    public int Cantidad { get; set; }

    /// <summary>
    /// Vacía las tablas antes de sembrar. Sin esto, una base que ya tiene vehículos se deja
    /// intacta: sembrar encima duplicaría el padrón en cada arranque.
    /// </summary>
    public bool Reiniciar { get; set; }
}

/// <summary>
/// Puebla la base con un juego de datos realista al arrancar, si se pide por configuración.
/// </summary>
/// <remarks>
/// Va por configuración y no por un endpoint a propósito: sembrar con <c>Reiniciar</c> borra
/// todos los vehículos, y eso no debe poder dispararlo cualquiera que alcance el puerto. Como
/// variable de entorno queda en manos de quien despliega, que es de quien es la decisión.
///
/// Es idempotente: con una base que ya tiene vehículos no hace nada, así que dejar
/// <c>Siembra__Cantidad</c> puesto no reconstruye los datos en cada reinicio del contenedor.
/// </remarks>
public static class SiembraDeDemostracion
{
    public static async Task SembrarSiSePideAsync(this WebApplication aplicacion, CancellationToken cancelacion = default)
    {
        var opciones = aplicacion.Configuration.GetSection(OpcionesDeSiembra.Seccion)
                           .Get<OpcionesDeSiembra>()
                       ?? new OpcionesDeSiembra();

        if (opciones.Cantidad <= 0)
        {
            return;
        }

        using var ambito = aplicacion.Services.CreateScope();

        var sembrador = ambito.ServiceProvider.GetRequiredService<SembradorDeDatos>();
        var registro = ambito.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(SiembraDeDemostracion));

        if (await sembrador.HayDatosAsync(cancelacion))
        {
            if (!opciones.Reiniciar)
            {
                registro.LogInformation(
                    "Se omite la siembra: la base ya tiene vehículos. Use {Clave}=true para vaciarla antes.",
                    $"{OpcionesDeSiembra.Seccion}__{nameof(OpcionesDeSiembra.Reiniciar)}");

                return;
            }

            var borrados = await sembrador.VaciarAsync(cancelacion);
            registro.LogWarning("Siembra con reinicio: se eliminaron {Cantidad} vehículos y sus estancias.", borrados);
        }

        var resumen = await sembrador.SembrarAsync(opciones.Cantidad, cancelacion);

        registro.LogInformation(
            "Datos de demostración sembrados: {Vehiculos} vehículos ({Oficiales} oficiales, " +
            "{Residentes} residentes, {NoResidentes} no residentes), {Estancias} estancias, " +
            "{Dentro} dentro ahora mismo.",
            resumen.Vehiculos, resumen.Oficiales, resumen.Residentes, resumen.NoResidentes,
            resumen.Estancias, resumen.VehiculosDentro);
    }
}
