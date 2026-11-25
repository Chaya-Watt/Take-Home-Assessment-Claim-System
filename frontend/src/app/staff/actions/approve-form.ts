import { Component, computed, inject, input, linkedSignal, output } from '@angular/core';
import { form, FormField, FormRoot, min, required, validate } from '@angular/forms/signals';
import { StaffClaimDetail } from '../../core/api/models';
import { errorMessage } from '../../core/api/problem';
import { ErrorAlert } from '../../core/ui/error-alert';
import { ariaInvalid, FieldError } from '../../core/ui/field-error';
import { bahtToSatang, ThbPipe } from '../../core/ui/money';
import { StaffApi } from '../staff-api';

/** Approve the claim for an amount up to the assessed amount (e.g. lower after a deductible). */
@Component({
  selector: 'app-approve-form',
  imports: [FormRoot, FormField, ErrorAlert, FieldError, ThbPipe],
  template: `
    <form [formRoot]="approveForm">
      <label for="approve-amount">Approved amount (THB)</label>
      <input
        id="approve-amount"
        type="number"
        step="0.01"
        inputmode="decimal"
        [formField]="approveForm.approvedBaht"
        [attr.aria-invalid]="ariaInvalid(approveForm.approvedBaht())"
        aria-describedby="approve-amount-hint approve-amount-error"
      />
      <small id="approve-amount-hint">Assessed: {{ claim().assessedAmount | thb }}</small>
      <app-field-error errorId="approve-amount-error" [field]="approveForm.approvedBaht()" />

      <label for="approve-note">Note for the claimant (optional)</label>
      <textarea id="approve-note" rows="2" [formField]="approveForm.note"></textarea>

      <app-error-alert [message]="formError()" />
      <button type="submit" [disabled]="approveForm().submitting()" [attr.aria-busy]="approveForm().submitting()">
        Approve claim
      </button>
    </form>
  `,
})
export class ApproveForm {
  readonly claim = input.required<StaffClaimDetail>();
  readonly done = output<StaffClaimDetail>();

  private readonly api = inject(StaffApi);

  /** Starts at the assessed amount; resets if a different claim is shown. */
  private readonly model = linkedSignal(() => ({
    approvedBaht: (this.claim().assessedAmount ?? 0) / 100 as number | null,
    note: '',
  }));

  protected readonly approveForm = form(
    this.model,
    (path) => {
      required(path.approvedBaht, { message: 'Enter the approved amount.' });
      min(path.approvedBaht, 0.01, { message: 'The amount must be greater than zero.' });
      validate(path.approvedBaht, ({ value }) => {
        const amount = value();
        const assessed = this.claim().assessedAmount ?? 0;
        return amount !== null && bahtToSatang(amount) > assessed
          ? { kind: 'overAssessed', message: 'The approved amount cannot be more than the assessed amount.' }
          : undefined;
      });
    },
    {
      submission: {
        action: async (field) => {
          const { approvedBaht, note } = field().value();
          try {
            this.done.emit(
              await this.api.approve(this.claim().id, {
                approvedAmount: bahtToSatang(approvedBaht ?? 0),
                note: note.trim() || null,
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

  protected readonly formError = computed(() => this.approveForm().errors()[0]?.message);
  protected readonly ariaInvalid = ariaInvalid;
}
