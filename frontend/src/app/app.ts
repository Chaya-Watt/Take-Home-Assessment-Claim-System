import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/** App shell. The header/navigation per role is added in phase F1. */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: `
    <header class="container">
      <nav aria-label="Main">
        <ul>
          <li><strong>Claim System</strong></li>
        </ul>
      </nav>
    </header>
    <main class="container">
      <router-outlet />
    </main>
  `,
})
export class App {}
