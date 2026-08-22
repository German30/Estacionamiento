using Estacionamiento.Aplicacion.Servicios;
using Microsoft.Extensions.DependencyInjection;

namespace Estacionamiento.Aplicacion;

public static class ExtensionesDeServicios
{
    /// <summary>Registra los casos de uso. La infraestructura aporta el resto de dependencias.</summary>
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        servicios.AddScoped<IServicioEstacionamiento, ServicioEstacionamiento>();
        return servicios;
    }
}
