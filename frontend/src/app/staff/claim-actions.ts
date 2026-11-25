import { StaffClaimDetail } from '../core/api/models';

export type StaffAction = 'assign' | 'requestInformation' | 'assess' | 'approve' | 'reject' | 'pay';

type ClaimState = Pick<StaffClaimDetail, 'status' | 'assignedStaffId'>;

/**
 * What the signed-in staff member (or supervisor) can do with a claim right now.
 * Mirrors the backend state machine; the backend still enforces it.
 */
export function availableActions(claim: ClaimState, userId: string | null): StaffAction[] {
  if (claim.status === 'Submitted') {
    return ['assign']; // anyone can pick up an unassigned claim
  }
  if (!userId || claim.assignedStaffId !== userId) {
    return []; // only the staff member who picked it up can act on it
  }
  switch (claim.status) {
    case 'UnderReview':
      return ['requestInformation', 'assess', 'reject'];
    case 'Assessed':
      return ['approve', 'reject'];
    case 'Approved':
      return ['pay'];
    default:
      return []; // PendingInformation waits for the claimant; Paid / Rejected / Withdrawn are final
  }
}

/** Why there is nothing to do, shown instead of the action buttons. */
export function noActionReason(claim: ClaimState, userId: string | null): string {
  if (claim.status === 'Paid' || claim.status === 'Rejected' || claim.status === 'Withdrawn') {
    return 'This claim is closed. No further actions are possible.';
  }
  if (claim.assignedStaffId !== userId) {
    return 'Another claims officer is handling this claim. Only they can act on it.';
  }
  if (claim.status === 'PendingInformation') {
    return 'Waiting for the claimant to answer your information request.';
  }
  return 'No actions are available.';
}
