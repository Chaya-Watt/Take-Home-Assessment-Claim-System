import { Component, input } from '@angular/core';

/** Temporary page for routes whose screens are built in a later phase (replaced as each phase lands). */
@Component({
  selector: 'app-coming-soon',
  template: `
    <h1 tabindex="-1">{{ heading() }}</h1>
    <p>This page is built in phase {{ phase() }} of the frontend plan.</p>
  `,
})
export class ComingSoon {
  /** Route data, bound by the router (`withComponentInputBinding`). */
  readonly heading = input('');
  readonly phase = input('');
}
