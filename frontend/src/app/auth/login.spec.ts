import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { fakeSession } from '../../testing/sessions';
import { Login } from './login';

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let backend: HttpTestingController;
  let navigateByUrl: ReturnType<typeof vi.spyOn>;

  beforeEach(async () => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    navigateByUrl = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    backend = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(Login);
    await fixture.whenStable();
  });

  const element = () => fixture.nativeElement as HTMLElement;

  function type(selector: string, value: string): void {
    const input = element().querySelector<HTMLInputElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  async function submit(): Promise<void> {
    element().querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    await fixture.whenStable();
  }

  it('shows field errors and does not call the API when the form is empty', async () => {
    await submit();

    expect(element().querySelector('#login-email-error')?.textContent).toContain('Enter your email.');
    expect(element().querySelector('#login-password-error')?.textContent).toContain('Enter your password.');
    expect(element().querySelector('#login-email')?.getAttribute('aria-invalid')).toBe('true');
    backend.expectNone('/api/auth/login');
  });

  it('signs in and goes to the home page of the role', async () => {
    type('#login-email', 'staff1@example.com');
    type('#login-password', 'Password123!');
    await submit();

    const request = backend.expectOne('/api/auth/login');
    expect(request.request.body).toEqual({ email: 'staff1@example.com', password: 'Password123!' });
    request.flush(fakeSession('Staff'));

    await vi.waitFor(() => expect(navigateByUrl).toHaveBeenCalledWith('/staff/claims'));
  });

  it('shows the error message from the backend', async () => {
    type('#login-email', 'staff1@example.com');
    type('#login-password', 'wrong');
    await submit();

    backend
      .expectOne('/api/auth/login')
      .flush({ status: 401, detail: 'Invalid email or password.' }, { status: 401, statusText: 'Unauthorized' });

    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(element().querySelector('[role="alert"]')?.textContent).toContain('Invalid email or password.');
    });
    expect(navigateByUrl).not.toHaveBeenCalled();
  });

  it('returns to the page that required sign-in', async () => {
    fixture.componentRef.setInput('returnUrl', '/claims/new');
    type('#login-email', 'claimant1@example.com');
    type('#login-password', 'Password123!');
    await submit();

    backend.expectOne('/api/auth/login').flush(fakeSession('Claimant'));

    await vi.waitFor(() => expect(navigateByUrl).toHaveBeenCalledWith('/claims/new'));
  });

  it('ignores a returnUrl that points outside the app', async () => {
    fixture.componentRef.setInput('returnUrl', '//evil.example.com');
    type('#login-email', 'claimant1@example.com');
    type('#login-password', 'Password123!');
    await submit();

    backend.expectOne('/api/auth/login').flush(fakeSession('Claimant'));

    await vi.waitFor(() => expect(navigateByUrl).toHaveBeenCalledWith('/claims'));
  });
});
