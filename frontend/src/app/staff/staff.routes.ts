import { Routes } from '@angular/router';

/** Staff pages (also used by supervisors), lazy-loaded under /staff/claims (guarded in app.routes.ts). */
export const staffRoutes: Routes = [
  { path: '', title: 'Work queue', loadComponent: () => import('./work-queue').then((m) => m.WorkQueue) },
  {
    path: ':claimId',
    title: 'Claim',
    loadComponent: () => import('./staff-claim-detail').then((m) => m.StaffClaimDetail),
  },
];
