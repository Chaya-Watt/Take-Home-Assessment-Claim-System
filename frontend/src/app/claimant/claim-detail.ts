import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ClaimDetail as ClaimDetailModel } from '../core/api/models';
import { errorMessage } from '../core/api/problem';
import { ClaimTimeline } from '../core/ui/claim-timeline';
import { ErrorAlert } from '../core/ui/error-alert';
import { IncidentTypePipe } from '../core/ui/labels';
import { ThbPipe } from '../core/ui/money';
import { StatusBadge } from '../core/ui/status-badge';
import { AnswerRequest } from './answer-request';
import { ClaimantApi } from './claimant-api';

@Component({
  selector: 'app-claim-detail',
  imports: [RouterLink, DatePipe, ThbPipe, IncidentTypePipe, StatusBadge, ErrorAlert, ClaimTimeline, AnswerRequest],
  template: `
    <p><a routerLink="/claims">← My claims</a></p>

    @if (claim.error(); as error) {
      <h1 tabindex="-1">Claim</h1>
      <app-error-alert [message]="errorMessage(error)" />
    } @else if (claim.hasValue()) {
      @let c = claim.value();
      <h1 tabindex="-1">Claim {{ c.claimNumber }} <app-status-badge [status]="c.status" /></h1>

      <app-error-alert [message]="actionError()" />

      @if (openRequest(); as request) {
        <article aria-labelledby="open-request-heading">
          <h2 id="open-request-heading">We need more information</h2>
          <p>{{ request.message }}</p>
          @if (request.dueDate) {
            <p>Please answer by {{ request.dueDate | date: 'mediumDate' : 'UTC' }}.</p>
          }
          <app-answer-request [claimId]="c.id" [request]="request" (answered)="claim.set($event)" />
        </article>
      }

      @if (c.decidedAt) {
        <article aria-labelledby="decision-heading">
          <h2 id="decision-heading">Decision</h2>
          <dl>
            <dt>Result</dt>
            <dd><app-status-badge [status]="c.status" /> on {{ c.decidedAt | date: 'medium' }}</dd>
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
              <dd>{{ c.paidAt | date: 'medium' }} — payment reference <strong>{{ c.paymentReference }}</strong></dd>
            }
          </dl>
        </article>
      }

      <section aria-labelledby="details-heading">
        <h2 id="details-heading">Details</h2>
        <dl>
          <dt>Policy</dt>
          <dd>{{ c.policyNumber }}</dd>
          <dt>Incident</dt>
          <dd>{{ c.incidentType | incidentType }} on {{ c.incidentDate | date: 'mediumDate' : 'UTC' }}</dd>
          <dt>Location</dt>
          <dd>{{ c.location }}</dd>
          <dt>Description</dt>
          <dd>{{ c.description }}</dd>
          <dt>Amount claimed</dt>
          <dd>{{ c.claimedAmount | thb }}</dd>
          <dt>Submitted</dt>
          <dd>{{ c.submittedAt | date: 'medium' }}</dd>
        </dl>
      </section>

      @if (answeredRequests().length > 0) {
        <section aria-labelledby="answered-heading">
          <h2 id="answered-heading">Information you provided</h2>
          @for (request of answeredRequests(); track request.id) {
            <p>
              <strong>{{ request.message }}</strong><br />
              {{ request.responseText }} <small>({{ request.respondedAt | date: 'medium' }})</small>
            </p>
          }
        </section>
      }

      <section aria-labelledby="timeline-heading">
        <h2 id="timeline-heading">Timeline</h2>
        <app-claim-timeline [entries]="c.timeline" />
      </section>

      @if (c.status === 'Submitted') {
        <section aria-labelledby="withdraw-heading">
          <h2 id="withdraw-heading">Withdraw this claim</h2>
          <p>You can withdraw until a claims officer picks it up. This cannot be undone.</p>
          @if (confirmingWithdraw()) {
            <div class="actions">
              <button type="button" class="contrast" [attr.aria-busy]="withdrawing()" [disabled]="withdrawing()" (click)="withdraw(c.id)">
                Yes, withdraw claim
              </button>
              <button type="button" class="secondary" (click)="confirmingWithdraw.set(false)">Keep my claim</button>
            </div>
          } @else {
            <button type="button" class="secondary outline" (click)="confirmingWithdraw.set(true)">Withdraw claim</button>
          }
        </section>
      }
    } @else {
      <h1 tabindex="-1">Claim</h1>
      <p aria-busy="true">Loading claim…</p>
    }
  `,
})
export class ClaimDetail {
  /** Route parameter `:claimId`. */
  readonly claimId = input.required<string>();

  private readonly api = inject(ClaimantApi);

  protected readonly claim = httpResource<ClaimDetailModel>(() => `/api/claims/${this.claimId()}`);

  protected readonly openRequest = computed(() =>
    this.claim.hasValue() ? this.claim.value().informationRequests.find((r) => r.isOpen) : undefined,
  );
  protected readonly answeredRequests = computed(() =>
    this.claim.hasValue() ? this.claim.value().informationRequests.filter((r) => !r.isOpen) : [],
  );

  protected readonly confirmingWithdraw = signal(false);
  protected readonly withdrawing = signal(false);
  protected readonly actionError = signal<string | null>(null);
  protected readonly errorMessage = errorMessage;

  protected async withdraw(claimId: string): Promise<void> {
    this.withdrawing.set(true);
    this.actionError.set(null);
    try {
      this.claim.set(await this.api.withdraw(claimId));
      this.confirmingWithdraw.set(false);
    } catch (error) {
      this.actionError.set(errorMessage(error));
      this.claim.reload(); // e.g. a staff member picked it up meanwhile: show the current status
    } finally {
      this.withdrawing.set(false);
    }
  }
}
