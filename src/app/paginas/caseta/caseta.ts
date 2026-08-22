import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, viewChild, ElementRef } from '@angular/core';
import { RouterLink } from '@angular/router';

import { Aviso } from '../../comun/aviso';
import { Icono } from '../../comun/icono';
import { Permanencia, ReglaPermanencia } from '../../comun/permanencia';
import { Placa } from '../../comun/placa';
import { SelloClase } from '../../comun/sello-clase';
import { AccesosApi } from '../../nucleo/api';
import { duracion, entero, hora, minutosDesde, pesos, placaNormalizada } from '../../nucleo/formato';
import type { EntradaRegistrada, SalidaRegistrada, VehiculoEnLista } from '../../nucleo/modelos';
import { comoProblema, type Problema } from '../../nucleo/problema';
import { Reloj } from '../../nucleo/reloj';

type Accion = 'entrada' | 'salida';

/** Longitud mínima que acepta el dominio. Sólo habilita los botones; validar es cosa de la API. */
const MINIMO_DE_PLACA = 5;

@Component({
  selector: 'ec-caseta',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, Aviso, Icono, Permanencia, ReglaPermanencia, Placa, SelloClase],
  templateUrl: './caseta.html',
  styleUrl: './caseta.scss',
})
export class Caseta {
  private readonly api = inject(AccesosApi);
  private readonly reloj = inject(Reloj);

  private readonly campo = viewChild<ElementRef<HTMLInputElement>>('campoDePlaca');

  protected readonly escrito = signal('');
  protected readonly enCurso = signal<Accion | null>(null);
  protected readonly problema = signal<Problema | null>(null);

  protected readonly entrada = signal<EntradaRegistrada | null>(null);
  protected readonly salida = signal<SalidaRegistrada | null>(null);

  protected readonly dentro = signal<readonly VehiculoEnLista[]>([]);
  protected readonly cargandoDentro = signal(true);
  protected readonly problemaDentro = signal<Problema | null>(null);

  protected readonly placa = computed(() => placaNormalizada(this.escrito()));
  protected readonly listaParaEnviar = computed(() => this.placa().length >= MINIMO_DE_PLACA);

  /** La normalización sólo se anuncia cuando cambia algo, para no repetir lo obvio. */
  protected readonly muestraNormalizacion = computed(
    () => this.escrito().length > 0 && this.escrito() !== this.placa(),
  );

  /**
   * Qué acción propone el estado del estacionamiento.
   *
   * El registro de la derecha ya sabe quién está dentro, así que la aplicación no tiene por qué
   * preguntarle al operador algo que puede deducir: si la placa está dentro, lo único que cabe
   * es una salida. La tecla Intro dispara esta propuesta y el botón correspondiente lo dice; el
   * otro sigue a un clic de distancia, porque la deducción puede equivocarse y la decisión es
   * de quien está en la caseta.
   */
  protected readonly propuesta = computed<Accion | null>(() => {
    if (!this.listaParaEnviar() || this.cargandoDentro()) {
      return null;
    }

    return this.dentro().some((fila) => fila.placa === this.placa()) ? 'salida' : 'entrada';
  });

  /** Filas del registro con su permanencia recalculada contra el latido común. */
  protected readonly filas = computed(() => {
    const ahora = this.reloj.ahora();

    return this.dentro().map((fila) => ({
      ...fila,
      minutos: fila.dentroDesde ? minutosDesde(fila.dentroDesde, ahora) : fila.minutosDentro,
      resaltada: fila.placa === this.placa(),
    }));
  });

  protected readonly hora = hora;
  protected readonly pesos = pesos;
  protected readonly duracion = duracion;
  protected readonly entero = entero;

  constructor() {
    this.cargarDentro();

    // Devuelve el foco al campo en cuanto termina una operación: el siguiente coche ya está ahí.
    effect(() => {
      if (this.enCurso() === null && (this.entrada() || this.salida())) {
        this.campo()?.nativeElement.focus();
      }
    });
  }

  protected cargarDentro(): void {
    this.cargandoDentro.set(true);

    this.api.dentro().subscribe({
      next: (filas) => {
        this.dentro.set(filas);
        this.problemaDentro.set(null);
        this.cargandoDentro.set(false);
        this.reloj.sincronizar();
      },
      error: (fallo) => {
        this.problemaDentro.set(comoProblema(fallo));
        this.cargandoDentro.set(false);
      },
    });
  }

  protected alTeclear(evento: Event): void {
    this.escrito.set((evento.target as HTMLInputElement).value);
  }

  /** Intro ejecuta la propuesta. Si no hay ninguna, no hace nada: no se adivina un cobro. */
  protected alPulsarIntro(evento: Event): void {
    evento.preventDefault();

    const propuesta = this.propuesta();

    if (propuesta) {
      this.ejecutar(propuesta);
    }
  }

  protected ejecutar(accion: Accion): void {
    if (!this.listaParaEnviar() || this.enCurso()) {
      return;
    }

    const placa = this.placa();

    this.enCurso.set(accion);
    this.problema.set(null);
    this.entrada.set(null);
    this.salida.set(null);

    // Las dos ramas se suscriben por separado en lugar de unir los observables: una unión de
    // Observable<Entrada> | Observable<Salida> no es invocable, y el `as` que haría falta para
    // forzarla borraría justo la comprobación que hace útil tipar el contrato.
    const alFallar = (fallo: unknown): void => {
      this.problema.set(comoProblema(fallo));
      this.enCurso.set(null);
      this.campo()?.nativeElement.select();
    };

    const alLograr = (): void => {
      this.escrito.set('');
      this.enCurso.set(null);
      this.cargarDentro();
    };

    if (accion === 'entrada') {
      this.api.registrarEntrada(placa).subscribe({
        next: (resultado) => {
          this.entrada.set(resultado);
          alLograr();
        },
        error: alFallar,
      });
    } else {
      this.api.registrarSalida(placa).subscribe({
        next: (resultado) => {
          this.salida.set(resultado);
          alLograr();
        },
        error: alFallar,
      });
    }
  }

  protected limpiar(): void {
    this.escrito.set('');
    this.entrada.set(null);
    this.salida.set(null);
    this.problema.set(null);
    this.campo()?.nativeElement.focus();
  }

  /** Rellena el campo desde el registro, para no volver a teclear una placa que ya está en pantalla. */
  protected tomarDelRegistro(placa: string): void {
    this.escrito.set(placa);
    this.campo()?.nativeElement.focus();
  }
}
