using System.Text;
using Estacionamiento.Aplicacion.Abstracciones;

namespace Estacionamiento.Infraestructura.Archivos;

/// <inheritdoc cref="IAlmacenDeInformes"/>
public sealed class AlmacenDeInformesEnDisco : IAlmacenDeInformes
{
    // Con marca de orden de bytes para que los acentos del encabezado se vean bien
    // también en editores que no detectan UTF-8 por su cuenta.
    private static readonly UTF8Encoding CodificacionDeSalida = new(encoderShouldEmitUTF8Identifier: true);

    public async Task<string> GuardarAsync(
        string ruta, string contenido, CancellationToken cancelacion = default)
    {
        var rutaAbsoluta = Path.GetFullPath(ruta);
        var carpeta = Path.GetDirectoryName(rutaAbsoluta);

        if (!string.IsNullOrEmpty(carpeta))
        {
            Directory.CreateDirectory(carpeta);
        }

        await File.WriteAllTextAsync(rutaAbsoluta, contenido, CodificacionDeSalida, cancelacion);

        return rutaAbsoluta;
    }
}
