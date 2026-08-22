import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, of, startWith, switchMap, tap } from 'rxjs';

import { Aviso } from '../../comun/aviso';
import { Icono } from '../../comun/icono';
import { Permanencia, ReglaPermanencia } from '../../comun/permanencia';
import { Placa } from '../../comun/placa';
import { SelloClase } from '../../comun/sello-clase';
import { VehiculosApi } from '../../nucleo/api';
import { duracion, entero, fechaYHora, hora, minutosDesde, pesos, placaNormalizada } from '../../nucleo/formato';
import type {
  DetalleDeVehiculo,
  DiscriminadorDeTipo,
  VehiculoEnLista,
} from '../../nucleo/modelos';
import { comoProblema, type Problema } from '../../nucleo/problema';
import { Reloj } from '../../nucleo/reloj';

type ClaseDeAlta = 'oficial' | 'residente';

const FILTROS: readonly { valor: DiscriminadorDeTipo | null; rotulo: string }[] = [
  { valor: null, rotulo: 'Todos' },
  { valor: 'Oficial', rotulo: 'Oficiales' },
  { valor: 'Residente', rotulo: 'Residentes' },
  { valor: 'NoResidente', rotulo: 'No residentes' },
];

@Component({
  selector: 'ec-padron',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, Aviso, Icono, Permanencia, ReglaPermanencia, Placa, SelloClase],
  templateUrl: './padron.html',
  styleUrl: './padron.scss',
})
export class Padron {
  private readonly api = inject(VehiculosApi);
  private readonly reloj = inject(Reloj);

  protected readonly filtros = FILTROS;

  protected readonly texto = signal('');
  protected readonly tipo = signal<DiscriminadorDeTipo | null>(null);
  protected readonly cargando = signal(true);
  protected readonly problema = signal<Problema | null>(null);

  /** Se incrementa para forzar una relectura tras un alta o un reintento. */
  private readonly revision = signal(0);

  private readonly consulta = computed(() => ({
    texto: this.texto().trim(),
    tipo: this.tipo(),
    revision: this.revision(),
  }));

  private readonly resultado = toSignal(
    toObservable(this.consulta).pipe(
      debounceTime(250),
      distinctUntilChanged(
        (a, b) => a.texto === b.texto && a.tipo === b.tipo && a.revision === b.revision,
      ),
      tap(() => {
        this.cargando.set(true);
        this.problema.set(null);
      }),
      switchMap(({ texto, tipo }) =>
        this.api.listar(texto, tipo).pipe(
          tap(() => this.cargando.set(false)),
          catchError((fallo) => {
            this.problema.set(comoProblema(fallo));
            this.cargando.set(false);

            return of([] as VehiculoEnLista[]);
          }),
        ),
      ),
      startWith([] as VehiculoEnLista[]),
    ),
    { initialValue: [] as VehiculoEnLista[] },
  );

  protected readonly filas = computed(() => {
    const ahora = this.reloj.ahora();

    return this.resultado().map((fila) => ({
      ...fila,
      minutos: fila.dentroDesde ? minutosDesde(fila.dentroDesde, ahora) : 0,
    }));
  });

  protected readonly hayFiltro = computed(() => this.texto().trim().length > 0 || this.tipo() !== null);

  // ── Expansión en el sitio ───────────────────────────────────────────────────
  // La ficha se abre dentro de la propia fila y el resto del registro se apaga. Ningún modal se
  // apodera de la lista: es la disciplina que donó el muro de carteles al descartarlo.

  protected readonly abierta = signal<string | null>(null);
  protected readonly detalle = signal<DetalleDeVehiculo | null>(null);
  protected readonly cargandoDetalle = signal(false);
  protected readonly problemaDetalle = signal<Problema | null>(null);

  // ── Alta ────────────────────────────────────────────────────────────────────

  protected readonly formularioAbierto = signal(false);
  protected readonly claseDeAlta = signal<ClaseDeAlta>('residente');
  protected readonly placaDeAlta = signal('');
  protected readonly dandoDeAlta = signal(false);
  protected readonly problemaDeAlta = signal<Problema | null>(null);
  protected readonly ultimaAlta = signal<string | null>(null);

  protected readonly placaNormalizadaDeAlta = computed(() => placaNormalizada(this.placaDeAlta()));
  protected readonly puedeDarDeAlta = computed(() => this.placaNormalizadaDeAlta().length >= 5);

  protected readonly pesos = pesos;
  protected readonly entero = entero;
  protected readonly duracion = duracion;
  protected readonly hora = hora;
  protected readonly fechaYHora = fechaYHora;

  protected alternar(placa: string): void {
    if (this.abierta() === placa) {
      this.abierta.set(null);

      return;
    }

    this.abierta.set(placa);
    this.detalle.set(null);
    this.problemaDetalle.set(null);
    this.cargandoDetalle.set(true);

    this.api.ficha(placa).subscribe({
      next: (detalle) => {
        this.detalle.set(detalle);
        this.cargandoDetalle.set(false);
      },
      error: (fallo) => {
        this.problemaDetalle.set(comoProblema(fallo));
        this.cargandoDetalle.set(false);
      },
    });
  }

  protected alEscribirFiltro(evento: Event): void {
    this.texto.set((evento.target as HTMLInputElement).value);
  }

  protected alEscribirAlta(evento: Event): void {
    this.placaDeAlta.set((evento.target as HTMLInputElement).value);
    this.problemaDeAlta.set(null);
  }

  protected limpiarFiltros(): void {
    this.texto.set('');
    this.tipo.set(null);
  }

  protected recargar(): void {
    this.revision.update((n) => n + 1);
  }

  protected darDeAlta(evento: Event): void {
    evento.preventDefault();

    if (!this.puedeDarDeAlta() || this.dandoDeAlta()) {
      return;
    }

    const placa = this.placaNormalizadaDeAlta();

    this.dandoDeAlta.set(true);
    this.problemaDeAlta.set(null);
    this.ultimaAlta.set(null);

    const peticion =
      this.claseDeAlta() === 'oficial'
        ? this.api.altaDeOficial(placa)
        : this.api.altaDeResidente(placa);

    peticion.subscribe({
      next: (alta) => {
        this.ultimaAlta.set(`${alta.placa} quedó registrada como ${alta.tipoDeVehiculo.toLowerCase()}.`);
        this.placaDeAlta.set('');
        this.dandoDeAlta.set(false);
        this.recargar();
      },
      error: (fallo) => {
        this.problemaDeAlta.set(comoProblema(fallo));
        this.dandoDeAlta.set(false);
      },
    });
  }
}
