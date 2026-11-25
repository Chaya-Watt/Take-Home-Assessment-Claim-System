import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { fakeSession, storeSession } from '../../../testing/sessions';
import { AuthStore, sessionStorageKey } from './auth-store';

describe('AuthStore', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('signs in, exposes the user and remembers the session', async () => {
    const store = TestBed.inject(AuthStore);
    const session = fakeSession('Staff');

    const done = store.login({ email: 'staff@example.com', password: 'Password123!' });
    const request = TestBed.inject(HttpTestingController).expectOne('/api/auth/login');
    expect(request.request.method).toBe('POST');
    request.flush(session);
    await done;

    expect(store.isLoggedIn()).toBe(true);
    expect(store.role()).toBe('Staff');
    expect(store.token()).toBe('token-Staff');
    expect(store.homeUrl()).toBe('/staff/claims');
    expect(JSON.parse(localStorage.getItem(sessionStorageKey)!)).toEqual(session);
  });

  it('registers a claimant and signs them in', async () => {
    const store = TestBed.inject(AuthStore);

    const done = store.register({ email: 'new@example.com', password: 'Password123!', fullName: 'New' });
    TestBed.inject(HttpTestingController).expectOne('/api/auth/register').flush(fakeSession('Claimant'));
    await done;

    expect(store.homeUrl()).toBe('/claims');
  });

  it('restores a stored session after a page reload', () => {
    storeSession(fakeSession('Supervisor'));

    const store = TestBed.inject(AuthStore);

    expect(store.user()?.fullName).toBe('Test Supervisor');
    expect(store.homeUrl()).toBe('/supervisor');
  });

  it('ignores a stored session that has already expired', () => {
    storeSession(fakeSession('Claimant', -1));

    expect(TestBed.inject(AuthStore).isLoggedIn()).toBe(false);
  });

  it('signs out when the session expires while the app is open', () => {
    vi.useFakeTimers();
    try {
      storeSession(fakeSession('Claimant', 1));
      const store = TestBed.inject(AuthStore);
      expect(store.hasValidSession()).toBe(true);

      vi.advanceTimersByTime(2 * 60_000);

      expect(store.hasValidSession()).toBe(false);
      expect(store.isLoggedIn()).toBe(false);
      expect(localStorage.getItem(sessionStorageKey)).toBeNull();
    } finally {
      vi.useRealTimers();
    }
  });

  it('logout clears the user and storage', () => {
    storeSession(fakeSession('Claimant'));
    const store = TestBed.inject(AuthStore);

    store.logout();

    expect(store.user()).toBeNull();
    expect(store.homeUrl()).toBe('/login');
    expect(localStorage.getItem(sessionStorageKey)).toBeNull();
  });
});
