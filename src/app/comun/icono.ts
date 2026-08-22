import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type NombreDeIcono =
  | 'entrada'
  | 'salida'
  | 'padron'
  | 'panel'
  | 'cierre'
  | 'buscar'
  | 'descargar'
  | 'guardar'
  | 'alerta'
  | 'listo'
  | 'reloj'
  | 'mas'
  | 'volver'
  | 'sello';

/**
 * Iconos dibujados a mano, un solo trazo de 1.75 sobre una retícula de 24. No hay emoji ni
 * glifos Unicode haciendo de icono en ninguna parte de esta aplicación: un icono es un dibujo.
 */
@Component({
  selector: 'ec-icono',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'ec-icono', 'aria-hidden': 'true' },
  template: `
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="1.75"
      stroke-linecap="round"
      stroke-linejoin="round"
      focusable="false"
    >
      @switch (nombre()) {
        @case ('entrada') {
          <path d="M3 20V6a2 2 0 0 1 2-2h5" />
          <path d="M21 12H10" />
          <path d="m15 7 5 5-5 5" />
          <path d="M3 20h4" />
        }
        @case ('salida') {
          <path d="M21 20V6a2 2 0 0 0-2-2h-5" />
          <path d="M3 12h11" />
          <path d="m9 7-5 5 5 5" />
          <path d="M21 20h-4" />
        }
        @case ('padron') {
          <rect x="3" y="4" width="18" height="16" rx="1.5" />
          <path d="M3 9h18" />
          <path d="M8 13h9" />
          <path d="M8 16.5h6" />
          <path d="M8 4v5" />
        }
        @case ('panel') {
          <path d="M4 19a8 8 0 1 1 16 0" />
          <path d="M12 19V9" />
          <path d="m12 9 4.5-3" />
          <path d="M4 19h16" />
        }
        @case ('cierre') {
          <path d="M6 3h12l-1.5 6H7.5z" />
          <path d="M12 9v6" />
          <rect x="5" y="15" width="14" height="6" rx="1.5" />
        }
        @case ('buscar') {
          <circle cx="10.5" cy="10.5" r="6.5" />
          <path d="m20 20-4.6-4.6" />
        }
        @case ('descargar') {
          <path d="M12 3v12" />
          <path d="m7.5 10.5 4.5 4.5 4.5-4.5" />
          <path d="M4 19h16" />
        }
        @case ('guardar') {
          <path d="M5 4h11l3 3v13H5z" />
          <path d="M9 4v5h6V4" />
          <path d="M8 20v-6h8v6" />
        }
        @case ('alerta') {
          <path d="M12 4.5 21 19.5H3z" />
          <path d="M12 10v4" />
          <path d="M12 17h.01" />
        }
        @case ('listo') {
          <circle cx="12" cy="12" r="8.5" />
          <path d="m8.5 12.2 2.4 2.4 4.6-5" />
        }
        @case ('reloj') {
          <circle cx="12" cy="12" r="8.5" />
          <path d="M12 7v5.2l3.4 2" />
        }
        @case ('mas') {
          <path d="M12 5v14" />
          <path d="M5 12h14" />
        }
        @case ('volver') {
          <path d="M20 12H4" />
          <path d="m10 6-6 6 6 6" />
        }
        @case ('sello') {
          <circle cx="12" cy="12" r="8.5" />
          <circle cx="12" cy="12" r="5" stroke-dasharray="2.4 2.4" />
        }
      }
    </svg>
  `,
  styles: `
    .ec-icono {
      display: inline-flex;
      flex: none;
    }

    svg {
      width: var(--icono-tamano, 1.15em);
      height: var(--icono-tamano, 1.15em);
    }
  `,
})
export class Icono {
  readonly nombre = input.required<NombreDeIcono>();
}
