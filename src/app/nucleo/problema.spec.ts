import { HttpErrorResponse } from '@angular/common/http';

import { comoProblema } from './problema';

function fallo(status: number, cuerpo: unknown): HttpErrorResponse {
  return new HttpErrorResponse({ status, error: cuerpo });
}

describe('comoProblema', () => {
  it('nunca ofrece reintentar un conflicto de estado', () => {
    // Un 409 significa «ya está dentro», «no está dentro» o «ya estaba dada de alta». Repetir la
    // petición da exactamente lo mismo; el botón de reintentar sería una promesa falsa.
    const p = comoProblema(
      fallo(409, { title: 'El vehículo ya está dentro', detail: 'ABC1234 tiene una entrada abierta.' }),
    );

    expect(p.reintentable).toBeFalse();
    expect(p.titulo).toBe('El vehículo ya está dentro');
    expect(p.detalle).toBe('ABC1234 tiene una entrada abierta.');
  });

  it('tampoco reintenta un 400 ni un 404', () => {
    expect(comoProblema(fallo(400, {})).reintentable).toBeFalse();
    expect(comoProblema(fallo(404, {})).reintentable).toBeFalse();
  });

  it('sí reintenta cuando el fallo es del servidor', () => {
    expect(comoProblema(fallo(500, {})).reintentable).toBeTrue();
    expect(comoProblema(fallo(503, {})).reintentable).toBeTrue();
  });

  it('explica el caso más frecuente: la API no está levantada', () => {
    const p = comoProblema(fallo(0, null));

    expect(p.estado).toBeNull();
    expect(p.reintentable).toBeTrue();
    expect(p.detalle).toContain('5209');
  });

  it('aplana los errores de validación de modelo en un solo mensaje', () => {
    const p = comoProblema(
      fallo(400, {
        title: 'One or more validation errors occurred.',
        errors: {
          Placa: ['El número de placa es obligatorio.'],
          Otro: ['Segundo problema.'],
        },
      }),
    );

    expect(p.detalle).toBe('El número de placa es obligatorio. Segundo problema.');
  });

  it('traduce un estado sin título al lenguaje del estacionamiento', () => {
    expect(comoProblema(fallo(409, {})).titulo).toBe('El estacionamiento no está en ese estado');
    expect(comoProblema(fallo(404, {})).titulo).toBe('No se encontró');
  });

  it('no se rompe con algo que no sea una respuesta HTTP', () => {
    const p = comoProblema(new Error('cualquier cosa'));

    expect(p.estado).toBeNull();
    expect(p.titulo).toBe('Algo salió mal');
  });
});
