import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { duracion } from '../nucleo/formato';

/** Tope de la escala: una semana dentro. Más allá, la barra se queda llena. */
const TOPE_MINUTOS = 7 * 24 * 60;

/** Marcas de la regla, en minutos. */
const MARCAS: readonly { minutos: number; rotulo: string }[] = [
  { minutos: 15, rotulo: '15m' },
  { minutos: 60, rotulo: '1h' },
  { minutos: 240, rotulo: '4h' },
  // Sin marca de 12 h: en escala logarítmica cae pegada a la de 1 d y los dos rótulos se pisan.
  { minutos: 1440, rotulo: '1d' },
  { minutos: 4320, rotulo: '3d' },
];

/**
 * Posición logarítmica en la escala.
 *
 * Logarítmica porque las estancias reales van de dos minutos a varios días: en una escala
 * lineal todo lo que dura menos de una hora se apelmaza contra el cero y deja de ser
 * comparable, que es justamente lo que la escala existe para permitir.
 */
function posicion(minutos: number): number {
  const acotado = Math.min(Math.max(minutos, 0), TOPE_MINUTOS);

  return Math.log1p(acotado) / Math.log1p(TOPE_MINUTOS);
}

/**
 * La escala continua de permanencia, prestada de la inmersión mesofótica que descartamos: un
 * solo eje rige toda la información y cada estancia queda clavada a su punto exacto, en vez de
 * imprimirse como un número suelto que no se puede comparar con el de la fila de al lado.
 */
@Component({
  selector: 'ec-permanencia',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="permanencia">
      <span class="permanencia__pista" aria-hidden="true">
        @for (marca of marcas; track marca.minutos) {
          <span class="permanencia__marca" [style.left.%]="marca.posicion"></span>
        }
        <span class="permanencia__barra" [style.--parte]="parte()"></span>
      </span>
      <span class="permanencia__cifra">{{ texto() }}</span>
    </span>
  `,
  styles: `
    :host {
      display: block;
    }

    .permanencia {
      display: grid;
      grid-template-columns: minmax(52px, 1fr) auto;
      align-items: center;
      gap: var(--e2);
    }

    .permanencia__pista {
      position: relative;
      height: 8px;
      border: 1px solid var(--papel-borde);
      border-radius: 1px;
      background: var(--papel-hondo);
      overflow: hidden;
    }

    .permanencia__marca {
      position: absolute;
      top: 0;
      bottom: 0;
      width: 1px;
      background: var(--papel-borde);
    }

    /* Se escala en lugar de crecer de ancho: animar width obliga al navegador a rehacer el
       diseño en cada cuadro, y hay una barra por fila del registro. */
    .permanencia__barra {
      position: absolute;
      inset-block: 0;
      left: 0;
      width: 100%;
      transform: scaleX(var(--parte, 0));
      transform-origin: left center;
      background: var(--verde);
      transition: transform var(--paso-largo) var(--curva);
    }

    /* Una estancia larga se oscurece dentro del mismo rol de color, nunca cambiando de rol:
       el ocre pertenece a la clase «no residente» y el rojo a lo irreversible. */
    .permanencia[data-largo="si"] .permanencia__barra {
      background: var(--verde-hondo);
    }

    .permanencia__cifra {
      font-family: var(--letra-medida);
      font-size: var(--t-menor);
      color: var(--tinta-media);
      white-space: nowrap;
    }
  `,
  host: { '[attr.data-largo]': 'largo() ? "si" : "no"' },
})
export class Permanencia {
  readonly minutos = input.required<number>();

  protected readonly marcas = MARCAS.map((marca) => ({
    ...marca,
    posicion: posicion(marca.minutos) * 100,
  }));

  protected readonly parte = computed(() => posicion(this.minutos()));
  protected readonly texto = computed(() => duracion(this.minutos()));
  protected readonly largo = computed(() => this.minutos() >= 1440);
}

/** La regla rotulada que encabeza una columna de permanencias, para que las barras se lean. */
@Component({
  selector: 'ec-regla-permanencia',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="regla" aria-hidden="true">
      @for (marca of marcas; track marca.minutos) {
        <span class="regla__marca" [style.left.%]="marca.posicion">{{ marca.rotulo }}</span>
      }
    </span>
  `,
  styles: `
    :host {
      display: block;
    }

    .regla {
      position: relative;
      display: block;
      height: 12px;
      margin-right: 58px;
    }

    /* Por debajo de esta anchura la columna es más estrecha que sus propios rótulos y las
       marcas se pisan. Una escala ilegible es peor que ninguna: las barras siguen siendo
       comparables entre sí, que es lo que la columna tiene que hacer. */
    @media (width < 760px) {
      :host {
        display: none;
      }
    }

    .regla__marca {
      position: absolute;
      top: 0;
      transform: translateX(-50%);
      font-family: var(--letra-medida);
      /* La cabecera de la tabla va en versalitas; una escala de medida, no. */
      text-transform: none;
      letter-spacing: 0;
      font-size: 10px;
      font-weight: var(--peso-medio);
      color: var(--tinta-tenue);
    }
  `,
})
export class ReglaPermanencia {
  protected readonly marcas = MARCAS.map((marca) => ({
    ...marca,
    posicion: posicion(marca.minutos) * 100,
  }));
}
