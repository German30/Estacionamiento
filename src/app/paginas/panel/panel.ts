import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { Aviso } from '../../comun/aviso';
import { Icono } from '../../comun/icono';
import { PanelApi } from '../../nucleo/api';
import { duracion, entero, pesos } from '../../nucleo/formato';
import type { PanelDeControl } from '../../nucleo/modelos';
import { comoProblema, type Problema } from '../../nucleo/problema';

@Component({
  selector: 'ec-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, Aviso, Icono],
  templateUrl: './panel.html',
  styleUrl: './panel.scss',
})
export class Panel {
  private readonly api = inject(PanelApi);

  protected readonly datos = signal<PanelDeControl | null>(null);
  protected readonly cargando = signal(true);
  protected readonly problema = signal<Problema | null>(null);

  protected readonly pesos = pesos;
  protected readonly entero = entero;
  protected readonly duracion = duracion;

  /** Ocupación como fracción (0 a 1) del padrón: alimenta directamente un scaleX. */
  protected readonly ocupacion = computed(() => {
    const datos = this.datos();

    if (!datos || datos.totalDeVehiculos === 0) {
      return 0;
    }

    return datos.vehiculosDentro / datos.totalDeVehiculos;
  });

  /** Las tres clases con su peso relativo, para leerlas como composición y no como tres cifras. */
  protected readonly clases = computed(() => {
    const datos = this.datos();

    if (!datos) {
      return [];
    }

    const total = Math.max(datos.totalDeVehiculos, 1);

    return [
      {
        nombre: 'Oficiales',
        clave: 'oficial',
        cantidad: datos.oficiales,
        parte: datos.oficiales / total,
        tarifa: 'No paga nunca',
      },
      {
        nombre: 'Residentes',
        clave: 'residente',
        cantidad: datos.residentes,
        parte: datos.residentes / total,
        tarifa: 'MXN$0.05 / min, a fin de mes',
      },
      {
        nombre: 'No residentes',
        clave: 'no-residente',
        cantidad: datos.noResidentes,
        parte: datos.noResidentes / total,
        tarifa: 'MXN$0.5 / min, al salir',
      },
    ];
  });

  constructor() {
    this.cargar();
  }

  protected cargar(): void {
    this.cargando.set(true);

    this.api.obtener().subscribe({
      next: (datos) => {
        this.datos.set(datos);
        this.problema.set(null);
        this.cargando.set(false);
      },
      error: (fallo) => {
        this.problema.set(comoProblema(fallo));
        this.cargando.set(false);
      },
    });
  }
}
