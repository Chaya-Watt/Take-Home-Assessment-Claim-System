import { AuthResponse, UserRole } from '../app/core/api/models';
import { sessionStorageKey } from '../app/core/auth/auth-store';

/** A session as returned by POST /api/auth/login. Test-only helper (excluded from the app build). */
export function fakeSession(role: UserRole, expiresInMinutes = 60): AuthResponse {
  return {
    accessToken: `token-${role}`,
    expiresAt: new Date(Date.now() + expiresInMinutes * 60_000).toISOString(),
    user: { id: `id-${role}`, email: `${role.toLowerCase()}@example.com`, fullName: `Test ${role}`, role },
  };
}

/** Signs a user in the way a page reload would: AuthStore reads localStorage when it is created. */
export function storeSession(session: AuthResponse): void {
  localStorage.setItem(sessionStorageKey, JSON.stringify(session));
}
