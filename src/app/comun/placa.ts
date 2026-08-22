import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type TamanoDePlaca = 'menuda' | 'fila' | 'ficha' | 'monumento';

/**
 * La placa, tratada como el objeto que es y no como una celda de texto.
 *
 * Es la llave primaria de todo el sistema: la caja troquelada, el remache y el ancho expandido
 * del eje variable existen para que el operador la reconozca de un golpe de vista y no la
 * confunda con el resto de la fila.
 */
@Component({
  selector: 'ec-placa',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="placa" [attr.data-tamano]="tamano()">
      @if (tamano() !== 'menuda') {
        <span class="placa__remache" aria-hidden="true"></span>
      }
      <span class="placa__valor">{{ valor() }}</span>
      @if (tamano() !== 'menuda') {
        <span class="placa__remache" aria-hidden="true"></span>
      }
    </span>
  `,
  styles: `
    :host {
      display: inline-flex;
      min-width: 0;
    }

    .placa {
      display: inline-flex;
      align-items: center;
      gap: var(--e2);
      padding: var(--e1) var(--e2);
      border: 1.5px solid var(--tinta);
      border-radius: var(--canto-placa);
      background: var(--papel-alto);
      box-shadow: inset 0 0 0 1.5px var(--papel-alto), var(--relieve-1);
      font-variation-settings: "wdth" var(--ancho-ancho);
      font-weight: var(--peso-duro);
      letter-spacing: 0.05em;
      color: var(--tinta);
      white-space: nowrap;
    }

    .placa__valor {
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .placa__remache {
      flex: none;
      width: 3px;
      height: 3px;
      border-radius: 999px;
      background: var(--tinta);
      opacity: 0.35;
    }

    .placa[data-tamano="menuda"] {
      font-size: var(--t-menor);
      padding: 1px var(--e1);
      gap: var(--e1);
      border-width: 1px;
    }

    .placa[data-tamano="fila"] {
      font-size: var(--t-base);
    }

    .placa[data-tamano="ficha"] {
      font-size: var(--t-placa);
      padding: var(--e2) var(--e4);
      gap: var(--e3);
      border-width: 2.5px;
      box-shadow: inset 0 0 0 2.5px var(--papel-alto), var(--relieve-2);
    }

    .placa[data-tamano="ficha"] .placa__remache {
      width: 6px;
      height: 6px;
    }

    .placa[data-tamano="monumento"] {
      font-size: clamp(2rem, 7vw, var(--t-placa));
      padding: var(--e3) var(--e5);
      gap: var(--e4);
      border-width: 3px;
      box-shadow: inset 0 0 0 3px var(--papel-alto), var(--relieve-2);
    }

    .placa[data-tamano="monumento"] .placa__remache {
      width: 7px;
      height: 7px;
    }
  `,
})
export class Placa {
  readonly valor = input.required<string>();
  readonly tamano = input<TamanoDePlaca>('fila');
}
