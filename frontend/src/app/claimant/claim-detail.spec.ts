import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ClaimDetail as ClaimDetailModel } from '../core/api/models';
import { fakeClaim } from '../../testing/claims';
import { ClaimDetail } from './claim-detail';

describe('ClaimDetail', () => {
  let fixture: ComponentFixture<ClaimDetail>;
  let backend: HttpTestingController;

  async function render(claim: ClaimDetailModel): Promise<void> {
    TestBed.configureTestingModule({
      imports: [ClaimDetail],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    backend = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ClaimDetail);
    fixture.componentRef.setInput('claimId', claim.id);
    // httpResource keeps the app "unstable" until answered: send the request with tick(), flush, then wait.
    TestBed.tick();
    backend.expectOne(`/api/claims/${claim.id}`).flush(claim);
    await fixture.whenStable();
  }

  const element = () => fixture.nativeElement as HTMLElement;
  const button = (label: string) =>
    [...element().querySelectorAll('button')].find((b) => b.textContent?.trim() === label);

  it('shows the claim, its details and its timeline', async () => {
    await render(fakeClaim());

    expect(element().querySelector('h1')?.textContent).toContain('Claim CLM-2026-000001');
    expect(element().textContent).toContain('POL-MTR-0001');
    expect(element().textContent).toContain('฿45,000.00');
    expect(element().querySelectorAll('.timeline li')).toHaveLength(1);
  });

  it('shows calendar dates (incident date, due date) on the same day the API sent', async () => {
    // Regression: formatting "2026-11-20" with timeZone 'UTC' showed Nov 19 in UTC+ zones (e.g. Bangkok).
    await render(
      fakeClaim({
        status: 'PendingInformation',
        incidentDate: '2026-11-20',
        informationRequests: [
          {
            id: 'request-1',
            message: 'Photos please',
            dueDate: '2026-12-02',
            requestedAt: '2026-11-21T03:00:00Z',
            responseText: null,
            respondedAt: null,
            isOpen: true,
          },
        ],
      }),
    );

    expect(element().textContent).toContain('Collision on Nov 20, 2026');
    expect(element().textContent).toContain('Please answer by Dec 2, 2026.');
  });

  it('withdraws a submitted claim after confirmation', async () => {
    await render(fakeClaim({ status: 'Submitted' }));

    button('Withdraw claim')!.click();
    await fixture.whenStable();
    button('Yes, withdraw claim')!.click();

    backend
      .expectOne({ method: 'POST', url: '/api/claims/claim-1/withdraw' })
      .flush(fakeClaim({ status: 'Withdrawn' }));
    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(element().querySelector('h1 .status-badge')?.textContent).toBe('Withdrawn');
    });
    expect(button('Withdraw claim')).toBeUndefined(); // no longer possible
  });

  it('offers no withdraw once a claims officer has picked the claim up', async () => {
    await render(fakeClaim({ status: 'UnderReview' }));

    expect(button('Withdraw claim')).toBeUndefined();
  });

  it('lets the claimant answer an open information request', async () => {
    await render(
      fakeClaim({
        status: 'PendingInformation',
        informationRequests: [
          {
            id: 'request-1',
            message: 'Please send the police report number',
            dueDate: '2026-12-02',
            requestedAt: '2026-11-21T03:00:00Z',
            responseText: null,
            respondedAt: null,
            isOpen: true,
          },
        ],
      }),
    );
    expect(element().textContent).toContain('We need more information');
    expect(element().textContent).toContain('Please send the police report number');

    const answer = element().querySelector<HTMLTextAreaElement>('#answer-request-1')!;
    answer.value = 'Report no. 1234/2569';
    answer.dispatchEvent(new Event('input'));
    element().querySelector('article form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    await fixture.whenStable();

    const call = backend.expectOne('/api/claims/claim-1/information-requests/request-1/response');
    expect(call.request.body).toEqual({ response: 'Report no. 1234/2569' });
    call.flush(
      fakeClaim({
        status: 'UnderReview',
        informationRequests: [
          {
            id: 'request-1',
            message: 'Please send the police report number',
            dueDate: '2026-12-02',
            requestedAt: '2026-11-21T03:00:00Z',
            responseText: 'Report no. 1234/2569',
            respondedAt: '2026-11-21T05:00:00Z',
            isOpen: false,
          },
        ],
      }),
    );

    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(element().textContent).not.toContain('We need more information');
      expect(element().textContent).toContain('Information you provided');
    });
  });

  it('shows the decision and payment once paid', async () => {
    await render(
      fakeClaim({
        status: 'Paid',
        approvedAmount: 30_000_00,
        decisionReason: 'Deductible applied',
        decidedAt: '2026-11-22T03:00:00Z',
        paymentReference: 'TRF-20261125-0001',
        paidAt: '2026-11-23T03:00:00Z',
      }),
    );

    const decision = element().querySelector('[aria-labelledby="decision-heading"]')!;
    expect(decision.textContent).toContain('฿30,000.00');
    expect(decision.textContent).toContain('Deductible applied');
    expect(decision.textContent).toContain('TRF-20261125-0001');
  });
});
