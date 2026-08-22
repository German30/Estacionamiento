using CapaAplicacion = Estacionamiento.Aplicacion;
using CapaDominio = Estacionamiento.Dominio.Vehiculos;

namespace Estacionamiento.Web.Contratos;

/// <summary>
/// Traduce los tipos de dominio y aplicación a los de la API. Está todo aquí para que cambiar
/// la forma del JSON sea editar un archivo, y para que ningún controlador caiga en la tentación
/// de serializar una entidad del dominio tal cual.
/// </summary>
internal static class Proyecciones
{
    public static EntradaRegistrada AContrato(this CapaAplicacion.Contratos.RegistroDeEntrada origen) =>
        new(origen.Placa.Valor,
            origen.TipoDeVehiculo,
            origen.Entrada,
            origen.VehiculoCreadoEnEsteMomento);

    public static SalidaRegistrada AContrato(this CapaDominio.ResultadoSalida origen) =>
        new(origen.Placa.Valor,
            origen.TipoDeVehiculo,
            origen.Entrada,
            origen.Salida,
            origen.MinutosFacturables,
            origen.MomentoDeCobro.ToString(),
            origen.ImporteACobrarAhora,
            origen.MinutosAcumulados,
            origen.SaldoPendiente);

    public static VehiculoDadoDeAlta AContrato(this CapaAplicacion.Contratos.VehiculoDadoDeAlta origen) =>
        new(origen.Placa.Valor, origen.TipoDeVehiculo, origen.FechaDeAlta);

    public static VehiculoEnLista AContrato(this CapaAplicacion.Consultas.VehiculoEnLista origen) =>
        new(origen.Placa.Valor,
            origen.Tipo,
            origen.MomentoDeCobro.ToString(),
            origen.EstaDentro,
            origen.DentroDesde,
            origen.MinutosDentro,
            origen.MinutosAcumulados,
            origen.SaldoPendiente,
            origen.TotalDeEstancias);

    public static DetalleDeVehiculo AContrato(this CapaAplicacion.Consultas.DetalleDeVehiculo origen) =>
        new(origen.Placa.Valor,
            origen.Tipo,
            origen.MomentoDeCobro.ToString(),
            origen.TarifaPorMinuto,
            origen.FechaDeAlta,
            origen.EstaDentro,
            origen.MinutosAcumulados,
            origen.SaldoPendiente,
            origen.TotalDeMinutos,
            origen.TotalCobrado,
            origen.Estancias.Select(AContrato).ToList());

    public static EstanciaEnLista AContrato(this CapaAplicacion.Consultas.EstanciaEnLista origen) =>
        new(origen.Entrada, origen.Salida, origen.Minutos, origen.ImporteCobrado, origen.EstaAbierta);

    public static PanelDeControl AContrato(this CapaAplicacion.Consultas.PanelDeControl origen) =>
        new(origen.VehiculosDentro,
            origen.TotalDeVehiculos,
            origen.Oficiales,
            origen.Residentes,
            origen.NoResidentes,
            origen.MinutosAcumuladosDeResidentes,
            origen.SaldoPendienteDeResidentes,
            origen.SalidasDeHoy,
            origen.CobradoHoy);

    public static InformeDePagos AContrato(this CapaAplicacion.Contratos.InformeDePagos origen) =>
        new(origen.RutaDelArchivo,
            origen.Contenido,
            origen.Lineas.Select(AContrato).ToList(),
            origen.TotalDeMinutos,
            origen.TotalAPagar);

    public static LineaDePagoDeResidente AContrato(this CapaAplicacion.Contratos.LineaDePagoDeResidente origen) =>
        new(origen.Placa.Valor, origen.MinutosEstacionado, origen.CantidadAPagar);

    public static ResumenDeComienzoDeMes AContrato(this CapaAplicacion.Contratos.ResumenDeComienzoDeMes origen) =>
        new(origen.VehiculosOficialesAfectados,
            origen.EstanciasEliminadas,
            origen.ResidentesReiniciados,
            origen.MinutosPuestosACero,
            origen.VehiculosDentroConservados);
}
