import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { StaffClaimSummaryPage } from '../core/api/models';
import { fakeSession, storeSession } from '../../testing/sessions';
import { WorkQueue } from './work-queue';

describe('WorkQueue', () => {
  let fixture: ComponentFixture<WorkQueue>;
  let backend: HttpTestingController;

  const page = (assignedStaffId: string | null): StaffClaimSummaryPage => ({
    items: [
      {
        id: 'claim-1',
        claimNumber: 'CLM-2026-000001',
        incidentType: 'Collision',
        incidentDate: '2026-11-20',
        claimedAmount: 40_000_00,
        status: assignedStaffId ? 'UnderReview' : 'Submitted',
        submittedAt: '2026-11-20T03:00:00Z',
        assignedStaffId,
        assignedAt: assignedStaffId ? '2026-11-20T04:00:00Z' : null,
      },
    ],
    page: 1,
    pageSize: 10,
    totalCount: 1,
  });

  beforeEach(() => {
    localStorage.clear();
    storeSession(fakeSession('Staff')); // user id "id-Staff"
    TestBed.configureTestingModule({
      imports: [WorkQueue],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    backend = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(WorkQueue);
    TestBed.tick(); // httpResource: send the request, then flush in each test
  });

  const element = () => fixture.nativeElement as HTMLElement;
  const tab = (label: string) =>
    [...element().querySelectorAll<HTMLButtonElement>('[role="group"] button')].find((b) => b.textContent?.trim() === label)!;

  it('starts with the claims waiting to be picked up', async () => {
    const request = backend.expectOne((r) => r.url === '/api/staff/claims');
    expect(request.request.params.get('queue')).toBe('Unassigned');
    request.flush(page(null));
    await fixture.whenStable();

    expect(tab('Waiting to be picked up').getAttribute('aria-pressed')).toBe('true');
    const row = element().querySelector('tbody tr')!;
    expect(row.textContent).toContain('CLM-2026-000001');
    expect(row.textContent).toContain('Collision, Nov 20, 2026'); // API sent "2026-11-20"
    expect(row.textContent).toContain('Nobody yet');
    expect(row.querySelector('a')?.getAttribute('href')).toBe('/staff/claims/claim-1');
  });

  it('switches to my claims and shows that I handle them', async () => {
    backend.expectOne((r) => r.url === '/api/staff/claims').flush(page(null));
    await fixture.whenStable();

    tab('My claims').click();
    TestBed.tick();
    const request = backend.expectOne((r) => r.url === '/api/staff/claims');
    expect(request.request.params.get('queue')).toBe('Mine');
    request.flush(page('id-Staff'));
    await fixture.whenStable();

    expect(element().querySelector('tbody tr')?.textContent).toContain('You');
  });

  it('sends no queue for "All claims"', async () => {
    backend.expectOne((r) => r.url === '/api/staff/claims').flush(page(null));
    await fixture.whenStable();

    tab('All claims').click();
    TestBed.tick();

    const request = backend.expectOne((r) => r.url === '/api/staff/claims');
    expect(request.request.params.has('queue')).toBe(false);
    request.flush(page('id-other'));
    await fixture.whenStable();
    expect(element().querySelector('tbody tr')?.textContent).toContain('Another officer');
  });
});
