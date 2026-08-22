import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { Aviso } from './comun/aviso';
import { Icono } from './comun/icono';
import { Salud } from './nucleo/salud';

@Component({
  selector: 'app-raiz',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Icono, Aviso],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly salud = inject(Salud);

  protected readonly contacto = this.salud.estado;

  protected readonly leyendaDeContacto = computed(() => {
    switch (this.contacto()) {
      case 'en-linea':
        return 'API en línea';
      case 'sin-contacto':
        return 'Sin contacto';
      default:
        return 'Probando contacto';
    }
  });
}
