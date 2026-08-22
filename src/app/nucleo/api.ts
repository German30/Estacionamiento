import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { Observable } from 'rxjs';

import type {
  DetalleDeVehiculo,
  DiscriminadorDeTipo,
  EntradaRegistrada,
  InformeDePagos,
  PanelDeControl,
  ResumenDeComienzoDeMes,
  SalidaRegistrada,
  VehiculoDadoDeAlta,
  VehiculoEnLista,
} from './modelos';

// Ruta relativa a propósito. La API no tiene CORS configurado, así que en desarrollo el
// proxy de Angular (proxy.conf.json) reenvía /api al 5209; en producción cualquier proxy
// inverso hace lo mismo sin recompilar. Una URL absoluta aquí rompería las dos cosas.
const RAIZ = '/api';

/** Entradas, salidas y quién está dentro ahora mismo. */
@Injectable({ providedIn: 'root' })
export class AccesosApi {
  private readonly http = inject(HttpClient);

  registrarEntrada(placa: string): Observable<EntradaRegistrada> {
    return this.http.post<EntradaRegistrada>(`${RAIZ}/accesos/entradas`, { placa });
  }

  registrarSalida(placa: string): Observable<SalidaRegistrada> {
    return this.http.post<SalidaRegistrada>(`${RAIZ}/accesos/salidas`, { placa });
  }

  /** Ya viene ordenado por el backend: el que más lleva dentro, primero. */
  dentro(): Observable<VehiculoEnLista[]> {
    return this.http.get<VehiculoEnLista[]>(`${RAIZ}/accesos/dentro`);
  }
}

/** El padrón: consulta, ficha y las dos altas. */
@Injectable({ providedIn: 'root' })
export class VehiculosApi {
  private readonly http = inject(HttpClient);

  listar(filtro?: string, tipo?: DiscriminadorDeTipo | null): Observable<VehiculoEnLista[]> {
    let parametros = new HttpParams();

    if (filtro?.trim()) {
      parametros = parametros.set('filtro', filtro.trim());
    }

    if (tipo) {
      parametros = parametros.set('tipo', tipo);
    }

    return this.http.get<VehiculoEnLista[]>(`${RAIZ}/vehiculos`, { params: parametros });
  }

  ficha(placa: string): Observable<DetalleDeVehiculo> {
    return this.http.get<DetalleDeVehiculo>(`${RAIZ}/vehiculos/${encodeURIComponent(placa)}`);
  }

  altaDeOficial(placa: string): Observable<VehiculoDadoDeAlta> {
    return this.http.post<VehiculoDadoDeAlta>(`${RAIZ}/vehiculos/oficiales`, { placa });
  }

  altaDeResidente(placa: string): Observable<VehiculoDadoDeAlta> {
    return this.http.post<VehiculoDadoDeAlta>(`${RAIZ}/vehiculos/residentes`, { placa });
  }
}

/** Estado del estacionamiento de un vistazo. */
@Injectable({ providedIn: 'root' })
export class PanelApi {
  private readonly http = inject(HttpClient);

  obtener(): Observable<PanelDeControl> {
    return this.http.get<PanelDeControl>(`${RAIZ}/panel`);
  }
}

/** Informe de pagos de residentes y cierre de mes. */
@Injectable({ providedIn: 'root' })
export class CierreApi {
  private readonly http = inject(HttpClient);

  /** Calcula el informe sin escribir nada. */
  informe(): Observable<InformeDePagos> {
    return this.http.get<InformeDePagos>(`${RAIZ}/cierre/informe`);
  }

  /** El mismo informe como archivo .txt, con el formato de ancho fijo del enunciado. */
  descargar(): Observable<Blob> {
    return this.http.get(`${RAIZ}/cierre/informe/descargar`, { responseType: 'blob' });
  }

  /** Deja el informe escrito en el disco del servidor. */
  guardarEnDisco(ruta: string): Observable<InformeDePagos> {
    return this.http.post<InformeDePagos>(`${RAIZ}/cierre/informe`, { ruta });
  }

  /** Irreversible. El backend exige la palabra exacta. */
  comenzarMes(confirmacion: string): Observable<ResumenDeComienzoDeMes> {
    return this.http.post<ResumenDeComienzoDeMes>(`${RAIZ}/cierre/mes`, { confirmacion });
  }
}
