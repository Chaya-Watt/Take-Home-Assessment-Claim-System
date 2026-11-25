import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ClaimStatus, StaffClaimQueue, StaffClaimSummary, StaffClaimSummaryPage } from '../core/api/models';
import { errorMessage } from '../core/api/problem';
import { AuthStore } from '../core/auth/auth-store';
import { ErrorAlert } from '../core/ui/error-alert';
import { IncidentTypePipe } from '../core/ui/labels';
import { ThbPipe } from '../core/ui/money';
import { Pager } from '../core/ui/pager';
import { claimStatusLabels, StatusBadge } from '../core/ui/status-badge';

const pageSize = 10;

/** The generated type includes `null` (the query parameter is optional in the API). */
type QueueTab = NonNullable<StaffClaimQueue> | 'All';

const tabs: readonly { value: QueueTab; label: string }[] = [
  { value: 'Unassigned', label: 'Waiting to be picked up' },
  { value: 'Mine', label: 'My claims' },
  { value: 'All', label: 'All claims' },
];

/** Staff and supervisor work queue: unassigned claims, my claims, or everything. Newest first. */
@Component({
  selector: 'app-work-queue',
  imports: [RouterLink, DatePipe, ThbPipe, IncidentTypePipe, StatusBadge, ErrorAlert, Pager],
  template: `
    <h1 tabindex="-1">Work queue</h1>

    <div role="group" aria-label="Which claims">
      @for (tab of tabs; track tab.value) {
        <button
          type="button"
          [class.secondary]="queue() !== tab.value"
          [attr.aria-pressed]="queue() === tab.value"
          (click)="show(tab.value)"
        >
          {{ tab.label }}
        </button>
      }
    </div>

    <label for="queue-status">Status</label>
    <select id="queue-status" #statusSelect [value]="status()" (change)="filterBy(statusSelect.value)">
      <option value="">All statuses</option>
      @for (option of statusOptions; track option.value) {
        <option [value]="option.value">{{ option.label }}</option>
      }
    </select>

    @if (claims.error(); as error) {
      <app-error-alert [message]="errorMessage(error)" />
    } @else if (claims.hasValue()) {
      @let result = claims.value();
      @if (result.items.length === 0) {
        <p>No claims here.</p>
      } @else {
        <div class="table-scroll">
          <table>
            <caption class="visually-hidden">Claims, newest first</caption>
            <thead>
              <tr>
                <th scope="col">Claim</th>
                <th scope="col">Incident</th>
                <th scope="col">Claimed</th>
                <th scope="col">Status</th>
                <th scope="col">Handled by</th>
                <th scope="col">Submitted</th>
              </tr>
            </thead>
            <tbody>
              @for (claim of result.items; track claim.id) {
                <tr>
                  <td><a [routerLink]="['/staff/claims', claim.id]">{{ claim.claimNumber }}</a></td>
                  <td>{{ claim.incidentType | incidentType }}, {{ claim.incidentDate | date: 'mediumDate' }}</td>
                  <td>{{ claim.claimedAmount | thb }}</td>
                  <td><app-status-badge [status]="claim.status" /></td>
                  <td>{{ handledBy(claim) }}</td>
                  <td>{{ claim.submittedAt | date: 'medium' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
        <app-pager [(page)]="page" [pageSize]="pageSize" [totalCount]="result.totalCount" itemLabel="claims" />
      }
    } @else {
      <p aria-busy="true">Loading claims…</p>
    }
  `,
})
export class WorkQueue {
  private readonly auth = inject(AuthStore);

  protected readonly queue = signal<QueueTab>('Unassigned');
  protected readonly status = signal<ClaimStatus | ''>('');
  protected readonly page = signal(1);

  /** Re-fetches whenever the tab, status filter or page changes. */
  protected readonly claims = httpResource<StaffClaimSummaryPage>(() => ({
    url: '/api/staff/claims',
    params: {
      page: this.page(),
      pageSize,
      ...(this.queue() !== 'All' ? { queue: this.queue() } : {}),
      ...(this.status() ? { status: this.status() } : {}),
    },
  }));

  protected readonly tabs = tabs;
  protected readonly pageSize = pageSize;
  protected readonly statusOptions = Object.entries(claimStatusLabels).map(([value, label]) => ({ value, label }));
  protected readonly errorMessage = errorMessage;

  protected show(queue: QueueTab): void {
    this.queue.set(queue);
    this.page.set(1);
  }

  protected filterBy(status: string): void {
    this.status.set(status as ClaimStatus | '');
    this.page.set(1);
  }

  protected handledBy(claim: StaffClaimSummary): string {
    if (claim.assignedStaffId === null) {
      return 'Nobody yet';
    }
    return claim.assignedStaffId === this.auth.user()?.id ? 'You' : 'Another officer';
  }
}
