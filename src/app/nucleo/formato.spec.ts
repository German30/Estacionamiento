import { duracion, minutosDesde, pesos, placaNormalizada } from './formato';

describe('formato', () => {
  describe('placaNormalizada', () => {
    it('reproduce lo que hará el dominio: mayúsculas, sin espacios ni guiones', () => {
      expect(placaNormalizada('abc-1234')).toBe('ABC1234');
      expect(placaNormalizada(' ABC 1234 ')).toBe('ABC1234');
      expect(placaNormalizada('ABC1234')).toBe('ABC1234');
    });

    it('deja pasar una placa vacía en vez de inventar un valor', () => {
      expect(placaNormalizada('')).toBe('');
      expect(placaNormalizada('---')).toBe('');
    });
  });

  describe('minutosDesde', () => {
    // El dominio cobra toda fracción como minuto completo. Si acá se redondeara hacia abajo, el
    // contador de la caseta enseñaría un minuto menos que el que se acaba de facturar.
    it('redondea hacia arriba, como la política de tiempo del dominio', () => {
      const entrada = '2026-08-22T12:00:00';
      const base = new Date(entrada).getTime();

      expect(minutosDesde(entrada, base)).toBe(0);
      expect(minutosDesde(entrada, base + 1_000)).toBe(1);
      expect(minutosDesde(entrada, base + 60_000)).toBe(1);
      expect(minutosDesde(entrada, base + 61_000)).toBe(2);
    });

    it('nunca devuelve negativos si el reloj del equipo va atrasado', () => {
      const entrada = '2026-08-22T12:00:00';
      const base = new Date(entrada).getTime();

      expect(minutosDesde(entrada, base - 600_000)).toBe(0);
    });
  });

  describe('duracion', () => {
    it('se queda en minutos por debajo de la hora', () => {
      expect(duracion(0)).toBe('0 min');
      expect(duracion(59)).toBe('59 min');
    });

    it('pasa a horas y rellena los minutos a dos cifras', () => {
      expect(duracion(60)).toBe('1 h 00 min');
      expect(duracion(187)).toBe('3 h 07 min');
    });

    it('pasa a días a partir de las veinticuatro horas', () => {
      expect(duracion(1440)).toBe('1 d 00 h');
      expect(duracion(1500)).toBe('1 d 01 h');
    });
  });

  describe('pesos', () => {
    it('siempre lleva dos decimales', () => {
      expect(pesos(0)).toContain('0.00');
      expect(pesos(738.5)).toContain('738.50');
    });
  });
});
