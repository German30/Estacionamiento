namespace Estacionamiento.Web.Contratos;

/// <summary>
/// Lo que la API devuelve. Son tipos propios y no los del dominio a propósito: la forma del
/// JSON es un contrato con quien consume la API, y no debe moverse porque el dominio se
/// reorganice por dentro. <see cref="Proyecciones"/> hace la traducción en un solo sitio.
/// </summary>
/// <param name="Placa">Placa ya normalizada.</param>
/// <param name="TipoDeVehiculo">"Oficial", "Residente" o "No residente".</param>
/// <param name="Entrada">Momento en que quedó registrada la entrada.</param>
/// <param name="VehiculoRecienCreado">
/// Cierto si la placa era desconocida y se dio de alta ahora como no residente. Merece avisarse:
/// suele significar que el empleado tecleó mal la placa de un residente.
/// </param>
public sealed record EntradaRegistrada(
    string Placa,
    string TipoDeVehiculo,
    DateTime Entrada,
    bool VehiculoRecienCreado);

/// <summary>Lo que hay que cobrar (o no) tras registrar una salida.</summary>
/// <param name="MinutosFacturables">Minutos de la estancia, redondeados según la política del dominio.</param>
/// <param name="MomentoDeCobro">"Ninguno", "ALaSalida" o "AFinDeMes".</param>
/// <param name="ImporteACobrarAhora">Importe en MXN que el empleado cobra en este momento. 0 si no procede.</param>
/// <param name="MinutosAcumulados">Minutos del mes en curso. Sólo para quien liquida a fin de mes.</param>
/// <param name="SaldoPendiente">Importe en MXN que deberá liquidar a fin de mes. Sólo para residentes.</param>
public sealed record SalidaRegistrada(
    string Placa,
    string TipoDeVehiculo,
    DateTime Entrada,
    DateTime Salida,
    int MinutosFacturables,
    string MomentoDeCobro,
    decimal ImporteACobrarAhora,
    int? MinutosAcumulados,
    decimal? SaldoPendiente);

/// <summary>Resultado de dar de alta un vehículo oficial o de residente.</summary>
public sealed record VehiculoDadoDeAlta(
    string Placa,
    string TipoDeVehiculo,
    DateTime FechaDeAlta);

/// <summary>Una fila de cualquiera de los listados de vehículos.</summary>
/// <param name="DentroDesde">Momento de la entrada abierta, o <c>null</c> si el vehículo no está dentro.</param>
/// <param name="MinutosDentro">Minutos que lleva dentro ahora mismo. 0 si no está dentro.</param>
public sealed record VehiculoEnLista(
    string Placa,
    string Tipo,
    string MomentoDeCobro,
    bool EstaDentro,
    DateTime? DentroDesde,
    int MinutosDentro,
    int? MinutosAcumulados,
    decimal? SaldoPendiente,
    int TotalDeEstancias);

/// <summary>Ficha de un vehículo con su historial de estancias.</summary>
/// <param name="TarifaPorMinuto">MXN por minuto que le corresponde a su tipo.</param>
public sealed record DetalleDeVehiculo(
    string Placa,
    string Tipo,
    string MomentoDeCobro,
    decimal TarifaPorMinuto,
    DateTime FechaDeAlta,
    bool EstaDentro,
    int? MinutosAcumulados,
    decimal? SaldoPendiente,
    int TotalDeMinutos,
    decimal TotalCobrado,
    IReadOnlyList<EstanciaEnLista> Estancias);

/// <summary>Una estancia del historial. <paramref name="Salida"/> nula significa que sigue abierta.</summary>
public sealed record EstanciaEnLista(
    DateTime Entrada,
    DateTime? Salida,
    int Minutos,
    decimal ImporteCobrado,
    bool EstaAbierta);

/// <summary>Estado del estacionamiento de un vistazo.</summary>
public sealed record PanelDeControl(
    int VehiculosDentro,
    int TotalDeVehiculos,
    int Oficiales,
    int Residentes,
    int NoResidentes,
    int MinutosAcumuladosDeResidentes,
    decimal SaldoPendienteDeResidentes,
    int SalidasDeHoy,
    decimal CobradoHoy);

/// <summary>Una línea del informe de pagos de residentes.</summary>
public sealed record LineaDePagoDeResidente(
    string Placa,
    int MinutosEstacionado,
    decimal CantidadAPagar);

/// <summary>
/// Informe de pagos de residentes. <paramref name="Contenido"/> trae el informe ya formateado
/// (columnas de ancho fijo, dos decimales) tal cual se descarga; las líneas van aparte para
/// quien prefiera pintarlo por su cuenta.
/// </summary>
/// <param name="RutaDelArchivo">Ruta del archivo escrito, o <c>null</c> si sólo se calculó.</param>
public sealed record InformeDePagos(
    string? RutaDelArchivo,
    string Contenido,
    IReadOnlyList<LineaDePagoDeResidente> Lineas,
    int TotalDeMinutos,
    decimal TotalAPagar);

/// <summary>Lo que el cierre de mes se llevó por delante.</summary>
/// <param name="VehiculosDentroConservados">
/// Vehículos que estaban dentro al cerrar: conservan su entrada abierta y saldrán con normalidad.
/// </param>
public sealed record ResumenDeComienzoDeMes(
    int VehiculosOficialesAfectados,
    int EstanciasEliminadas,
    int ResidentesReiniciados,
    int MinutosPuestosACero,
    int VehiculosDentroConservados);
