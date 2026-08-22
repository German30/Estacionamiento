// Espejo exacto de los contratos de la API.
//
// La fuente de verdad es ../Estacionamiento/src/Estacionamiento.Web/Contratos/. El backend los
// mantiene aparte de su dominio justamente para que este archivo no se mueva cuando aquél se
// reorganiza por dentro. Las fechas llegan como DateTime local sin desplazamiento, así que
// viajan como cadena y se convierten en el borde.

/** Lo que la API devuelve en `tipo` / `tipoDeVehiculo`. Ojo: lleva espacio. */
export type TipoDeVehiculo = 'Oficial' | 'Residente' | 'No residente';

/** Lo que el filtro `?tipo=` de `/api/vehiculos` espera: el discriminador, sin espacio. */
export type DiscriminadorDeTipo = 'Oficial' | 'Residente' | 'NoResidente';

export type MomentoDeCobro = 'Ninguno' | 'ALaSalida' | 'AFinDeMes';

export interface EntradaRegistrada {
  readonly placa: string;
  readonly tipoDeVehiculo: TipoDeVehiculo;
  readonly entrada: string;
  /** La placa era desconocida y se dio de alta ahora como no residente. */
  readonly vehiculoRecienCreado: boolean;
}

export interface SalidaRegistrada {
  readonly placa: string;
  readonly tipoDeVehiculo: TipoDeVehiculo;
  readonly entrada: string;
  readonly salida: string;
  readonly minutosFacturables: number;
  readonly momentoDeCobro: MomentoDeCobro;
  readonly importeACobrarAhora: number;
  readonly minutosAcumulados: number | null;
  readonly saldoPendiente: number | null;
}

export interface VehiculoDadoDeAlta {
  readonly placa: string;
  readonly tipoDeVehiculo: TipoDeVehiculo;
  readonly fechaDeAlta: string;
}

export interface VehiculoEnLista {
  readonly placa: string;
  readonly tipo: TipoDeVehiculo;
  readonly momentoDeCobro: MomentoDeCobro;
  readonly estaDentro: boolean;
  readonly dentroDesde: string | null;
  readonly minutosDentro: number;
  readonly minutosAcumulados: number | null;
  readonly saldoPendiente: number | null;
  readonly totalDeEstancias: number;
}

export interface EstanciaEnLista {
  readonly entrada: string;
  readonly salida: string | null;
  readonly minutos: number;
  readonly importeCobrado: number;
  readonly estaAbierta: boolean;
}

export interface DetalleDeVehiculo {
  readonly placa: string;
  readonly tipo: TipoDeVehiculo;
  readonly momentoDeCobro: MomentoDeCobro;
  readonly tarifaPorMinuto: number;
  readonly fechaDeAlta: string;
  readonly estaDentro: boolean;
  readonly minutosAcumulados: number | null;
  readonly saldoPendiente: number | null;
  readonly totalDeMinutos: number;
  readonly totalCobrado: number;
  readonly estancias: readonly EstanciaEnLista[];
}

export interface PanelDeControl {
  readonly vehiculosDentro: number;
  readonly totalDeVehiculos: number;
  readonly oficiales: number;
  readonly residentes: number;
  readonly noResidentes: number;
  readonly minutosAcumuladosDeResidentes: number;
  readonly saldoPendienteDeResidentes: number;
  readonly salidasDeHoy: number;
  readonly cobradoHoy: number;
}

export interface LineaDePagoDeResidente {
  readonly placa: string;
  readonly minutosEstacionado: number;
  readonly cantidadAPagar: number;
}

export interface InformeDePagos {
  readonly rutaDelArchivo: string | null;
  /** El informe ya formateado en columnas de ancho fijo, tal cual se descarga. */
  readonly contenido: string;
  readonly lineas: readonly LineaDePagoDeResidente[];
  readonly totalDeMinutos: number;
  readonly totalAPagar: number;
}

export interface ResumenDeComienzoDeMes {
  readonly vehiculosOficialesAfectados: number;
  readonly estanciasEliminadas: number;
  readonly residentesReiniciados: number;
  readonly minutosPuestosACero: number;
  readonly vehiculosDentroConservados: number;
}

/** Palabra exacta que el backend exige para dejar pasar el cierre de mes. */
export const PALABRA_DE_CONFIRMACION = 'COMENZAR';

/** Tarifas del dominio, sólo para explicarlas en pantalla. Nunca para calcular con ellas. */
export const TARIFAS: ReadonlyMap<TipoDeVehiculo, number> = new Map([
  ['Oficial', 0],
  ['Residente', 0.05],
  ['No residente', 0.5],
]);

export const DISCRIMINADOR: ReadonlyMap<TipoDeVehiculo, DiscriminadorDeTipo> = new Map([
  ['Oficial', 'Oficial'],
  ['Residente', 'Residente'],
  ['No residente', 'NoResidente'],
]);
