import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { fakeClaim, motorPolicy, propertyPolicy } from '../../testing/claims';
import { NewClaim, toRequest } from './new-claim';

describe('NewClaim', () => {
  let fixture: ComponentFixture<NewClaim>;
  let backend: HttpTestingController;
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [NewClaim],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    backend = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(NewClaim);
    // httpResource keeps the app "unstable" until answered: send the request with tick(), flush, then wait.
    TestBed.tick();
    backend.expectOne('/api/policies/mine').flush([motorPolicy, propertyPolicy]);
    await fixture.whenStable();
  });

  const element = () => fixture.nativeElement as HTMLElement;
  const query = <T extends Element>(selector: string) => element().querySelector<T>(selector)!;
  const optionLabels = (selector: string) =>
    [...element().querySelectorAll(`${selector} option`)].map((o) => o.textContent?.trim()).slice(1); // skip "Choose…"

  async function set(selector: string, value: string): Promise<void> {
    const control = query<HTMLInputElement | HTMLSelectElement>(selector);
    control.value = value;
    control.dispatchEvent(new Event('input'));
    control.dispatchEvent(new Event('change'));
    await fixture.whenStable();
  }

  it('lists the incident types of the chosen policy only', async () => {
    expect(query<HTMLSelectElement>('#claim-type').disabled).toBe(true);

    await set('#claim-policy', motorPolicy.id);
    expect(optionLabels('#claim-type')).toEqual(['Collision', 'Theft', 'Flood', 'Glass']);

    await set('#claim-policy', propertyPolicy.id);
    expect(optionLabels('#claim-type')).toEqual(['Fire', 'Flood', 'Burglary', 'Water damage']);
  });

  it('submits the claim with the amount in satang and opens it', async () => {
    await set('#claim-policy', motorPolicy.id);
    await set('#claim-type', 'Collision');
    await set('#claim-date', '2026-11-20');
    await set('#claim-location', 'Sukhumvit Rd');
    await set('#claim-description', 'Rear-ended at a red light');
    await set('#claim-amount', '1234.56');

    query('form').dispatchEvent(new Event('submit', { cancelable: true }));
    await fixture.whenStable();

    const call = backend.expectOne({ method: 'POST', url: '/api/claims' });
    expect(call.request.body).toEqual({
      policyId: motorPolicy.id,
      incidentType: 'Collision',
      incidentDate: '2026-11-20',
      location: 'Sukhumvit Rd',
      description: 'Rear-ended at a red light',
      claimedAmount: 123_456,
    });
    call.flush(fakeClaim({ id: 'claim-42' }));

    await vi.waitFor(() => expect(navigate).toHaveBeenCalledWith(['/claims', 'claim-42']));
  });

  it('blocks an amount above the sum insured before calling the API', async () => {
    await set('#claim-policy', motorPolicy.id); // sum insured 500,000 THB
    await set('#claim-amount', '500000.01');

    query('form').dispatchEvent(new Event('submit', { cancelable: true }));
    await fixture.whenStable();

    expect(query('#claim-amount-error').textContent).toContain('more than the sum insured');
    backend.expectNone('/api/claims');
  });

  it('shows the error returned by the backend', async () => {
    await set('#claim-policy', motorPolicy.id);
    await set('#claim-type', 'Theft');
    await set('#claim-location', 'Bangkok');
    await set('#claim-description', 'Car stolen');
    await set('#claim-amount', '100');

    query('form').dispatchEvent(new Event('submit', { cancelable: true }));
    await fixture.whenStable();
    backend
      .expectOne('/api/claims')
      .flush(
        { status: 400, detail: 'Policy POL-MTR-0001 does not cover incidents on 2025-12-31.' },
        { status: 400, statusText: 'Bad Request' },
      );

    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(query('[role="alert"]').textContent).toContain('does not cover incidents');
    });
    expect(navigate).not.toHaveBeenCalled();
  });
});

describe('toRequest', () => {
  it('converts baht to satang and trims text', () => {
    expect(
      toRequest({
        policyId: 'p',
        incidentType: 'Glass',
        incidentDate: '2026-11-20',
        location: '  Bangkok ',
        description: ' Cracked windscreen ',
        claimedBaht: 19.99,
      }),
    ).toEqual({
      policyId: 'p',
      incidentType: 'Glass',
      incidentDate: '2026-11-20',
      location: 'Bangkok',
      description: 'Cracked windscreen',
      claimedAmount: 1999,
    });
  });
});
