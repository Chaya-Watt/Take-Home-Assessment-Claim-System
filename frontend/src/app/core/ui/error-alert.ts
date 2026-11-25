import { Component, input } from '@angular/core';

/** Shows an error message and announces it to screen readers (role="alert"). Renders nothing when empty. */
@Component({
  selector: 'app-error-alert',
  template: `
    @if (message()) {
      <p class="error-alert" role="alert">{{ message() }}</p>
    }
  `,
})
export class ErrorAlert {
  readonly message = input<string | null | undefined>(null);
}
