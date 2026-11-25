import { ClaimDetail, PolicyResponse } from '../app/core/api/models';

/** Test-only builders shaped like API responses (excluded from the app build). */

export const motorPolicy: PolicyResponse = {
  id: 'policy-motor',
  policyNumber: 'POL-MTR-0001',
  productType: 'Motor',
  sumInsured: 500_000_00,
  startDate: '2026-01-01',
  endDate: '2027-12-31',
  incidentTypes: ['Collision', 'Theft', 'Flood', 'Glass'],
};

export const propertyPolicy: PolicyResponse = {
  id: 'policy-property',
  policyNumber: 'POL-PRP-0001',
  productType: 'Property',
  sumInsured: 3_000_000_00,
  startDate: '2026-01-01',
  endDate: '2027-12-31',
  incidentTypes: ['Fire', 'Flood', 'Burglary', 'WaterDamage'],
};

export function fakeClaim(overrides: Partial<ClaimDetail> = {}): ClaimDetail {
  return {
    id: 'claim-1',
    claimNumber: 'CLM-2026-000001',
    policyId: motorPolicy.id,
    policyNumber: motorPolicy.policyNumber,
    incidentType: 'Collision',
    incidentDate: '2026-11-20',
    location: 'Bangkok',
    description: 'Rear-ended at a red light',
    claimedAmount: 45_000_00,
    status: 'Submitted',
    submittedAt: '2026-11-20T03:00:00Z',
    approvedAmount: null,
    decisionReason: null,
    decidedAt: null,
    paymentReference: null,
    paidAt: null,
    timeline: [{ fromStatus: null, toStatus: 'Submitted', changedAt: '2026-11-20T03:00:00Z', note: null }],
    informationRequests: [],
    ...overrides,
  };
}
