import { DOCUMENT } from '@angular/common';
import { afterNextRender, Component, computed, inject, Injector } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, skip } from 'rxjs';
import { UserRole } from './core/api/models';
import { AuthStore } from './core/auth/auth-store';

interface NavLink {
  path: string;
  label: string;
}

/** Supervisors also get the staff work queue: they can do everything staff can. */
export const navLinks: Record<UserRole, readonly NavLink[]> = {
  Claimant: [
    { path: '/claims', label: 'My claims' },
    { path: '/claims/new', label: 'Report an incident' },
  ],
  Staff: [{ path: '/staff/claims', label: 'Work queue' }],
  Supervisor: [
    { path: '/supervisor', label: 'Dashboard' },
    { path: '/staff/claims', label: 'Work queue' },
  ],
};

/** App shell: skip link, header with the signed-in user's navigation, and the routed page. */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <a class="skip-link" href="#main-content" (click)="skipToContent($event)">Skip to main content</a>
    <header class="container">
      <nav aria-label="Main">
        <ul>
          <li><a routerLink="/"><strong>Claim System</strong></a></li>
        </ul>
        @if (auth.user(); as user) {
          <ul>
            @for (link of links(); track link.path) {
              <li>
                <a
                  [routerLink]="link.path"
                  routerLinkActive="active"
                  [routerLinkActiveOptions]="{ exact: true }"
                  ariaCurrentWhenActive="page"
                  >{{ link.label }}</a
                >
              </li>
            }
            <li>
              <span>{{ user.fullName }} <small>({{ user.role }})</small></span>
            </li>
            <li><button type="button" class="outline secondary" (click)="logout()">Sign out</button></li>
          </ul>
        }
      </nav>
    </header>
    <main class="container" id="main-content" tabindex="-1">
      <router-outlet />
    </main>
  `,
})
export class App {
  protected readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly document = inject(DOCUMENT);

  protected readonly links = computed(() => {
    const role = this.auth.role();
    return role ? navLinks[role] : [];
  });

  constructor() {
    // Accessibility: after each in-app navigation, move focus to the new page's heading
    // so screen-reader and keyboard users start at the top of the new content.
    const injector = inject(Injector);
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        skip(1), // not on the first page load
        takeUntilDestroyed(),
      )
      .subscribe(() =>
        afterNextRender(() => this.document.querySelector<HTMLElement>('main h1')?.focus(), { injector }),
      );
  }

  protected logout(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }

  /** A plain "#main-content" link would trigger a full navigation with the router's <base href>. */
  protected skipToContent(event: Event): void {
    event.preventDefault();
    this.document.getElementById('main-content')?.focus();
  }
}
