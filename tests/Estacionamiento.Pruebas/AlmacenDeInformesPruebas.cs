using System.Text;
using Estacionamiento.Infraestructura.Archivos;

namespace Estacionamiento.Pruebas;

/// <summary>El informe acaba en un archivo de texto que abre una persona: los acentos importan.</summary>
public class AlmacenDeInformesPruebas : IDisposable
{
    private readonly string _carpeta = Path.Combine(
        Path.GetTempPath(), $"estacionamiento-pruebas-{Guid.NewGuid():N}");

    private readonly AlmacenDeInformesEnDisco _almacen = new();

    [Fact]
    public async Task Guarda_el_contenido_tal_cual_y_devuelve_la_ruta_absoluta()
    {
        var ruta = Path.Combine(_carpeta, "pagos.txt");
        const string contenido = "Núm. placa\nS1234A\n";

        var rutaEscrita = await _almacen.GuardarAsync(ruta, contenido);

        Assert.Equal(Path.GetFullPath(ruta), rutaEscrita);
        Assert.Equal(contenido, await File.ReadAllTextAsync(rutaEscrita));
    }

    [Fact]
    public async Task Crea_la_carpeta_del_informe_si_no_existe()
    {
        var ruta = Path.Combine(_carpeta, "informes", "2026", "agosto.txt");

        var rutaEscrita = await _almacen.GuardarAsync(ruta, "contenido");

        Assert.True(File.Exists(rutaEscrita));
    }

    [Fact]
    public async Task Escribe_en_UTF8_con_marca_de_orden_de_bytes()
    {
        var ruta = Path.Combine(_carpeta, "acentos.txt");

        await _almacen.GuardarAsync(ruta, "Núm. placa");

        var bytes = await File.ReadAllBytesAsync(ruta);

        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);
        Assert.Equal("Núm. placa", Encoding.UTF8.GetString(bytes[3..]));
    }

    [Fact]
    public async Task Un_informe_nuevo_reemplaza_al_anterior_sin_dejar_restos()
    {
        var ruta = Path.Combine(_carpeta, "pagos.txt");

        await _almacen.GuardarAsync(ruta, "una línea muy larga que ocupa bastante");
        await _almacen.GuardarAsync(ruta, "corta");

        Assert.Equal("corta", await File.ReadAllTextAsync(ruta));
    }

    public void Dispose()
    {
        if (Directory.Exists(_carpeta))
        {
            Directory.Delete(_carpeta, recursive: true);
        }
    }
}
