import { inject } from '@angular/core';
import { Routes } from '@angular/router';
import { AuthStore } from './core/auth/auth-store';
import { authGuard, roleGuard } from './core/auth/guards';

const comingSoon = () => import('./layout/coming-soon').then((m) => m.ComingSoon);

/**
 * Access per route mirrors the backend policies:
 * /claims → Claimant, /staff → Staff or Supervisor, /supervisor → Supervisor.
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: () => inject(AuthStore).homeUrl() },
  { path: 'login', title: 'Sign in', loadComponent: () => import('./auth/login').then((m) => m.Login) },
  { path: 'register', title: 'Register', loadComponent: () => import('./auth/register').then((m) => m.Register) },

  // Placeholders until phases F2–F4 build these screens.
  {
    path: 'claims',
    canActivate: [authGuard, roleGuard('Claimant')],
    children: [
      { path: '', title: 'My claims', loadComponent: comingSoon, data: { heading: 'My claims', phase: 'F2' } },
      { path: 'new', title: 'Report an incident', loadComponent: comingSoon, data: { heading: 'Report an incident', phase: 'F2' } },
    ],
  },
  {
    path: 'staff/claims',
    canActivate: [authGuard, roleGuard('Staff', 'Supervisor')],
    title: 'Work queue',
    loadComponent: comingSoon,
    data: { heading: 'Work queue', phase: 'F3' },
  },
  {
    path: 'supervisor',
    canActivate: [authGuard, roleGuard('Supervisor')],
    title: 'Team dashboard',
    loadComponent: comingSoon,
    data: { heading: 'Team dashboard', phase: 'F4' },
  },

  { path: '**', redirectTo: '' },
];
