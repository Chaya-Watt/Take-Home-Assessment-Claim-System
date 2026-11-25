import { Component, computed, inject, signal } from '@angular/core';
import { email, form, FormField, FormRoot, minLength, required } from '@angular/forms/signals';
import { Router, RouterLink } from '@angular/router';
import { errorMessage } from '../core/api/problem';
import { AuthStore } from '../core/auth/auth-store';
import { ErrorAlert } from '../core/ui/error-alert';
import { ariaInvalid, FieldError } from '../core/ui/field-error';

/** Same rule as the backend (AuthService.MinPasswordLength). */
const minPasswordLength = 8;

@Component({
  selector: 'app-register',
  imports: [FormRoot, FormField, RouterLink, ErrorAlert, FieldError],
  template: `
    <article class="narrow">
      <h1 tabindex="-1">Create a claimant account</h1>

      <form [formRoot]="registerForm">
        <label for="register-name">Full name</label>
        <input
          id="register-name"
          autocomplete="name"
          [formField]="registerForm.fullName"
          [attr.aria-invalid]="ariaInvalid(registerForm.fullName())"
          aria-describedby="register-name-error"
        />
        <app-field-error errorId="register-name-error" [field]="registerForm.fullName()" />

        <label for="register-email">Email</label>
        <input
          id="register-email"
          type="email"
          autocomplete="email"
          [formField]="registerForm.email"
          [attr.aria-invalid]="ariaInvalid(registerForm.email())"
          aria-describedby="register-email-error"
        />
        <app-field-error errorId="register-email-error" [field]="registerForm.email()" />

        <label for="register-password">Password</label>
        <input
          id="register-password"
          type="password"
          autocomplete="new-password"
          [formField]="registerForm.password"
          [attr.aria-invalid]="ariaInvalid(registerForm.password())"
          aria-describedby="register-password-error"
        />
        <app-field-error errorId="register-password-error" [field]="registerForm.password()" />

        <app-error-alert [message]="formError()" />
        <button type="submit" [disabled]="registerForm().submitting()" [attr.aria-busy]="registerForm().submitting()">
          Create account
        </button>
      </form>

      <p>Already have an account? <a routerLink="/login">Sign in</a></p>
    </article>
  `,
})
export class Register {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);

  private readonly model = signal({ fullName: '', email: '', password: '' });
  protected readonly registerForm = form(
    this.model,
    (path) => {
      required(path.fullName, { message: 'Enter your full name.' });
      required(path.email, { message: 'Enter your email.' });
      email(path.email, { message: 'Enter a valid email address.' });
      required(path.password, { message: 'Choose a password.' });
      minLength(path.password, minPasswordLength, {
        message: `Password must be at least ${minPasswordLength} characters.`,
      });
    },
    {
      submission: {
        action: async (field) => {
          try {
            await this.auth.register(field().value());
          } catch (error) {
            return { kind: 'server', message: errorMessage(error) };
          }
          await this.router.navigateByUrl(this.auth.homeUrl());
          return undefined;
        },
      },
    },
  );

  protected readonly formError = computed(() => this.registerForm().errors()[0]?.message);
  protected readonly ariaInvalid = ariaInvalid;
}
