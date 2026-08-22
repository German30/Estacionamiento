import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { Aviso } from '../../comun/aviso';
import { Icono } from '../../comun/icono';
import { Permanencia } from '../../comun/permanencia';
import { Placa } from '../../comun/placa';
import { SelloClase } from '../../comun/sello-clase';
import { VehiculosApi } from '../../nucleo/api';
import { duracion, entero, fechaLarga, fechaYHora, minutosDesde, pesos } from '../../nucleo/formato';
import type { DetalleDeVehiculo } from '../../nucleo/modelos';
import { comoProblema, type Problema } from '../../nucleo/problema';
import { Reloj } from '../../nucleo/reloj';

@Component({
  selector: 'ec-ficha',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, Aviso, Icono, Permanencia, Placa, SelloClase],
  templateUrl: './ficha.html',
  styleUrl: './ficha.scss',
})
export class Ficha {
  private readonly api = inject(VehiculosApi);
  private readonly reloj = inject(Reloj);

  /** Llega del parámetro de ruta gracias a `withComponentInputBinding`. */
  readonly placa = input.required<string>();

  protected readonly detalle = signal<DetalleDeVehiculo | null>(null);
  protected readonly cargando = signal(true);
  protected readonly problema = signal<Problema | null>(null);

  /** La estancia abierta, si la hay: es la única fila cuyo contador sigue corriendo. */
  protected readonly estanciaAbierta = computed(() => {
    const ahora = this.reloj.ahora();
    const abierta = this.detalle()?.estancias.find((estancia) => estancia.estaAbierta);

    return abierta ? { ...abierta, minutos: minutosDesde(abierta.entrada, ahora) } : null;
  });

  protected readonly pesos = pesos;
  protected readonly entero = entero;
  protected readonly duracion = duracion;
  protected readonly fechaYHora = fechaYHora;
  protected readonly fechaLarga = fechaLarga;

  constructor() {
    // `placa` es una señal de entrada, así que leerla dentro del efecto vuelve a disparar la
    // carga al navegar de una ficha a otra: el router reutiliza el componente y sólo cambia el
    // parámetro, de modo que una llamada en el constructor se quedaría con la primera placa.
    effect(() => this.cargar(this.placa()));
  }

  protected recargar(): void {
    this.cargar(this.placa());
  }

  private cargar(placa: string): void {
    this.cargando.set(true);

    this.api.ficha(placa).subscribe({
      next: (detalle) => {
        this.detalle.set(detalle);
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
