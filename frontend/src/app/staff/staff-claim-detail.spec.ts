import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { StaffClaimDetail as StaffClaimDetailModel, UserRole } from '../core/api/models';
import { fakeStaffClaim } from '../../testing/claims';
import { fakeSession, storeSession } from '../../testing/sessions';
import { StaffClaimDetail } from './staff-claim-detail';

describe('StaffClaimDetail', () => {
  let fixture: ComponentFixture<StaffClaimDetail>;
  let backend: HttpTestingController;

  async function render(claim: StaffClaimDetailModel, role: UserRole = 'Staff'): Promise<void> {
    localStorage.clear();
    storeSession(fakeSession(role)); // user id "id-Staff" / "id-Supervisor"
    TestBed.configureTestingModule({
      imports: [StaffClaimDetail],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    backend = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(StaffClaimDetail);
    fixture.componentRef.setInput('claimId', claim.id);
    TestBed.tick(); // httpResource: send the request, flush, then wait
    backend.expectOne(`/api/staff/claims/${claim.id}`).flush(claim);
    await fixture.whenStable();
  }

  const element = () => fixture.nativeElement as HTMLElement;
  const actions = () => element().querySelector('[aria-labelledby="actions-heading"]')!;
  const summaries = () => [...actions().querySelectorAll('summary')].map((s) => s.textContent?.trim());
  const button = (label: string) =>
    [...element().querySelectorAll('button')].find((b) => b.textContent?.trim() === label);

  it('shows the claimant, policy and incident', async () => {
    await render(fakeStaffClaim());

    expect(element().querySelector('h1')?.textContent).toContain('Claim CLM-2026-000001');
    expect(element().textContent).toContain('Somchai Jaidee');
    expect(element().textContent).toContain('POL-MTR-0001');
    expect(element().textContent).toContain('฿40,000.00');
  });

  it('picks up a submitted claim and then offers the review actions', async () => {
    await render(fakeStaffClaim({ status: 'Submitted' }));

    button('Pick up this claim')!.click();
    backend
      .expectOne({ method: 'POST', url: '/api/staff/claims/claim-1/assign' })
      .flush(fakeStaffClaim({ status: 'UnderReview', assignedStaffId: 'id-Staff' }));

    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(summaries()).toEqual([
        'Ask the claimant for more information',
        'Record the assessment',
        'Reject the claim',
      ]);
    });
    expect(element().textContent).toContain('Handled by: You');
  });

  it('tells the officer when someone else picked the claim up first, and reloads it', async () => {
    await render(fakeStaffClaim({ status: 'Submitted' }));

    button('Pick up this claim')!.click();
    backend.expectOne('/api/staff/claims/claim-1/assign').flush(
      { status: 409, detail: 'Cannot assign a claim that is UnderReview.', code: 'claim.invalid_transition' },
      { status: 409, statusText: 'Conflict' },
    );

    await vi.waitFor(() => backend.expectOne('/api/staff/claims/claim-1').flush(
      fakeStaffClaim({ status: 'UnderReview', assignedStaffId: 'id-other' }),
    ));
    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(actions().querySelector('[role="alert"]')?.textContent).toContain('Cannot assign a claim that is UnderReview.');
      expect(actions().textContent).toContain('Another claims officer is handling this claim');
    });
    expect(button('Pick up this claim')).toBeUndefined();
  });

  it('approves for the amount typed in baht, sent in satang', async () => {
    await render(
      fakeStaffClaim({ status: 'Assessed', assignedStaffId: 'id-Staff', assessedAmount: 35_000_00, assessmentSummary: 'OK' }),
    );
    expect(summaries()).toEqual(['Approve the claim', 'Reject the claim']);
    const amount = element().querySelector<HTMLInputElement>('#approve-amount')!;
    expect(amount.value).toBe('35000'); // pre-filled with the assessed amount

    amount.value = '30000.50';
    amount.dispatchEvent(new Event('input'));
    const note = element().querySelector<HTMLTextAreaElement>('#approve-note')!;
    note.value = 'Deductible applied';
    note.dispatchEvent(new Event('input'));
    amount.closest('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    await fixture.whenStable();

    const request = backend.expectOne({ method: 'POST', url: '/api/staff/claims/claim-1/approve' });
    expect(request.request.body).toEqual({ approvedAmount: 3_000_050, note: 'Deductible applied' });
    request.flush(fakeStaffClaim({ status: 'Approved', assignedStaffId: 'id-Staff', approvedAmount: 3_000_050 }));

    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(summaries()).toEqual(['Record the payment']);
    });
  });

  it('does not let an approval exceed the assessed amount', async () => {
    await render(fakeStaffClaim({ status: 'Assessed', assignedStaffId: 'id-Staff', assessedAmount: 35_000_00 }));
    const amount = element().querySelector<HTMLInputElement>('#approve-amount')!;

    amount.value = '35000.01';
    amount.dispatchEvent(new Event('input'));
    amount.closest('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    await fixture.whenStable();

    expect(element().querySelector('#approve-amount-error')?.textContent).toContain('cannot be more than the assessed amount');
    backend.expectNone('/api/staff/claims/claim-1/approve');
  });

  it('lets a supervisor act on a claim they picked up', async () => {
    await render(fakeStaffClaim({ status: 'Approved', assignedStaffId: 'id-Supervisor', approvedAmount: 30_000_00 }), 'Supervisor');

    expect(summaries()).toEqual(['Record the payment']);
  });

  it('shows no actions on a closed claim', async () => {
    await render(fakeStaffClaim({ status: 'Paid', assignedStaffId: 'id-Staff', paymentReference: 'TRF-1', paidAt: '2026-11-23T03:00:00Z' }));

    expect(summaries()).toEqual([]);
    expect(actions().textContent).toContain('This claim is closed');
  });
});
