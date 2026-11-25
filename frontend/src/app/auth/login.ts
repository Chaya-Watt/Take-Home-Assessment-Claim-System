import { Component, computed, inject, input, signal } from '@angular/core';
import { email, form, FormField, FormRoot, required } from '@angular/forms/signals';
import { Router, RouterLink } from '@angular/router';
import { errorMessage } from '../core/api/problem';
import { AuthStore } from '../core/auth/auth-store';
import { ErrorAlert } from '../core/ui/error-alert';
import { ariaInvalid, FieldError } from '../core/ui/field-error';

@Component({
  selector: 'app-login',
  imports: [FormRoot, FormField, RouterLink, ErrorAlert, FieldError],
  template: `
    <article class="narrow">
      <h1 tabindex="-1">Sign in</h1>
      @if (expired()) {
        <p role="status">Your session has expired. Please sign in again.</p>
      }

      <form [formRoot]="loginForm">
        <label for="login-email">Email</label>
        <input
          id="login-email"
          type="email"
          autocomplete="username"
          [formField]="loginForm.email"
          [attr.aria-invalid]="ariaInvalid(loginForm.email())"
          aria-describedby="login-email-error"
        />
        <app-field-error errorId="login-email-error" [field]="loginForm.email()" />

        <label for="login-password">Password</label>
        <input
          id="login-password"
          type="password"
          autocomplete="current-password"
          [formField]="loginForm.password"
          [attr.aria-invalid]="ariaInvalid(loginForm.password())"
          aria-describedby="login-password-error"
        />
        <app-field-error errorId="login-password-error" [field]="loginForm.password()" />

        <app-error-alert [message]="formError()" />
        <button type="submit" [disabled]="loginForm().submitting()" [attr.aria-busy]="loginForm().submitting()">
          Sign in
        </button>
      </form>

      <p>New claimant? <a routerLink="/register">Create an account</a></p>
      <details>
        <summary>Demo accounts</summary>
        <p>
          Password for all: <code>Password123!</code><br />
          <code>claimant1@example.com</code>, <code>claimant2@example.com</code>,
          <code>staff1@example.com</code>, <code>staff2@example.com</code>, <code>supervisor@example.com</code>
        </p>
      </details>
    </article>
  `,
})
export class Login {
  /** Query parameters, bound by the router (`withComponentInputBinding`). */
  readonly returnUrl = input<string>();
  readonly expired = input<string>();

  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);

  private readonly model = signal({ email: '', password: '' });
  protected readonly loginForm = form(
    this.model,
    (path) => {
      required(path.email, { message: 'Enter your email.' });
      email(path.email, { message: 'Enter a valid email address.' });
      required(path.password, { message: 'Enter your password.' });
    },
    {
      submission: {
        action: async (field) => {
          try {
            await this.auth.login(field().value());
          } catch (error) {
            return { kind: 'server', message: errorMessage(error) };
          }
          await this.router.navigateByUrl(this.destination());
          return undefined;
        },
      },
    },
  );

  /** Error returned by the server on submit (field validation errors are shown under each field). */
  protected readonly formError = computed(() => this.loginForm().errors()[0]?.message);
  protected readonly ariaInvalid = ariaInvalid;

  /** Back to the page that required sign-in, if it is an in-app path; otherwise the role's home page. */
  private destination(): string {
    const url = this.returnUrl();
    return url && url.startsWith('/') && !url.startsWith('//') ? url : this.auth.homeUrl();
  }
}
