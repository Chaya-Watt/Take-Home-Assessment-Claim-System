import { httpResource } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { disabled, form, FormField, FormRoot, min, required, validate } from '@angular/forms/signals';
import { Router, RouterLink } from '@angular/router';
import { IncidentType, PolicyResponse, SubmitClaimRequest } from '../core/api/models';
import { errorMessage } from '../core/api/problem';
import { ErrorAlert } from '../core/ui/error-alert';
import { ariaInvalid, FieldError } from '../core/ui/field-error';
import { IncidentTypePipe, todayIsoDate } from '../core/ui/labels';
import { bahtToSatang, ThbPipe } from '../core/ui/money';
import { ClaimantApi } from './claimant-api';

interface NewClaimModel {
  policyId: string;
  incidentType: IncidentType | '';
  incidentDate: string;
  location: string;
  description: string;
  /** Typed in baht; converted to satang when sent. */
  claimedBaht: number | null;
}

@Component({
  selector: 'app-new-claim',
  imports: [FormRoot, FormField, RouterLink, ErrorAlert, FieldError, IncidentTypePipe, ThbPipe],
  template: `
    <h1 tabindex="-1">Report an incident</h1>

    @if (policies.error(); as error) {
      <app-error-alert [message]="errorMessage(error)" />
    } @else if (!policies.hasValue()) {
      <p aria-busy="true">Loading your policies…</p>
    } @else if (policies.value().length === 0) {
      <p>You have no policies to claim against.</p>
    } @else {
      <form [formRoot]="claimForm">
        <label for="claim-policy">Policy</label>
        <select
          id="claim-policy"
          [formField]="claimForm.policyId"
          (change)="claimForm.incidentType().value.set('')"
          [attr.aria-invalid]="ariaInvalid(claimForm.policyId())"
          aria-describedby="claim-policy-error"
        >
          <option value="">Choose a policy…</option>
          @for (policy of policies.value(); track policy.id) {
            <option [value]="policy.id">
              {{ policy.policyNumber }} — {{ policy.productType }} (sum insured {{ policy.sumInsured | thb }})
            </option>
          }
        </select>
        <app-field-error errorId="claim-policy-error" [field]="claimForm.policyId()" />

        <label for="claim-type">What happened?</label>
        <select
          id="claim-type"
          [formField]="claimForm.incidentType"
          [attr.aria-invalid]="ariaInvalid(claimForm.incidentType())"
          aria-describedby="claim-type-error claim-type-hint"
        >
          <option value="">Choose an incident type…</option>
          @for (type of incidentTypes(); track type) {
            <option [value]="type">{{ type | incidentType }}</option>
          }
        </select>
        <small id="claim-type-hint">Only the incident types covered by the chosen policy are listed.</small>
        <app-field-error errorId="claim-type-error" [field]="claimForm.incidentType()" />

        <label for="claim-date">Date of the incident</label>
        <input
          id="claim-date"
          type="date"
          [formField]="claimForm.incidentDate"
          [attr.aria-invalid]="ariaInvalid(claimForm.incidentDate())"
          aria-describedby="claim-date-error"
        />
        <app-field-error errorId="claim-date-error" [field]="claimForm.incidentDate()" />

        <label for="claim-location">Location</label>
        <input
          id="claim-location"
          [formField]="claimForm.location"
          [attr.aria-invalid]="ariaInvalid(claimForm.location())"
          aria-describedby="claim-location-error"
        />
        <app-field-error errorId="claim-location-error" [field]="claimForm.location()" />

        <label for="claim-description">What happened, in your own words</label>
        <textarea
          id="claim-description"
          rows="4"
          [formField]="claimForm.description"
          [attr.aria-invalid]="ariaInvalid(claimForm.description())"
          aria-describedby="claim-description-error"
        ></textarea>
        <app-field-error errorId="claim-description-error" [field]="claimForm.description()" />

        <label for="claim-amount">Amount claimed (THB)</label>
        <input
          id="claim-amount"
          type="number"
          step="0.01"
          inputmode="decimal"
          [formField]="claimForm.claimedBaht"
          [attr.aria-invalid]="ariaInvalid(claimForm.claimedBaht())"
          aria-describedby="claim-amount-error"
        />
        <app-field-error errorId="claim-amount-error" [field]="claimForm.claimedBaht()" />

        <app-error-alert [message]="formError()" />
        <div class="actions">
          <button type="submit" [disabled]="claimForm().submitting()" [attr.aria-busy]="claimForm().submitting()">
            Submit claim
          </button>
          <a routerLink="/claims" class="secondary">Cancel</a>
        </div>
      </form>
    }
  `,
})
export class NewClaim {
  private readonly api = inject(ClaimantApi);
  private readonly router = inject(Router);

  protected readonly today = todayIsoDate();
  protected readonly policies = httpResource<PolicyResponse[]>(() => '/api/policies/mine');

  private readonly model = signal<NewClaimModel>({
    policyId: '',
    incidentType: '',
    incidentDate: this.today,
    location: '',
    description: '',
    claimedBaht: null,
  });

  protected readonly selectedPolicy = computed(() =>
    this.policies.hasValue() ? this.policies.value().find((p) => p.id === this.model().policyId) : undefined,
  );

  /** The incident type list follows the chosen policy (Motor → Collision, Theft, …). */
  protected readonly incidentTypes = computed(() => this.selectedPolicy()?.incidentTypes ?? []);

  // Client-side checks mirror the backend's rules for quick feedback; the backend still decides.
  protected readonly claimForm = form(
    this.model,
    (path) => {
      required(path.policyId, { message: 'Choose the policy you are claiming on.' });
      disabled(path.incidentType, { when: ({ valueOf }) => !valueOf(path.policyId) });
      required(path.incidentType, { message: 'Choose what happened.' });
      required(path.incidentDate, { message: 'Enter the date of the incident.' });
      validate(path.incidentDate, ({ value }) =>
        value() > this.today
          ? { kind: 'future', message: 'The incident date cannot be in the future.' }
          : undefined,
      );
      required(path.location, { message: 'Enter where it happened.' });
      required(path.description, { message: 'Describe what happened.' });
      required(path.claimedBaht, { message: 'Enter the amount you are claiming.' });
      min(path.claimedBaht, 0.01, { message: 'The amount must be greater than zero.' });
      validate(path.claimedBaht, ({ value }) => {
        const sumInsured = this.selectedPolicy()?.sumInsured;
        const amount = value();
        return sumInsured !== undefined && amount !== null && bahtToSatang(amount) > sumInsured
          ? { kind: 'overSumInsured', message: 'The amount cannot be more than the sum insured.' }
          : undefined;
      });
    },
    {
      submission: {
        action: async (field) => {
          try {
            const claim = await this.api.submit(toRequest(field().value()));
            await this.router.navigate(['/claims', claim.id]);
          } catch (error) {
            return { kind: 'server', message: errorMessage(error) };
          }
          return undefined;
        },
      },
    },
  );

  protected readonly formError = computed(() => this.claimForm().errors()[0]?.message);
  protected readonly ariaInvalid = ariaInvalid;
  protected readonly errorMessage = errorMessage;
}

/** Form values (validated) → API request; baht becomes satang. */
export function toRequest(value: NewClaimModel): SubmitClaimRequest {
  return {
    policyId: value.policyId,
    incidentType: value.incidentType as IncidentType,
    incidentDate: value.incidentDate,
    location: value.location.trim(),
    description: value.description.trim(),
    claimedAmount: bahtToSatang(value.claimedBaht ?? 0),
  };
}
