import { HttpClient } from '@angular/common/http';
import { computed, inject, Service, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest, UserRole } from '../api/models';

export const sessionStorageKey = 'claim-system.session';

/** Where each role lands after signing in. */
const homeByRole: Record<UserRole, string> = {
  Claimant: '/claims',
  Staff: '/staff/claims',
  Supervisor: '/supervisor',
};

/**
 * The signed-in user and their JWT. Kept in a signal and in localStorage so a page reload stays signed in.
 * An expired session is dropped instead of being sent to the API.
 */
@Service()
export class AuthStore {
  private readonly http = inject(HttpClient);
  private readonly session = signal<AuthResponse | null>(loadSession());

  readonly user = computed(() => this.session()?.user ?? null);
  readonly role = computed(() => this.user()?.role ?? null);
  readonly token = computed(() => this.session()?.accessToken ?? null);
  readonly isLoggedIn = computed(() => this.session() !== null);
  readonly homeUrl = computed(() => {
    const role = this.role();
    return role ? homeByRole[role] : '/login';
  });

  async login(request: LoginRequest): Promise<void> {
    this.start(await firstValueFrom(this.http.post<AuthResponse>('/api/auth/login', request)));
  }

  /** Creates a claimant account and signs it in. */
  async register(request: RegisterRequest): Promise<void> {
    this.start(await firstValueFrom(this.http.post<AuthResponse>('/api/auth/register', request)));
  }

  logout(): void {
    this.session.set(null);
    localStorage.removeItem(sessionStorageKey);
  }

  /** True when signed in with a token that has not expired yet; signs out otherwise. */
  hasValidSession(): boolean {
    const session = this.session();
    if (session && !isExpired(session)) {
      return true;
    }
    if (session) {
      this.logout();
    }
    return false;
  }

  private start(session: AuthResponse): void {
    this.session.set(session);
    localStorage.setItem(sessionStorageKey, JSON.stringify(session));
  }
}

function isExpired(session: AuthResponse): boolean {
  return Date.parse(session.expiresAt) <= Date.now();
}

function loadSession(): AuthResponse | null {
  try {
    const stored = localStorage.getItem(sessionStorageKey);
    const session = stored ? (JSON.parse(stored) as AuthResponse) : null;
    return session && !isExpired(session) ? session : null;
  } catch {
    return null; // corrupted value: treat as signed out
  }
}
