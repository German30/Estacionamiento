import type { Routes } from '@angular/router';

// La caseta es la raíz: es la pantalla donde alguien pasa el turno entero. El panel es de
// consulta y se visita, no se habita.
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'caseta' },
  {
    path: 'caseta',
    title: 'Caseta — Padrón',
    loadComponent: () => import('./paginas/caseta/caseta').then((m) => m.Caseta),
  },
  {
    path: 'panel',
    title: 'Panel — Padrón',
    loadComponent: () => import('./paginas/panel/panel').then((m) => m.Panel),
  },
  {
    path: 'padron',
    title: 'Padrón de vehículos',
    loadComponent: () => import('./paginas/padron/padron').then((m) => m.Padron),
  },
  {
    path: 'padron/:placa',
    title: 'Ficha de vehículo — Padrón',
    loadComponent: () => import('./paginas/ficha/ficha').then((m) => m.Ficha),
  },
  {
    path: 'cierre',
    title: 'Cierre de mes — Padrón',
    loadComponent: () => import('./paginas/cierre/cierre').then((m) => m.Cierre),
  },
  { path: '**', redirectTo: 'caseta' },
];
