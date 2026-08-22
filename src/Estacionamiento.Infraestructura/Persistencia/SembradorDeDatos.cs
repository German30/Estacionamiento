using Estacionamiento.Dominio.Comun;
using Estacionamiento.Dominio.Vehiculos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Estacionamiento.Infraestructura.Persistencia;

/// <summary>Qué se sembró.</summary>
public sealed record ResumenDeSiembra(
    int Oficiales,
    int Residentes,
    int NoResidentes,
    int Estancias,
    int VehiculosDentro,
    int MinutosAcumuladosDeResidentes,
    decimal ImporteCobradoANoResidentes)
{
    public int Vehiculos => Oficiales + Residentes + NoResidentes;
}

/// <summary>
/// Genera un juego de datos de demostración.
/// </summary>
/// <remarks>
/// Las estancias se crean llamando a <see cref="Vehiculo.RegistrarEntrada"/> y
/// <see cref="Vehiculo.RegistrarSalida"/>, no insertando filas a mano: los importes cobrados y
/// los minutos acumulados los calcula el dominio con las mismas reglas que en producción, así
/// que los datos sembrados no pueden contradecir a las tarifas.
///
/// La semilla del generador es fija, de modo que sembrar dos veces produce exactamente el mismo
/// juego de datos y las capturas o informes de una demostración son reproducibles.
/// </remarks>
public sealed class SembradorDeDatos
{
    /// <summary>Semilla fija: misma entrada, mismos datos.</summary>
    private const int Semilla = 20260821;

    private const int DiasDeHistorial = 28;

    private static readonly char[] Letras = "ABCDEFGHJKLMNPRSTUVWXYZ".ToCharArray(); // sin I, O, Q: se confunden

    private readonly EstacionamientoDbContext _contexto;
    private readonly IReloj _reloj;
    private readonly ILogger<SembradorDeDatos> _registro;

    public SembradorDeDatos(
        EstacionamientoDbContext contexto, IReloj reloj, ILogger<SembradorDeDatos> registro)
    {
        _contexto = contexto;
        _reloj = reloj;
        _registro = registro;
    }

    public Task<bool> HayDatosAsync(CancellationToken cancelacion = default) =>
        _contexto.Vehiculos.AnyAsync(cancelacion);

    /// <summary>Borra todos los vehículos y, en cascada, sus estancias.</summary>
    public async Task<int> VaciarAsync(CancellationToken cancelacion = default)
    {
        var borrados = await _contexto.Vehiculos.ExecuteDeleteAsync(cancelacion);
        _registro.LogInformation("Se eliminaron {Cantidad} vehículos y sus estancias.", borrados);

        return borrados;
    }

    public async Task<ResumenDeSiembra> SembrarAsync(
        int cantidadDeVehiculos, CancellationToken cancelacion = default)
    {
        if (cantidadDeVehiculos <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cantidadDeVehiculos), cantidadDeVehiculos,
                "La cantidad de vehículos a sembrar debe ser mayor que cero.");
        }

        var azar = new Random(Semilla);
        var placasUsadas = new HashSet<string>(StringComparer.Ordinal);

        // Mezcla parecida a la de un estacionamiento real: mayoría de visitantes, un grupo
        // estable de residentes y unos pocos vehículos oficiales.
        var oficiales = Math.Max(1, cantidadDeVehiculos * 15 / 100);
        var residentes = Math.Max(1, cantidadDeVehiculos * 35 / 100);
        var noResidentes = cantidadDeVehiculos - oficiales - residentes;

        var ahora = _reloj.Ahora;
        var inicioDelHistorial = ahora.AddDays(-DiasDeHistorial);

        var vehiculos = new List<Vehiculo>(cantidadDeVehiculos);

        for (var i = 0; i < cantidadDeVehiculos; i++)
        {
            var placa = Placa.Crear(GenerarPlacaUnica(azar, placasUsadas));
            var fechaDeAlta = inicioDelHistorial.AddMinutes(-azar.Next(0, 120 * 24));

            vehiculos.Add(i switch
            {
                _ when i < oficiales => new VehiculoOficial(placa, fechaDeAlta),
                _ when i < oficiales + residentes => new VehiculoResidente(placa, fechaDeAlta),
                _ => new VehiculoNoResidente(placa, fechaDeAlta)
            });
        }

        var totalDeEstancias = 0;
        var vehiculosDentro = 0;

        foreach (var vehiculo in vehiculos)
        {
            var (estancias, sigueDentro) = GenerarEstancias(vehiculo, azar, inicioDelHistorial, ahora);

            totalDeEstancias += estancias;

            if (sigueDentro)
            {
                vehiculosDentro++;
            }
        }

        await _contexto.Vehiculos.AddRangeAsync(vehiculos, cancelacion);
        await _contexto.SaveChangesAsync(cancelacion);

        var resumen = new ResumenDeSiembra(
            oficiales,
            residentes,
            noResidentes,
            totalDeEstancias,
            vehiculosDentro,
            vehiculos.OfType<VehiculoResidente>().Sum(residente => residente.MinutosAcumulados),
            vehiculos.SelectMany(v => v.Estancias).Sum(estancia => estancia.ImporteCobrado));

        _registro.LogInformation(
            "Sembrados {Vehiculos} vehículos y {Estancias} estancias.",
            resumen.Vehiculos, resumen.Estancias);

        return resumen;
    }

    /// <summary>
    /// Recorre el historial hacia delante abriendo y cerrando estancias. El cursor nunca
    /// retrocede, así que un vehículo jamás acaba con dos estancias solapadas.
    /// </summary>
    private static (int Estancias, bool SigueDentro) GenerarEstancias(
        Vehiculo vehiculo, Random azar, DateTime inicio, DateTime ahora)
    {
        // Los oficiales entran menos: son unos pocos vehículos de servicio.
        var visitasPrevistas = vehiculo is VehiculoOficial ? azar.Next(1, 5) : azar.Next(1, 8);

        var cursor = inicio.AddMinutes(azar.Next(0, 60 * 24));
        var estancias = 0;

        for (var visita = 0; visita < visitasPrevistas; visita++)
        {
            var entrada = cursor.AddMinutes(azar.Next(60, 60 * 72));

            // Duración típica: de un recado de un cuarto de hora a una jornada de diez horas.
            var duracion = TimeSpan.FromMinutes(azar.Next(15, 600));

            if (entrada.Add(duracion) >= ahora)
            {
                break; // Ya no cabe una estancia completa antes de "ahora".
            }

            vehiculo.RegistrarEntrada(entrada);
            vehiculo.RegistrarSalida(entrada.Add(duracion));

            cursor = entrada.Add(duracion);
            estancias++;
        }

        // Uno de cada ocho vehículos se queda dentro, para que el juego de datos incluya
        // estancias abiertas y se pueda probar "registrar salida" nada más sembrar.
        if (azar.Next(0, 8) != 0)
        {
            return (estancias, SigueDentro: false);
        }

        var entradaAbierta = ahora.AddMinutes(-azar.Next(10, 300));

        if (entradaAbierta <= cursor)
        {
            return (estancias, SigueDentro: false);
        }

        vehiculo.RegistrarEntrada(entradaAbierta);

        return (estancias + 1, SigueDentro: true);
    }

    /// <summary>Placas con formato mexicano habitual (tres letras, tres dígitos y una letra).</summary>
    private static string GenerarPlacaUnica(Random azar, HashSet<string> usadas)
    {
        while (true)
        {
            var placa = string.Create(7, azar, static (destino, generador) =>
            {
                for (var i = 0; i < 3; i++)
                {
                    destino[i] = Letras[generador.Next(Letras.Length)];
                }

                for (var i = 3; i < 6; i++)
                {
                    destino[i] = (char)('0' + generador.Next(10));
                }

                destino[6] = Letras[generador.Next(Letras.Length)];
            });

            if (usadas.Add(placa))
            {
                return placa;
            }
        }
    }
}
