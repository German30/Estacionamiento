import { HttpErrorResponse } from '@angular/common/http';

/** Cuerpo `application/problem+json` (RFC 9457) tal como lo emite ASP.NET Core. */
export interface DetalleDeProblema {
  readonly type?: string;
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;
  readonly errors?: Record<string, string[]>;
}

/**
 * Un fallo ya traducido al lenguaje del estacionamiento.
 *
 * `reintentable` es la distinción que más importa: un 409 significa que el estacionamiento no
 * está en el estado que hace falta —ya está dentro, no está dentro, ya estaba dada de alta— y
 * nunca se arregla repitiendo la petición. Ofrecer «reintentar» ahí es mentirle al operador.
 */
export interface Problema {
  readonly titulo: string;
  readonly detalle: string;
  readonly estado: number | null;
  readonly reintentable: boolean;
}

const SIN_RED: Problema = {
  titulo: 'No hay contacto con el servidor',
  detalle:
    'La API del estacionamiento no respondió. Comprobá que esté levantada en el puerto 5209 ' +
    '(docker compose up -d desde la carpeta Estacionamiento) y volvé a intentar.',
  estado: null,
  reintentable: true,
};

/** Traduce cualquier fallo de HttpClient a algo que un empleado pueda leer y accionar. */
export function comoProblema(fallo: unknown): Problema {
  if (!(fallo instanceof HttpErrorResponse)) {
    return {
      titulo: 'Algo salió mal',
      detalle: 'Ocurrió un error inesperado en la aplicación. Recargá la página.',
      estado: null,
      reintentable: true,
    };
  }

  // status 0 es «ni siquiera salió»: servidor caído, DNS, o CORS.
  if (fallo.status === 0) {
    return SIN_RED;
  }

  const cuerpo = (fallo.error ?? {}) as DetalleDeProblema;
  const validaciones = erroresDeValidacion(cuerpo);

  const titulo = cuerpo.title?.trim() || tituloPorEstado(fallo.status);
  const detalle =
    validaciones ??
    cuerpo.detail?.trim() ??
    'El servidor rechazó la operación sin explicar el motivo.';

  return {
    titulo,
    detalle,
    estado: fallo.status,
    // Los 4xx son decisiones del servidor sobre una petición concreta: repetirla da lo mismo.
    reintentable: fallo.status >= 500,
  };
}

/** Aplana el diccionario `errors` de la validación de modelo de ASP.NET Core. */
function erroresDeValidacion(cuerpo: DetalleDeProblema): string | null {
  const mensajes = Object.values(cuerpo.errors ?? {}).flat().filter(Boolean);

  return mensajes.length ? mensajes.join(' ') : null;
}

function tituloPorEstado(estado: number): string {
  switch (estado) {
    case 400:
      return 'La petición no es válida';
    case 404:
      return 'No se encontró';
    case 409:
      return 'El estacionamiento no está en ese estado';
    default:
      return estado >= 500 ? 'El servidor falló' : 'La operación no procedió';
  }
}
