import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import type { TipoDeVehiculo } from '../nucleo/modelos';

/**
 * El cuño de clasificación del padrón.
 *
 * La clase es la única dimensión que cambia las reglas de cobro, así que es la única que se
 * codifica en color: tinta para el oficial, verdigrís para el residente, ocre para el no
 * residente. El ocre está deliberadamente lejos del rojo de sello para que un no residente
 * nunca se lea como una alarma.
 */
@Component({
  selector: 'ec-sello-clase',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="sello" [attr.data-clase]="clase()">{{ tipo() }}</span>`,
  styles: `
    :host {
      display: inline-flex;
      min-width: 0;
    }

    .sello {
      display: inline-flex;
      align-items: center;
      padding: 1px var(--e2);
      border: 1px solid var(--_borde);
      border-radius: var(--canto-sello);
      background: var(--_vela);
      color: var(--_tinta);
      font-size: var(--t-micro);
      font-weight: var(--peso-duro);
      font-variation-settings: "wdth" var(--ancho-estrecho);
      letter-spacing: 0.09em;
      text-transform: uppercase;
      white-space: nowrap;
    }

    .sello[data-clase="oficial"] {
      --_tinta: var(--clase-oficial);
      --_vela: var(--clase-oficial-vela);
      --_borde: var(--clase-oficial-borde);
    }

    .sello[data-clase="residente"] {
      --_tinta: var(--clase-residente);
      --_vela: var(--clase-residente-vela);
      --_borde: var(--clase-residente-borde);
    }

    .sello[data-clase="no-residente"] {
      --_tinta: var(--clase-no-residente);
      --_vela: var(--clase-no-residente-vela);
      --_borde: var(--clase-no-residente-borde);
    }
  `,
})
export class SelloClase {
  readonly tipo = input.required<TipoDeVehiculo>();

  protected readonly clase = computed(() => {
    switch (this.tipo()) {
      case 'Oficial':
        return 'oficial';
      case 'Residente':
        return 'residente';
      default:
        return 'no-residente';
    }
  });
}
