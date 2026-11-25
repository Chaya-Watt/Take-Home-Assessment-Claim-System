import { ClaimStatus } from '../core/api/models';
import { availableActions, noActionReason, StaffAction } from './claim-actions';

describe('availableActions', () => {
  const me = 'staff-me';
  const someoneElse = 'staff-other';

  it.each<[ClaimStatus, StaffAction[]]>([
    ['UnderReview', ['requestInformation', 'assess', 'reject']],
    ['PendingInformation', []], // waiting for the claimant
    ['Assessed', ['approve', 'reject']],
    ['Approved', ['pay']],
    ['Paid', []],
    ['Rejected', []],
  ])('assigned to me, %s → %j', (status, expected) => {
    expect(availableActions({ status, assignedStaffId: me }, me)).toEqual(expected);
  });

  it('anyone can pick up a submitted claim', () => {
    expect(availableActions({ status: 'Submitted', assignedStaffId: null }, me)).toEqual(['assign']);
  });

  it.each<ClaimStatus>(['UnderReview', 'Assessed', 'Approved'])(
    'nothing to do on a %s claim handled by someone else',
    (status) => {
      expect(availableActions({ status, assignedStaffId: someoneElse }, me)).toEqual([]);
      expect(noActionReason({ status, assignedStaffId: someoneElse }, me)).toContain('Another claims officer');
    },
  );

  it('explains why there is nothing to do', () => {
    expect(noActionReason({ status: 'Paid', assignedStaffId: me }, me)).toContain('closed');
    expect(noActionReason({ status: 'Withdrawn', assignedStaffId: null }, me)).toContain('closed');
    expect(noActionReason({ status: 'PendingInformation', assignedStaffId: me }, me)).toContain('Waiting for the claimant');
  });
});
