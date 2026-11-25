import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ClaimStatus, ClaimSummaryPage } from '../core/api/models';
import { errorMessage } from '../core/api/problem';
import { ErrorAlert } from '../core/ui/error-alert';
import { IncidentTypePipe } from '../core/ui/labels';
import { ThbPipe } from '../core/ui/money';
import { Pager } from '../core/ui/pager';
import { claimStatusLabels, StatusBadge } from '../core/ui/status-badge';

const pageSize = 10;

@Component({
  selector: 'app-my-claims',
  imports: [RouterLink, DatePipe, ThbPipe, IncidentTypePipe, StatusBadge, ErrorAlert, Pager],
  template: `
    <hgroup>
      <h1 tabindex="-1">My claims</h1>
      <p><a routerLink="/claims/new">Report an incident</a></p>
    </hgroup>

    <label for="status-filter">Status</label>
    <select id="status-filter" #statusSelect [value]="status()" (change)="filterBy(statusSelect.value)">
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
        <p>No claims found. <a routerLink="/claims/new">Report an incident</a> to start a claim.</p>
      } @else {
        <div class="table-scroll">
          <table>
            <caption class="visually-hidden">Your claims, newest first</caption>
            <thead>
              <tr>
                <th scope="col">Claim</th>
                <th scope="col">Incident</th>
                <th scope="col">Claimed</th>
                <th scope="col">Status</th>
                <th scope="col">Submitted</th>
              </tr>
            </thead>
            <tbody>
              @for (claim of result.items; track claim.id) {
                <tr>
                  <td><a [routerLink]="['/claims', claim.id]">{{ claim.claimNumber }}</a></td>
                  <td>{{ claim.incidentType | incidentType }}, {{ claim.incidentDate | date: 'mediumDate' }}</td>
                  <td>{{ claim.claimedAmount | thb }}</td>
                  <td><app-status-badge [status]="claim.status" /></td>
                  <td>{{ claim.submittedAt | date: 'medium' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <app-pager [(page)]="page" [pageSize]="pageSize" [totalCount]="result.totalCount" itemLabel="claims" />
      }
    } @else {
      <p aria-busy="true">Loading your claims…</p>
    }
  `,
})
export class MyClaims {
  protected readonly status = signal<ClaimStatus | ''>('');
  protected readonly page = signal(1);

  /** Re-fetches whenever the status filter or the page changes. */
  protected readonly claims = httpResource<ClaimSummaryPage>(() => ({
    url: '/api/claims/mine',
    params: { page: this.page(), pageSize, ...(this.status() ? { status: this.status() } : {}) },
  }));

  protected readonly pageSize = pageSize;
  protected readonly statusOptions = Object.entries(claimStatusLabels).map(([value, label]) => ({ value, label }));
  protected readonly errorMessage = errorMessage;

  protected filterBy(status: string): void {
    this.status.set(status as ClaimStatus | '');
    this.page.set(1);
  }
}
