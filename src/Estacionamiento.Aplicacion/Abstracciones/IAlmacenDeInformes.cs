namespace Estacionamiento.Aplicacion.Abstracciones;

/// <summary>
/// Escritura de informes. Se abstrae para que el formato del informe (lógica de aplicación,
/// verificable con pruebas) quede separado de dónde y cómo se guarda el archivo.
/// </summary>
public interface IAlmacenDeInformes
{
    /// <summary>Guarda el informe y devuelve la ruta absoluta del archivo escrito.</summary>
    Task<string> GuardarAsync(string ruta, string contenido, CancellationToken cancelacion = default);
}
