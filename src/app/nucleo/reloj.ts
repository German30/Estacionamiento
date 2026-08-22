import { Injectable, NgZone, signal, inject } from '@angular/core';

/**
 * Un solo latido para toda la aplicación.
 *
 * La permanencia de un vehículo se mide contra el reloj, no contra una respuesta del servidor:
 * si cada contador abriera su propio intervalo, veinte filas del registro avanzarían en veinte
 * momentos distintos y la columna dejaría de ser comparable. Aquí hay una única señal y todos
 * los contadores derivan de ella, así que la tabla entera avanza de golpe.
 *
 * Cada 15 segundos: el minuto es la unidad facturable, y latir más rápido sólo gasta cuadros.
 */
@Injectable({ providedIn: 'root' })
export class Reloj {
  private readonly zona = inject(NgZone);
  private readonly latido = signal(Date.now());

  /** Milisegundos actuales. Leerla dentro de un `computed` lo vuelve un contador vivo. */
  readonly ahora = this.latido.asReadonly();

  constructor() {
    // Fuera de la zona: el intervalo no debe disparar una detección de cambios global cada
    // 15 s. La escritura de la señal ya notifica exactamente a quien la lee.
    this.zona.runOutsideAngular(() => {
      setInterval(() => this.latido.set(Date.now()), 15_000);
    });
  }

  /** Vuelve a poner la hora al instante, para no esperar al siguiente latido tras una acción. */
  sincronizar(): void {
    this.latido.set(Date.now());
  }
}
