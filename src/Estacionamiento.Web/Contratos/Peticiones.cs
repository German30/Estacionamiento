using System.ComponentModel.DataAnnotations;

namespace Estacionamiento.Web.Contratos;

/// <summary>
/// Cuerpo de las operaciones que sólo necesitan una placa: entrada, salida y las dos altas.
/// El empleado teclea la placa como se la dictan; normalizarla es cosa del dominio
/// (<c>Placa.Crear</c>), así que aquí sólo se comprueba que venga algo.
/// </summary>
/// <param name="Placa">Placa del vehículo. Admite minúsculas, espacios y guiones: "abc-1234" y "ABC1234" son la misma.</param>
public sealed record PeticionConPlaca(
    [Required(ErrorMessage = "El número de placa es obligatorio.")]
    string Placa);

/// <summary>Cuerpo del cierre de mes.</summary>
/// <param name="Confirmacion">
/// Debe ser exactamente <c>COMENZAR</c>. El cierre borra las estancias de los vehículos
/// oficiales y pone a cero el tiempo acumulado de los residentes: es irreversible, así que se
/// exige teclear la palabra en lugar de bastar con acertar la ruta.
/// </param>
public sealed record PeticionDeCierreDeMes(
    [Required(ErrorMessage = "Escriba COMENZAR para confirmar el cierre de mes.")]
    string Confirmacion)
{
    /// <summary>Palabra que hay que teclear para que el cierre proceda.</summary>
    public const string PalabraDeConfirmacion = "COMENZAR";

    public bool EstaConfirmado =>
        string.Equals(Confirmacion?.Trim(), PalabraDeConfirmacion, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Cuerpo de la petición que deja el informe escrito en el disco del servidor.</summary>
/// <param name="Ruta">
/// Ruta del archivo a escribir, vista desde el servidor. En el contenedor, <c>/informes</c>
/// está montado contra el anfitrión: <c>/informes/pagos-agosto.txt</c> aparece en <c>./informes</c>.
/// </param>
public sealed record PeticionDeInformeEnDisco(
    [Required(ErrorMessage = "Indique la ruta del archivo a escribir.")]
    string Ruta);
