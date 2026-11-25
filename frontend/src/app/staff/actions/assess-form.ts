import { Component, computed, inject, input, linkedSignal, output } from '@angular/core';
import { form, FormField, FormRoot, min, required, validate } from '@angular/forms/signals';
import { StaffClaimDetail } from '../../core/api/models';
import { errorMessage } from '../../core/api/problem';
import { ErrorAlert } from '../../core/ui/error-alert';
import { ariaInvalid, FieldError } from '../../core/ui/field-error';
import { bahtToSatang, ThbPipe } from '../../core/ui/money';
import { StaffApi } from '../staff-api';

/** Record how much the claim is worth after review (≤ the claimed amount). */
@Component({
  selector: 'app-assess-form',
  imports: [FormRoot, FormField, ErrorAlert, FieldError, ThbPipe],
  template: `
    <form [formRoot]="assessForm">
      <label for="assess-amount">Assessed amount (THB)</label>
      <input
        id="assess-amount"
        type="number"
        step="0.01"
        inputmode="decimal"
        [formField]="assessForm.assessedBaht"
        [attr.aria-invalid]="ariaInvalid(assessForm.assessedBaht())"
        aria-describedby="assess-amount-hint assess-amount-error"
      />
      <small id="assess-amount-hint">Claimed: {{ claim().claimedAmount | thb }}</small>
      <app-field-error errorId="assess-amount-error" [field]="assessForm.assessedBaht()" />

      <label for="assess-summary">Assessment summary</label>
      <textarea
        id="assess-summary"
        rows="3"
        [formField]="assessForm.summary"
        [attr.aria-invalid]="ariaInvalid(assessForm.summary())"
        aria-describedby="assess-summary-error"
      ></textarea>
      <app-field-error errorId="assess-summary-error" [field]="assessForm.summary()" />

      <app-error-alert [message]="formError()" />
      <button type="submit" [disabled]="assessForm().submitting()" [attr.aria-busy]="assessForm().submitting()">
        Save assessment
      </button>
    </form>
  `,
})
export class AssessForm {
  readonly claim = input.required<StaffClaimDetail>();
  readonly done = output<StaffClaimDetail>();

  private readonly api = inject(StaffApi);

  /** Starts at the claimed amount; resets if a different claim is shown. */
  private readonly model = linkedSignal(() => ({
    assessedBaht: this.claim().claimedAmount / 100 as number | null,
    summary: '',
  }));

  protected readonly assessForm = form(
    this.model,
    (path) => {
      required(path.assessedBaht, { message: 'Enter the assessed amount.' });
      min(path.assessedBaht, 0.01, { message: 'The amount must be greater than zero.' });
      validate(path.assessedBaht, ({ value }) => {
        const amount = value();
        return amount !== null && bahtToSatang(amount) > this.claim().claimedAmount
          ? { kind: 'overClaimed', message: 'The assessed amount cannot be more than the claimed amount.' }
          : undefined;
      });
      required(path.summary, { message: 'Summarise how you assessed the claim.' });
    },
    {
      submission: {
        action: async (field) => {
          const { assessedBaht, summary } = field().value();
          try {
            this.done.emit(
              await this.api.assess(this.claim().id, {
                assessedAmount: bahtToSatang(assessedBaht ?? 0),
                summary: summary.trim(),
              }),
            );
          } catch (error) {
            return { kind: 'server', message: errorMessage(error) };
          }
          return undefined;
        },
      },
    },
  );

  protected readonly formError = computed(() => this.assessForm().errors()[0]?.message);
  protected readonly ariaInvalid = ariaInvalid;
}
