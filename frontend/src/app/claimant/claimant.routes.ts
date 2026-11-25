import { Routes } from '@angular/router';

/** Claimant pages, lazy-loaded under /claims (guarded in app.routes.ts). */
export const claimantRoutes: Routes = [
  { path: '', title: 'My claims', loadComponent: () => import('./my-claims').then((m) => m.MyClaims) },
  { path: 'new', title: 'Report an incident', loadComponent: () => import('./new-claim').then((m) => m.NewClaim) },
  { path: ':claimId', title: 'Claim', loadComponent: () => import('./claim-detail').then((m) => m.ClaimDetail) },
];
