import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { DashboardClaim, SupervisorDashboard } from '../core/api/models';
import { Dashboard } from './dashboard';

describe('Dashboard', () => {
  let fixture: ComponentFixture<Dashboard>;
  let backend: HttpTestingController;

  const claim = (overrides: Partial<DashboardClaim>): DashboardClaim => ({
    id: 'claim-1',
    claimNumber: 'CLM-2026-000001',
    status: 'Submitted',
    assignedStaffId: null,
    assignedStaffName: null,
    submittedAt: '2026-11-22T12:00:00Z',
    inStatusSince: '2026-11-22T12:00:00Z',
    ...overrides,
  });

  const data: SupervisorDashboard = {
    generatedAt: '2026-11-25T12:00:00Z',
    claimsByStatus: { Submitted: 3, UnderReview: 1, PendingInformation: 0, Assessed: 0, Approved: 0, Rejected: 2, Paid: 5, Withdrawn: 0 },
    unassignedCount: 3,
    oldestUnassigned: [claim({ id: 'u1', claimNumber: 'CLM-2026-000010' })],
    longestInCurrentStatus: [
      claim({ id: 'u1', claimNumber: 'CLM-2026-000010' }),
      claim({
        id: 'r1',
        claimNumber: 'CLM-2026-000011',
        status: 'UnderReview',
        assignedStaffId: 'staff-1',
        assignedStaffName: 'Anan Wongsa',
        inStatusSince: '2026-11-25T07:00:00Z',
      }),
    ],
    staff: [
      {
        staffId: 'staff-1',
        fullName: 'Anan Wongsa',
        role: 'Staff',
        openClaims: 1,
        openClaimsByStatus: { UnderReview: 1 },
        claims: [
          claim({
            id: 'r1',
            claimNumber: 'CLM-2026-000011',
            status: 'UnderReview',
            assignedStaffId: 'staff-1',
            assignedStaffName: 'Anan Wongsa',
            inStatusSince: '2026-11-25T07:00:00Z',
          }),
        ],
      },
      { staffId: 'staff-2', fullName: 'Pim Srisuk', role: 'Staff', openClaims: 0, openClaimsByStatus: {}, claims: [] },
    ],
  };

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [Dashboard],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    backend = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(Dashboard);
    TestBed.tick(); // httpResource: send the request, flush, then wait
    backend.expectOne('/api/supervisor/dashboard').flush(data);
    await fixture.whenStable();
  });

  const element = () => fixture.nativeElement as HTMLElement;
  const section = (headingId: string) => element().querySelector(`[aria-labelledby="${headingId}"]`)!;

  it('shows how many claims are in each status', () => {
    const cells = [...section('by-status-heading').querySelectorAll('tbody td')].map((td) => td.textContent?.trim());
    expect(cells).toEqual(['3', '1', '0', '0', '0', '2', '5', '0']);
  });

  it('lists the unassigned claims with how long they have waited, linking to the claim', () => {
    const unassigned = section('unassigned-heading');
    expect(unassigned.querySelector('h2')?.textContent).toContain('(3)');
    expect(unassigned.textContent).toContain('3 days'); // submitted 22 Nov, dashboard built 25 Nov
    expect(unassigned.querySelector('a')?.getAttribute('href')).toBe('/staff/claims/u1');
  });

  it('shows who holds each stuck claim', () => {
    const rows = [...section('stuck-heading').querySelectorAll('tbody tr')].map((tr) => tr.textContent);
    expect(rows[0]).toContain('Nobody yet');
    expect(rows[1]).toContain('Anan Wongsa');
    expect(rows[1]).toContain('5 hours');
  });

  it("shows each staff member's open claims", () => {
    const [anan, pim] = [...section('workload-heading').querySelectorAll('details')];
    expect(anan.querySelector('summary')?.textContent).toContain('Anan Wongsa (Staff): 1 open claim');
    expect(anan.hasAttribute('open')).toBe(true);
    expect(anan.querySelector('li a')?.getAttribute('href')).toBe('/staff/claims/r1');
    expect(pim.querySelector('summary')?.textContent).toContain('0 open claims');
    expect(pim.hasAttribute('open')).toBe(false);
  });

  it('reloads when Refresh is pressed', async () => {
    const refresh = [...element().querySelectorAll('button')].find((b) => b.textContent?.trim() === 'Refresh')!;

    refresh.click();
    TestBed.tick();

    backend.expectOne('/api/supervisor/dashboard').flush({ ...data, unassignedCount: 0, oldestUnassigned: [] });
    await fixture.whenStable();
    expect(section('unassigned-heading').textContent).toContain('Every claim has been picked up.');
  });
});
