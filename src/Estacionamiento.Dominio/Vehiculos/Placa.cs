using System.Diagnostics.CodeAnalysis;
using Estacionamiento.Dominio.Excepciones;

namespace Estacionamiento.Dominio.Vehiculos;

/// <summary>
/// Número de placa de un vehículo. Es la identidad de negocio: el empleado sólo
/// teclea la placa, así que aquí viven la normalización y la validación para que
/// "abc-1234", " ABC1234 " y "ABC1234" sean el mismo vehículo.
/// </summary>
public readonly record struct Placa : IComparable<Placa>
{
    public const int LongitudMinima = 5;
    public const int LongitudMaxima = 10;

    private readonly string? _valor;

    private Placa(string valor) => _valor = valor;

    /// <summary>Texto normalizado de la placa (mayúsculas, sin espacios ni guiones).</summary>
    public string Valor => _valor ?? string.Empty;

    /// <summary>Normaliza y valida. Lanza <see cref="PlacaInvalidaException"/> si no es una placa aceptable.</summary>
    public static Placa Crear(string? entrada)
    {
        if (!TryCrear(entrada, out var placa, out var error))
        {
            throw new PlacaInvalidaException(error);
        }

        return placa;
    }

    /// <summary>Variante sin excepciones, para validar la entrada del empleado antes de actuar.</summary>
    public static bool TryCrear(string? entrada, out Placa placa, [NotNullWhen(false)] out string? error)
    {
        placa = default;

        if (string.IsNullOrWhiteSpace(entrada))
        {
            error = "El número de placa no puede estar vacío.";
            return false;
        }

        var normalizada = Normalizar(entrada);

        if (normalizada.Length < LongitudMinima || normalizada.Length > LongitudMaxima)
        {
            error = $"El número de placa debe tener entre {LongitudMinima} y {LongitudMaxima} " +
                    $"caracteres alfanuméricos (recibido: \"{entrada.Trim()}\").";
            return false;
        }

        if (!normalizada.All(char.IsAsciiLetterOrDigit))
        {
            error = $"El número de placa sólo admite letras y dígitos (recibido: \"{entrada.Trim()}\").";
            return false;
        }

        placa = new Placa(normalizada);
        error = null;
        return true;
    }

    /// <summary>
    /// Reconstruye una placa ya persistida sin volver a validarla. Uso exclusivo del
    /// conversor de Entity Framework Core: los datos guardados se dan por buenos, y
    /// revalidarlos rompería la lectura si alguna vez cambian las reglas.
    /// </summary>
    public static Placa DesdeAlmacenamiento(string valor) => new(valor);

    private static string Normalizar(string entrada)
    {
        Span<char> destino = stackalloc char[entrada.Length];
        var escritos = 0;

        foreach (var caracter in entrada)
        {
            if (char.IsWhiteSpace(caracter) || caracter is '-' or '_' or '.')
            {
                continue;
            }

            destino[escritos++] = char.ToUpperInvariant(caracter);
        }

        return new string(destino[..escritos]);
    }

    public int CompareTo(Placa otra) => string.CompareOrdinal(Valor, otra.Valor);

    public override string ToString() => Valor;

    public static implicit operator string(Placa placa) => placa.Valor;
}
