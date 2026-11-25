import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { fakeSession, storeSession } from '../../../testing/sessions';
import { AuthStore } from './auth-store';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  function setUp(signedIn: boolean) {
    localStorage.clear();
    if (signedIn) {
      storeSession(fakeSession('Claimant'));
    }
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    return { http: TestBed.inject(HttpClient), backend: TestBed.inject(HttpTestingController), navigate };
  }

  it('adds the bearer token to API calls when signed in', () => {
    const { http, backend } = setUp(true);

    http.get('/api/claims/mine').subscribe();

    expect(backend.expectOne('/api/claims/mine').request.headers.get('Authorization')).toBe('Bearer token-Claimant');
  });

  it('sends no token when signed out', () => {
    const { http, backend } = setUp(false);

    http.get('/api/policies/mine').subscribe();

    expect(backend.expectOne('/api/policies/mine').request.headers.has('Authorization')).toBe(false);
  });

  it('signs out and goes to the login page when the API rejects the token', () => {
    const { http, backend, navigate } = setUp(true);
    let failed = false;

    http.get('/api/claims/mine').subscribe({ error: () => (failed = true) });
    backend.expectOne('/api/claims/mine').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(failed).toBe(true); // the caller still sees the error
    expect(TestBed.inject(AuthStore).isLoggedIn()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(['/login'], { queryParams: { expired: true } });
  });

  it('leaves a failed sign-in (401 without a token) to the login page', () => {
    const { http, backend, navigate } = setUp(false);

    http.post('/api/auth/login', {}).subscribe({ error: () => undefined });
    backend.expectOne('/api/auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(navigate).not.toHaveBeenCalled();
  });
});
