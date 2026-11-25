import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ClaimSummaryPage } from '../core/api/models';
import { MyClaims } from './my-claims';

describe('MyClaims', () => {
  let fixture: ComponentFixture<MyClaims>;
  let backend: HttpTestingController;

  const page = (totalCount: number, status: ClaimSummaryPage['items'][number]['status'] = 'Submitted'): ClaimSummaryPage => ({
    items: [
      {
        id: 'claim-1',
        claimNumber: 'CLM-2026-000001',
        policyId: 'policy-motor',
        incidentType: 'WaterDamage',
        incidentDate: '2026-11-20',
        claimedAmount: 45_000_00,
        status,
        submittedAt: '2026-11-20T03:00:00Z',
      },
    ],
    page: 1,
    pageSize: 10,
    totalCount,
  });

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [MyClaims],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    backend = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(MyClaims);
    // httpResource keeps the app "unstable" until answered: send the request with tick(), flush, then whenStable().
    TestBed.tick();
  });

  const element = () => fixture.nativeElement as HTMLElement;

  it('loads the first page and shows each claim', async () => {
    const call = backend.expectOne((r) => r.url === '/api/claims/mine');
    expect(call.request.params.get('page')).toBe('1');
    expect(call.request.params.has('status')).toBe(false);
    call.flush(page(1));
    await fixture.whenStable();

    const row = element().querySelector('tbody tr')!;
    expect(row.textContent).toContain('CLM-2026-000001');
    expect(row.textContent).toContain('Water damage');
    expect(row.textContent).toContain('฿45,000.00');
    expect(row.querySelector('a')?.getAttribute('href')).toBe('/claims/claim-1');
    expect(element().querySelector('.pager')?.textContent).toContain('Page 1 of 1');
  });

  it('reloads from page 1 with the chosen status', async () => {
    backend.expectOne((r) => r.url === '/api/claims/mine').flush(page(25));
    await fixture.whenStable();
    (element().querySelector('.pager button:last-of-type') as HTMLButtonElement).click(); // Next → page 2
    TestBed.tick();
    backend.expectOne((r) => r.params.get('page') === '2').flush(page(25));
    await fixture.whenStable();

    const select = element().querySelector<HTMLSelectElement>('#status-filter')!;
    select.value = 'Paid';
    select.dispatchEvent(new Event('change'));
    TestBed.tick();

    const call = backend.expectOne((r) => r.url === '/api/claims/mine');
    expect(call.request.params.get('status')).toBe('Paid');
    expect(call.request.params.get('page')).toBe('1');
  });

  it('tells the user when there are no claims', async () => {
    backend.expectOne((r) => r.url === '/api/claims/mine').flush({ items: [], page: 1, pageSize: 10, totalCount: 0 });
    await fixture.whenStable();

    expect(element().textContent).toContain('No claims found.');
  });
});
