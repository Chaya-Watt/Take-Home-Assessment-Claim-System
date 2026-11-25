import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { UserRole } from '../api/models';
import { AuthStore } from './auth-store';

/** Only signed-in users; others go to the login page and come back afterwards. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthStore);
  return auth.hasValidSession()
    ? true
    : inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/**
 * Only the given roles (mirrors the backend's authorization policies). Use after `authGuard`.
 * A user with another role is sent to their own home page.
 */
export function roleGuard(...roles: UserRole[]): CanActivateFn {
  return () => {
    const auth = inject(AuthStore);
    const role = auth.role();
    return role !== null && roles.includes(role) ? true : inject(Router).createUrlTree([auth.homeUrl()]);
  };
}
