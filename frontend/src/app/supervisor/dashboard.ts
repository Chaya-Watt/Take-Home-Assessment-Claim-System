import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ClaimStatus, SupervisorDashboard } from '../core/api/models';
import { errorMessage } from '../core/api/problem';
import { timeSince } from '../core/ui/duration';
import { ErrorAlert } from '../core/ui/error-alert';
import { claimStatusLabels, StatusBadge } from '../core/ui/status-badge';

const statuses = Object.keys(claimStatusLabels) as ClaimStatus[];

/**
 * Team overview for supervisors: where claims are stuck, what nobody has picked up,
 * and who is working on what. Every claim links to the staff claim page, where the supervisor can act.
 */
@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, DatePipe, StatusBadge, ErrorAlert],
  template: `
    <hgroup>
      <h1 tabindex="-1">Team dashboard</h1>
      @if (dashboard.hasValue()) {
        <p>Updated {{ dashboard.value().generatedAt | date: 'medium' }}</p>
      }
    </hgroup>
    <button
      type="button"
      class="secondary"
      [disabled]="dashboard.isLoading()"
      [attr.aria-busy]="dashboard.isLoading()"
      (click)="dashboard.reload()"
    >
      Refresh
    </button>

    @if (dashboard.error(); as error) {
      <app-error-alert [message]="errorMessage(error)" />
    } @else if (dashboard.hasValue()) {
      @let d = dashboard.value();

      <section aria-labelledby="by-status-heading">
        <h2 id="by-status-heading">Claims by status</h2>
        <div class="table-scroll">
          <table>
            <thead>
              <tr>
                @for (status of statuses; track status) {
                  <th scope="col"><app-status-badge [status]="status" /></th>
                }
              </tr>
            </thead>
            <tbody>
              <tr>
                @for (status of statuses; track status) {
                  <td>{{ d.claimsByStatus[status] ?? 0 }}</td>
                }
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section aria-labelledby="unassigned-heading">
        <h2 id="unassigned-heading">Waiting to be picked up ({{ d.unassignedCount }})</h2>
        @if (d.oldestUnassigned.length === 0) {
          <p>Every claim has been picked up.</p>
        } @else {
          @if (d.unassignedCount > d.oldestUnassigned.length) {
            <p>The {{ d.oldestUnassigned.length }} that have waited longest:</p>
          }
          <div class="table-scroll">
            <table>
              <thead>
                <tr>
                  <th scope="col">Claim</th>
                  <th scope="col">Submitted</th>
                  <th scope="col">Waiting for</th>
                </tr>
              </thead>
              <tbody>
                @for (claim of d.oldestUnassigned; track claim.id) {
                  <tr>
                    <td><a [routerLink]="['/staff/claims', claim.id]">{{ claim.claimNumber }}</a></td>
                    <td>{{ claim.submittedAt | date: 'medium' }}</td>
                    <td>{{ waitingFor(claim.submittedAt) }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </section>

      <section aria-labelledby="stuck-heading">
        <h2 id="stuck-heading">Open claims stuck longest in their current status</h2>
        @if (d.longestInCurrentStatus.length === 0) {
          <p>No open claims.</p>
        } @else {
          <div class="table-scroll">
            <table>
              <thead>
                <tr>
                  <th scope="col">Claim</th>
                  <th scope="col">Status</th>
                  <th scope="col">Handled by</th>
                  <th scope="col">In this status for</th>
                </tr>
              </thead>
              <tbody>
                @for (claim of d.longestInCurrentStatus; track claim.id) {
                  <tr>
                    <td><a [routerLink]="['/staff/claims', claim.id]">{{ claim.claimNumber }}</a></td>
                    <td><app-status-badge [status]="claim.status" /></td>
                    <td>{{ claim.assignedStaffName ?? 'Nobody yet' }}</td>
                    <td>{{ waitingFor(claim.inStatusSince) }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </section>

      <section aria-labelledby="workload-heading">
        <h2 id="workload-heading">Team workload</h2>
        @for (member of d.staff; track member.staffId) {
          <details [open]="member.openClaims > 0">
            <summary>
              {{ member.fullName }} ({{ member.role }}): {{ member.openClaims }} open
              {{ member.openClaims === 1 ? 'claim' : 'claims' }}
            </summary>
            @if (member.openClaims === 0) {
              <p>No open claims.</p>
            } @else {
              <p>
                @for (entry of byStatus(member.openClaimsByStatus); track entry.status) {
                  <app-status-badge [status]="entry.status" /> {{ entry.count }}&nbsp;
                }
              </p>
              <ul>
                @for (claim of member.claims; track claim.id) {
                  <li>
                    <a [routerLink]="['/staff/claims', claim.id]">{{ claim.claimNumber }}</a>
                    <app-status-badge [status]="claim.status" /> for {{ waitingFor(claim.inStatusSince) }}
                  </li>
                }
              </ul>
            }
          </details>
        }
      </section>
    } @else {
      <p aria-busy="true">Loading the dashboard…</p>
    }
  `,
})
export class Dashboard {
  protected readonly dashboard = httpResource<SupervisorDashboard>(() => '/api/supervisor/dashboard');

  /** Durations are measured against the time the server built the dashboard, not the browser clock. */
  private readonly generatedAt = computed(() => (this.dashboard.hasValue() ? this.dashboard.value().generatedAt : ''));

  protected readonly statuses = statuses;
  protected readonly errorMessage = errorMessage;

  protected waitingFor(since: string): string {
    return timeSince(since, this.generatedAt());
  }

  /** Status counts in the state machine's order, skipping statuses with no claims. */
  protected byStatus(counts: Record<string, number>): { status: ClaimStatus; count: number }[] {
    return statuses.filter((status) => counts[status]).map((status) => ({ status, count: counts[status] }));
  }
}
