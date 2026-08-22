import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { App } from './app';
import { routes } from './app.routes';

describe('App', () => {
  let sondas: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes)],
    }).compileComponents();

    sondas = TestBed.inject(HttpTestingController);
  });

  afterEach(() => sondas.verify());

  it('se construye', () => {
    const marco = TestBed.createComponent(App);

    expect(marco.componentInstance).toBeTruthy();
    sondas.match('/salud');
  });

  it('separa la operación de la administración en dos zonas rotuladas', () => {
    const marco = TestBed.createComponent(App);
    marco.detectChanges();
    sondas.match('/salud');

    const zonas = Array.from(
      (marco.nativeElement as HTMLElement).querySelectorAll('.zona__rotulo'),
    ).map((z) => z.textContent?.trim());

    expect(zonas).toEqual(['Operación', 'Administración']);
  });

  it('avisa una sola vez y arriba cuando la API no responde', async () => {
    const marco = TestBed.createComponent(App);
    marco.detectChanges();

    // Una sonda caída es el modo de fallo más común de este front: el backend no está levantado.
    sondas.expectOne('/salud').error(new ProgressEvent('error'), { status: 0 });
    await marco.whenStable();
    marco.detectChanges();

    const raiz = marco.nativeElement as HTMLElement;

    expect(raiz.querySelector('.contacto')?.getAttribute('data-estado')).toBe('sin-contacto');
    expect(raiz.querySelectorAll('.corte-de-servicio ec-aviso').length).toBe(1);
  });
});
