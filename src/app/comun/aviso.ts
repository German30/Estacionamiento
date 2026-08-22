import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { Icono } from './icono';
import type { Problema } from '../nucleo/problema';

export type TonoDeAviso = 'sello' | 'atencion' | 'listo';

/**
 * Un fallo o una advertencia, dicho en el lenguaje del estacionamiento.
 *
 * El botón de reintentar sólo aparece cuando reintentar puede funcionar. Un 409 —«ya está
 * dentro», «no está dentro», «ya estaba dada de alta»— no se arregla repitiendo la petición,
 * y ofrecerlo ahí sería mandar al operador a golpear un botón que nunca va a servir.
 */
@Component({
  selector: 'ec-aviso',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Icono],
  template: `
    <div class="aviso" [attr.data-tono]="tono()" role="alert">
      <ec-icono class="aviso__marca" [nombre]="icono()" />
      <div class="aviso__texto">
        <p class="aviso__titulo">{{ titulo() }}</p>
        <p class="aviso__detalle">{{ detalle() }}</p>
      </div>
      @if (conReintento() && problema()?.reintentable) {
        <button type="button" class="boton boton--menudo" (click)="reintentar.emit()">
          Reintentar
        </button>
      }
    </div>
  `,
  styles: `
    :host {
      display: block;
    }

    .aviso {
      display: grid;
      grid-template-columns: auto 1fr auto;
      align-items: start;
      gap: var(--e3);
      padding: var(--e3) var(--e4);
      border: 1px solid var(--_borde);
      border-radius: var(--canto-caja);
      background: var(--_vela);
      color: var(--_tinta);
    }

    .aviso[data-tono="sello"] {
      --_tinta: var(--sello-hondo);
      --_vela: var(--sello-vela);
      --_borde: var(--sello-borde);
    }

    .aviso[data-tono="atencion"] {
      --_tinta: #5c4610;
      --_vela: var(--ocre-vela);
      --_borde: var(--ocre-borde);
    }

    .aviso[data-tono="listo"] {
      --_tinta: var(--verde-hondo);
      --_vela: var(--verde-vela);
      --_borde: var(--verde-borde);
    }

    .aviso__marca {
      --icono-tamano: 1.25rem;

      margin-top: 1px;
    }

    .aviso__titulo {
      font-weight: var(--peso-fuerte);
    }

    .aviso__detalle {
      max-width: var(--medida-prosa);
      margin-top: var(--e1);
      font-size: var(--t-menor);
      line-height: 1.55;
      /* Tinte del propio matiz, nunca gris: sobre una superficie de color el gris se ensucia. */
      color: color-mix(in oklab, var(--_tinta) 82%, var(--_vela));
    }

    .aviso .boton {
      align-self: center;
    }
  `,
})
export class Aviso {
  /** Un fallo ya traducido. Alternativa a pasar `titulo` y `detalle` sueltos. */
  readonly problema = input<Problema | null>(null);
  readonly tono = input<TonoDeAviso>('sello');
  readonly encabezado = input<string | null>(null);
  /** Muestra el botón de reintentar cuando el problema lo admite. */
  readonly conReintento = input(false);
  readonly cuerpo = input<string | null>(null);

  readonly reintentar = output<void>();

  protected readonly titulo = computed(
    () => this.encabezado() ?? this.problema()?.titulo ?? 'Algo salió mal',
  );

  protected readonly detalle = computed(() => this.cuerpo() ?? this.problema()?.detalle ?? '');

  protected readonly icono = computed(() => {
    switch (this.tono()) {
      case 'listo':
        return 'listo' as const;
      default:
        return 'alerta' as const;
    }
  });
}
