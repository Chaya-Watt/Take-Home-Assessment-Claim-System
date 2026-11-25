import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { fakeStaffClaim } from '../../testing/claims';
import { StaffApi } from './staff-api';

describe('StaffApi', () => {
  let api: StaffApi;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(StaffApi);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it.each([
    ['assignToMe', () => api.assignToMe('claim-1'), 'assign', null],
    [
      'requestInformation',
      () => api.requestInformation('claim-1', { message: 'Photos', dueDate: null }),
      'information-requests',
      { message: 'Photos', dueDate: null },
    ],
    ['assess', () => api.assess('claim-1', { assessedAmount: 100, summary: 'OK' }), 'assessment', { assessedAmount: 100, summary: 'OK' }],
    ['approve', () => api.approve('claim-1', { approvedAmount: 90, note: null }), 'approve', { approvedAmount: 90, note: null }],
    ['reject', () => api.reject('claim-1', { reason: 'Not covered' }), 'reject', { reason: 'Not covered' }],
    ['pay', () => api.pay('claim-1', { paymentReference: 'TRF-1' }), 'pay', { paymentReference: 'TRF-1' }],
  ] as const)('%s posts to /api/staff/claims/{id}/%s', async (_name, call, path, body) => {
    const result = call();

    const request = backend.expectOne({ method: 'POST', url: `/api/staff/claims/claim-1/${path}` });
    expect(request.request.body).toEqual(body);
    request.flush(fakeStaffClaim({ status: 'UnderReview' }));

    expect((await result).status).toBe('UnderReview');
  });
});
