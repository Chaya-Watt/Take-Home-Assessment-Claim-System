import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { fakeClaim, motorPolicy } from '../../testing/claims';
import { ClaimantApi } from './claimant-api';

describe('ClaimantApi', () => {
  let api: ClaimantApi;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(ClaimantApi);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('submits a claim', async () => {
    const request = {
      policyId: motorPolicy.id,
      incidentType: 'Collision' as const,
      incidentDate: '2026-11-20',
      location: 'Bangkok',
      description: 'Rear-ended',
      claimedAmount: 45_000_00,
    };

    const result = api.submit(request);
    const call = backend.expectOne({ method: 'POST', url: '/api/claims' });
    expect(call.request.body).toEqual(request);
    call.flush(fakeClaim());

    expect((await result).claimNumber).toBe('CLM-2026-000001');
  });

  it('withdraws a claim', async () => {
    const result = api.withdraw('claim-1');
    backend.expectOne({ method: 'POST', url: '/api/claims/claim-1/withdraw' }).flush(fakeClaim({ status: 'Withdrawn' }));

    expect((await result).status).toBe('Withdrawn');
  });

  it('answers an information request', async () => {
    const result = api.answerInformationRequest('claim-1', 'request-1', 'Police report no. 1234');
    const call = backend.expectOne({
      method: 'POST',
      url: '/api/claims/claim-1/information-requests/request-1/response',
    });
    expect(call.request.body).toEqual({ response: 'Police report no. 1234' });
    call.flush(fakeClaim({ status: 'UnderReview' }));

    expect((await result).status).toBe('UnderReview');
  });
});
