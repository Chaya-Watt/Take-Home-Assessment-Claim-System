import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { UserRole } from '../api/models';
import { fakeSession, storeSession } from '../../../testing/sessions';
import { authGuard, roleGuard } from './guards';

describe('route guards', () => {
  function setUp(role: UserRole | null) {
    localStorage.clear();
    if (role) {
      storeSession(fakeSession(role));
    }
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideRouter([])] });
  }

  /** Runs a guard and returns `true` or the URL it redirects to. */
  function run(guard: ReturnType<typeof roleGuard>, url = '/somewhere'): true | string {
    const result = TestBed.runInInjectionContext(() =>
      guard({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot),
    );
    return result instanceof UrlTree ? TestBed.inject(Router).serializeUrl(result) : (result as true);
  }

  describe('authGuard', () => {
    it('lets a signed-in user through', () => {
      setUp('Claimant');
      expect(run(authGuard)).toBe(true);
    });

    it('sends a signed-out user to login and remembers where they were going', () => {
      setUp(null);
      expect(run(authGuard, '/claims/new')).toBe('/login?returnUrl=%2Fclaims%2Fnew');
    });
  });

  describe('roleGuard', () => {
    it.each([
      ['Staff', true],
      ['Supervisor', true], // supervisors can do everything staff can
      ['Claimant', '/claims'],
    ] as const)('staff pages: %s → %s', (role, expected) => {
      setUp(role);
      expect(run(roleGuard('Staff', 'Supervisor'))).toBe(expected);
    });

    it.each([
      ['Supervisor', true],
      ['Staff', '/staff/claims'],
      ['Claimant', '/claims'],
    ] as const)('supervisor dashboard: %s → %s', (role, expected) => {
      setUp(role);
      expect(run(roleGuard('Supervisor'))).toBe(expected);
    });

    it.each([
      ['Claimant', true],
      ['Staff', '/staff/claims'],
      ['Supervisor', '/supervisor'],
    ] as const)('claimant pages: %s → %s', (role, expected) => {
      setUp(role);
      expect(run(roleGuard('Claimant'))).toBe(expected);
    });
  });
});
