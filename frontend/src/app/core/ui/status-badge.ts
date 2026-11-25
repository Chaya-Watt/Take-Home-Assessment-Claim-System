import { Component, computed, input } from '@angular/core';
import { ClaimStatus } from '../api/models';

export const claimStatusLabels: Record<ClaimStatus, string> = {
  Submitted: 'Submitted',
  UnderReview: 'Under review',
  PendingInformation: 'Waiting for information',
  Assessed: 'Assessed',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Paid: 'Paid',
  Withdrawn: 'Withdrawn',
};

/** A claim status as a small labelled pill (colour is decoration only; the text carries the meaning). */
@Component({
  selector: 'app-status-badge',
  template: `<span class="status-badge" [attr.data-status]="status()">{{ label() }}</span>`,
})
export class StatusBadge {
  readonly status = input.required<ClaimStatus>();
  protected readonly label = computed(() => claimStatusLabels[this.status()]);
}
