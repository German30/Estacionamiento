// Formato es-MX. Presentación pura: nada aquí decide un importe ni redondea un minuto — esas
// dos cosas ya las resolvió el dominio y duplicarlas sería crear una segunda verdad.

const PESOS = new Intl.NumberFormat('es-MX', {
  style: 'currency',
  currency: 'MXN',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

const ENTERO = new Intl.NumberFormat('es-MX', { maximumFractionDigits: 0 });

const HORA = new Intl.DateTimeFormat('es-MX', { hour: '2-digit', minute: '2-digit', hour12: false });

const FECHA_HORA = new Intl.DateTimeFormat('es-MX', {
  day: '2-digit',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
});

const FECHA_LARGA = new Intl.DateTimeFormat('es-MX', {
  day: 'numeric',
  month: 'long',
  year: 'numeric',
});

export function pesos(importe: number): string {
  return PESOS.format(importe);
}

export function entero(valor: number): string {
  return ENTERO.format(valor);
}

/**
 * Las fechas llegan sin desplazamiento («2026-08-22T14:03:11») porque el backend trabaja en
 * hora local del estacionamiento. `new Date` las interpreta como locales, que es lo correcto.
 */
export function comoFecha(iso: string): Date {
  return new Date(iso);
}

export function hora(iso: string): string {
  return HORA.format(comoFecha(iso));
}

export function fechaYHora(iso: string): string {
  return FECHA_HORA.format(comoFecha(iso));
}

export function fechaLarga(iso: string): string {
  return FECHA_LARGA.format(comoFecha(iso));
}

/**
 * Duración en la forma que un operador dice en voz alta: «3 h 07 min».
 * Bajo una hora se queda en minutos para no inventar precisión que no hay.
 */
export function duracion(minutos: number): string {
  if (minutos < 60) {
    return `${entero(minutos)} min`;
  }

  const horas = Math.floor(minutos / 60);
  const resto = minutos % 60;

  if (horas < 24) {
    return `${entero(horas)} h ${String(resto).padStart(2, '0')} min`;
  }

  const dias = Math.floor(horas / 24);

  return `${entero(dias)} d ${String(horas % 24).padStart(2, '0')} h`;
}

/** Minutos transcurridos desde una entrada, redondeados como los redondea el dominio: hacia arriba. */
export function minutosDesde(iso: string, ahora: number): number {
  const transcurrido = (ahora - comoFecha(iso).getTime()) / 60_000;

  return Math.max(0, Math.ceil(transcurrido));
}

/**
 * Previsualiza la normalización que hará el dominio: mayúsculas, sin espacios ni guiones.
 * Es una vista previa, no una validación: quien decide si una placa vale es `Placa.Crear`.
 */
export function placaNormalizada(escrito: string): string {
  return escrito.toUpperCase().replace(/[^A-Z0-9]/g, '');
}
