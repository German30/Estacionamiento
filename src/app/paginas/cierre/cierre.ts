import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';

import { Aviso } from '../../comun/aviso';
import { Icono } from '../../comun/icono';
import { Placa } from '../../comun/placa';
import { CierreApi } from '../../nucleo/api';
import { duracion, entero, pesos } from '../../nucleo/formato';
import {
  PALABRA_DE_CONFIRMACION,
  type InformeDePagos,
  type ResumenDeComienzoDeMes,
} from '../../nucleo/modelos';
import { comoProblema, type Problema } from '../../nucleo/problema';

@Component({
  selector: 'ec-cierre',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Aviso, Icono, Placa],
  templateUrl: './cierre.html',
  styleUrl: './cierre.scss',
})
export class Cierre {
  private readonly api = inject(CierreApi);

  protected readonly palabra = PALABRA_DE_CONFIRMACION;

  protected readonly informe = signal<InformeDePagos | null>(null);
  protected readonly cargando = signal(true);
  protected readonly problema = signal<Problema | null>(null);

  protected readonly descargando = signal(false);
  protected readonly problemaDeDescarga = signal<Problema | null>(null);

  protected readonly ruta = signal('/informes/pagos-del-mes.txt');
  protected readonly guardando = signal(false);
  protected readonly guardadoEn = signal<string | null>(null);
  protected readonly problemaDeGuardado = signal<Problema | null>(null);

  /**
   * Confirmación de que el informe está a salvo.
   *
   * Comenzar mes pone a cero lo que se cobra este mes, y el enunciado del backend lo dice sin
   * rodeos: quien no descargue antes el informe, lo pierde. Descargar o guardar marca esto solo;
   * queda la casilla para quien ya lo tenía, porque impedirle cerrar el mes a alguien que hizo
   * bien su trabajo sería tratarlo como sospechoso.
   */
  protected readonly informeASalvo = signal(false);

  protected readonly confirmacion = signal('');
  protected readonly cerrando = signal(false);
  protected readonly resumen = signal<ResumenDeComienzoDeMes | null>(null);
  protected readonly problemaDeCierre = signal<Problema | null>(null);

  protected readonly palabraCorrecta = computed(
    () => this.confirmacion().trim().toUpperCase() === PALABRA_DE_CONFIRMACION,
  );

  protected readonly puedeCerrar = computed(
    () => this.informeASalvo() && this.palabraCorrecta() && !this.cerrando() && !this.resumen(),
  );

  /** Residentes que sí deben algo: lo que realmente se pierde si nadie descargó el informe. */
  protected readonly conSaldo = computed(
    () => this.informe()?.lineas.filter((linea) => linea.cantidadAPagar > 0).length ?? 0,
  );

  protected readonly pesos = pesos;
  protected readonly entero = entero;
  protected readonly duracion = duracion;

  constructor() {
    this.cargar();
  }

  protected cargar(): void {
    this.cargando.set(true);

    this.api.informe().subscribe({
      next: (informe) => {
        this.informe.set(informe);
        this.problema.set(null);
        this.cargando.set(false);
      },
      error: (fallo) => {
        this.problema.set(comoProblema(fallo));
        this.cargando.set(false);
      },
    });
  }

  protected descargar(): void {
    this.descargando.set(true);
    this.problemaDeDescarga.set(null);

    this.api.descargar().subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const enlace = document.createElement('a');

        enlace.href = url;
        enlace.download = `pagos-${new Date().toISOString().slice(0, 7)}.txt`;
        enlace.click();

        URL.revokeObjectURL(url);

        this.descargando.set(false);
        this.informeASalvo.set(true);
      },
      error: (fallo) => {
        this.problemaDeDescarga.set(comoProblema(fallo));
        this.descargando.set(false);
      },
    });
  }

  protected alEscribirRuta(evento: Event): void {
    this.ruta.set((evento.target as HTMLInputElement).value);
    this.problemaDeGuardado.set(null);
  }

  protected guardarEnDisco(evento: Event): void {
    evento.preventDefault();

    const ruta = this.ruta().trim();

    if (!ruta || this.guardando()) {
      return;
    }

    this.guardando.set(true);
    this.problemaDeGuardado.set(null);
    this.guardadoEn.set(null);

    this.api.guardarEnDisco(ruta).subscribe({
      next: (informe) => {
        this.informe.set(informe);
        this.guardadoEn.set(informe.rutaDelArchivo ?? ruta);
        this.guardando.set(false);
        this.informeASalvo.set(true);
      },
      error: (fallo) => {
        this.problemaDeGuardado.set(comoProblema(fallo));
        this.guardando.set(false);
      },
    });
  }

  protected alEscribirConfirmacion(evento: Event): void {
    this.confirmacion.set((evento.target as HTMLInputElement).value);
    this.problemaDeCierre.set(null);
  }

  protected comenzarMes(evento: Event): void {
    evento.preventDefault();

    if (!this.puedeCerrar()) {
      return;
    }

    this.cerrando.set(true);
    this.problemaDeCierre.set(null);

    this.api.comenzarMes(this.confirmacion().trim().toUpperCase()).subscribe({
      next: (resumen) => {
        this.resumen.set(resumen);
        this.confirmacion.set('');
        this.cerrando.set(false);
        this.cargar();
      },
      error: (fallo) => {
        this.problemaDeCierre.set(comoProblema(fallo));
        this.cerrando.set(false);
      },
    });
  }
}
