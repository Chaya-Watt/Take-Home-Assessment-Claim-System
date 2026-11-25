import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { UserRole } from './core/api/models';
import { fakeSession, storeSession } from '../testing/sessions';
import { App } from './app';

describe('App shell', () => {
  async function render(role: UserRole | null): Promise<HTMLElement> {
    localStorage.clear();
    if (role) {
      storeSession(fakeSession(role));
    }
    TestBed.configureTestingModule({ imports: [App], providers: [provideHttpClient(), provideRouter([])] });
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  const navLabels = (element: HTMLElement) =>
    [...element.querySelectorAll('nav a')].map((a) => a.textContent?.trim()).slice(1); // skip the brand link

  it('shows only the brand, a skip link and the main landmark when signed out', async () => {
    const element = await render(null);

    expect(navLabels(element)).toEqual([]);
    expect(element.querySelector('.skip-link')).not.toBeNull();
    expect(element.querySelector('main#main-content')).not.toBeNull();
  });

  it.each([
    ['Claimant', ['My claims', 'Report an incident']],
    ['Staff', ['Work queue']],
    ['Supervisor', ['Dashboard', 'Work queue']],
  ] as const)('shows the %s navigation', async (role, expected) => {
    const element = await render(role);

    expect(navLabels(element)).toEqual(expected);
    expect(element.querySelector('nav')?.textContent).toContain(`Test ${role}`);
    expect(element.querySelector('nav button')?.textContent).toContain('Sign out');
  });
});
