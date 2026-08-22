import { HttpClient } from '@angular/common/http';
import { Injectable, NgZone, inject, signal } from '@angular/core';
import { catchError, of, tap } from 'rxjs';

export type EstadoDeContacto = 'probando' | 'en-linea' | 'sin-contacto';

/**
 * Sonda del backend.
 *
 * El modo de fallo más frecuente de este front no es un error de negocio: es que la API no
 * está levantada. Cuando eso pasa, cada pantalla enseñaría su propio «no hay contacto» y el
 * empleado tendría que deducir que el problema es común. Con la sonda arriba, el aviso se da
 * una vez y en el sitio donde se mira el estado del sistema.
 */
@Injectable({ providedIn: 'root' })
export class Salud {
  private readonly http = inject(HttpClient);
  private readonly zona = inject(NgZone);
  private readonly interno = signal<EstadoDeContacto>('probando');

  readonly estado = this.interno.asReadonly();

  constructor() {
    this.sondear();

    this.zona.runOutsideAngular(() => {
      setInterval(() => this.zona.run(() => this.sondear()), 30_000);
    });
  }

  /** No toca la base de datos: responde si el proceso puede atender peticiones. */
  sondear(): void {
    this.http
      .get('/salud', { responseType: 'text' })
      .pipe(
        tap(() => this.interno.set('en-linea')),
        catchError(() => {
          this.interno.set('sin-contacto');

          return of(null);
        }),
      )
      .subscribe();
  }
}
