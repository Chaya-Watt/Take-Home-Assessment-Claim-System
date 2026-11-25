import { inject } from '@angular/core';
import { Routes } from '@angular/router';
import { AuthStore } from './core/auth/auth-store';
import { authGuard, roleGuard } from './core/auth/guards';

/**
 * Access per route mirrors the backend policies:
 * /claims → Claimant, /staff → Staff or Supervisor, /supervisor → Supervisor.
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: () => inject(AuthStore).homeUrl() },
  { path: 'login', title: 'Sign in', loadComponent: () => import('./auth/login').then((m) => m.Login) },
  { path: 'register', title: 'Register', loadComponent: () => import('./auth/register').then((m) => m.Register) },

  {
    path: 'claims',
    canActivate: [authGuard, roleGuard('Claimant')],
    loadChildren: () => import('./claimant/claimant.routes').then((m) => m.claimantRoutes),
  },

  {
    path: 'staff/claims',
    canActivate: [authGuard, roleGuard('Staff', 'Supervisor')],
    loadChildren: () => import('./staff/staff.routes').then((m) => m.staffRoutes),
  },

  {
    path: 'supervisor',
    canActivate: [authGuard, roleGuard('Supervisor')],
    title: 'Team dashboard',
    loadComponent: () => import('./supervisor/dashboard').then((m) => m.Dashboard),
  },

  { path: '**', redirectTo: '' },
];
