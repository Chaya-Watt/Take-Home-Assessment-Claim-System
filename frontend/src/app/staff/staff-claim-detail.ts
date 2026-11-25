import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { StaffClaimDetail as StaffClaimDetailModel } from '../core/api/models';
import { errorMessage } from '../core/api/problem';
import { AuthStore } from '../core/auth/auth-store';
import { ClaimTimeline } from '../core/ui/claim-timeline';
import { ErrorAlert } from '../core/ui/error-alert';
import { IncidentTypePipe } from '../core/ui/labels';
import { ThbPipe } from '../core/ui/money';
import { StatusBadge } from '../core/ui/status-badge';
import { ApproveForm } from './actions/approve-form';
import { AssessForm } from './actions/assess-form';
import { PayForm } from './actions/pay-form';
import { RejectForm } from './actions/reject-form';
import { RequestInfoForm } from './actions/request-info-form';
import { availableActions, noActionReason } from './claim-actions';
import { StaffApi } from './staff-api';

/** Everything staff need about one claim, plus the actions allowed for its status and assignee. */
@Component({
  selector: 'app-staff-claim-detail',
  imports: [
    RouterLink,
    DatePipe,
    ThbPipe,
    IncidentTypePipe,
    StatusBadge,
    ErrorAlert,
    ClaimTimeline,
    RequestInfoForm,
    AssessForm,
    ApproveForm,
    RejectForm,
    PayForm,
  ],
  template: `
    <p><a routerLink="/staff/claims">← Work queue</a></p>

    @if (claim.error(); as error) {
      <h1 tabindex="-1">Claim</h1>
      <app-error-alert [message]="errorMessage(error)" />
    } @else if (claim.hasValue()) {
      @let c = claim.value();
      <h1 tabindex="-1">Claim {{ c.claimNumber }} <app-status-badge [status]="c.status" /></h1>
      <p>Handled by: <strong>{{ handledBy() }}</strong></p>

      <article aria-labelledby="actions-heading">
        <h2 id="actions-heading">Actions</h2>
        <app-error-alert [message]="actionError()" />

        @if (actions().length === 0) {
          <p>{{ noActionReason() }}</p>
        }
        @if (can('assign')) {
          <p>Nobody is handling this claim yet.</p>
          <button type="button" [disabled]="assigning()" [attr.aria-busy]="assigning()" (click)="assignToMe(c.id)">
            Pick up this claim
          </button>
        }
        @if (can('requestInformation')) {
          <details>
            <summary>Ask the claimant for more information</summary>
            <app-request-info-form [claim]="c" (done)="claim.set($event)" />
          </details>
        }
        @if (can('assess')) {
          <details>
            <summary>Record the assessment</summary>
            <app-assess-form [claim]="c" (done)="claim.set($event)" />
          </details>
        }
        @if (can('approve')) {
          <details open>
            <summary>Approve the claim</summary>
            <app-approve-form [claim]="c" (done)="claim.set($event)" />
          </details>
        }
        @if (can('reject')) {
          <details>
            <summary>Reject the claim</summary>
            <app-reject-form [claim]="c" (done)="claim.set($event)" />
          </details>
        }
        @if (can('pay')) {
          <details open>
            <summary>Record the payment</summary>
            <app-pay-form [claim]="c" (done)="claim.set($event)" />
          </details>
        }
      </article>

      <div class="grid">
        <section aria-labelledby="claimant-heading">
          <h2 id="claimant-heading">Claimant</h2>
          <dl>
            <dt>Name</dt>
            <dd>{{ c.claimant.fullName }}</dd>
            <dt>Email</dt>
            <dd>{{ c.claimant.email }}</dd>
          </dl>
        </section>
        <section aria-labelledby="policy-heading">
          <h2 id="policy-heading">Policy</h2>
          <dl>
            <dt>Number</dt>
            <dd>{{ c.policy.policyNumber }} ({{ c.policy.productType }})</dd>
            <dt>Sum insured</dt>
            <dd>{{ c.policy.sumInsured | thb }}</dd>
          </dl>
        </section>
      </div>

      <section aria-labelledby="incident-heading">
        <h2 id="incident-heading">Incident</h2>
        <dl>
          <dt>What happened</dt>
          <dd>{{ c.incidentType | incidentType }} on {{ c.incidentDate | date: 'mediumDate' : 'UTC' }}, {{ c.location }}</dd>
          <dt>Description</dt>
          <dd>{{ c.description }}</dd>
          <dt>Amount claimed</dt>
          <dd>{{ c.claimedAmount | thb }}</dd>
          <dt>Submitted</dt>
          <dd>{{ c.submittedAt | date: 'medium' }}</dd>
        </dl>
      </section>

      @if (c.assessedAmount !== null || c.decidedAt) {
        <section aria-labelledby="outcome-heading">
          <h2 id="outcome-heading">Assessment and decision</h2>
          <dl>
            @if (c.assessedAmount !== null) {
              <dt>Assessed amount</dt>
              <dd>{{ c.assessedAmount | thb }}</dd>
              <dt>Assessment summary</dt>
              <dd>{{ c.assessmentSummary }}</dd>
            }
            @if (c.decidedAt) {
              <dt>Decision</dt>
              <dd><app-status-badge [status]="c.status" /> on {{ c.decidedAt | date: 'medium' }}</dd>
            }
            @if (c.approvedAmount !== null) {
              <dt>Approved amount</dt>
              <dd>{{ c.approvedAmount | thb }}</dd>
            }
            @if (c.decisionReason) {
              <dt>{{ c.status === 'Rejected' ? 'Reason' : 'Note' }}</dt>
              <dd>{{ c.decisionReason }}</dd>
            }
            @if (c.paidAt) {
              <dt>Paid</dt>
              <dd>{{ c.paidAt | date: 'medium' }}, reference <strong>{{ c.paymentReference }}</strong></dd>
            }
          </dl>
        </section>
      }

      @if (c.informationRequests.length > 0) {
        <section aria-labelledby="requests-heading">
          <h2 id="requests-heading">Information requests</h2>
          @for (request of c.informationRequests; track request.id) {
            <p>
              <strong>{{ request.message }}</strong>
              <small>(asked {{ request.requestedAt | date: 'medium' }})</small><br />
              @if (request.isOpen) {
                <em>Waiting for the claimant's answer</em>
                @if (request.dueDate) {
                  <em>, due {{ request.dueDate | date: 'mediumDate' : 'UTC' }}</em>
                }
              } @else {
                {{ request.responseText }} <small>(answered {{ request.respondedAt | date: 'medium' }})</small>
              }
            </p>
          }
        </section>
      }

      <section aria-labelledby="timeline-heading">
        <h2 id="timeline-heading">Timeline</h2>
        <app-claim-timeline [entries]="c.timeline" />
      </section>
    } @else {
      <h1 tabindex="-1">Claim</h1>
      <p aria-busy="true">Loading claim…</p>
    }
  `,
})
export class StaffClaimDetail {
  /** Route parameter `:claimId`. */
  readonly claimId = input.required<string>();

  private readonly api = inject(StaffApi);
  private readonly auth = inject(AuthStore);

  protected readonly claim = httpResource<StaffClaimDetailModel>(() => `/api/staff/claims/${this.claimId()}`);

  private readonly userId = computed(() => this.auth.user()?.id ?? null);
  protected readonly actions = computed(() =>
    this.claim.hasValue() ? availableActions(this.claim.value(), this.userId()) : [],
  );
  protected readonly noActionReason = computed(() =>
    this.claim.hasValue() ? noActionReason(this.claim.value(), this.userId()) : '',
  );
  protected readonly handledBy = computed(() => {
    const assignee = this.claim.hasValue() ? this.claim.value().assignedStaffId : null;
    if (assignee === null) {
      return 'Nobody yet';
    }
    return assignee === this.userId() ? 'You' : 'Another claims officer';
  });

  protected readonly assigning = signal(false);
  protected readonly actionError = signal<string | null>(null);
  protected readonly errorMessage = errorMessage;

  protected can(action: ReturnType<typeof availableActions>[number]): boolean {
    return this.actions().includes(action);
  }

  protected async assignToMe(claimId: string): Promise<void> {
    this.assigning.set(true);
    this.actionError.set(null);
    try {
      this.claim.set(await this.api.assignToMe(claimId));
    } catch (error) {
      // Most likely someone else picked it up first (409): say so and show who has it now.
      this.actionError.set(errorMessage(error));
      this.claim.reload();
    } finally {
      this.assigning.set(false);
    }
  }
}
