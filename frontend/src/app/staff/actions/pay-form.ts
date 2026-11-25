import { Component, computed, inject, input, output, signal } from '@angular/core';
import { form, FormField, FormRoot, required } from '@angular/forms/signals';
import { StaffClaimDetail } from '../../core/api/models';
import { errorMessage } from '../../core/api/problem';
import { ErrorAlert } from '../../core/ui/error-alert';
import { ariaInvalid, FieldError } from '../../core/ui/field-error';
import { ThbPipe } from '../../core/ui/money';
import { StaffApi } from '../staff-api';

/** Record the payment made outside the system (bank transfer) by its reference. */
@Component({
  selector: 'app-pay-form',
  imports: [FormRoot, FormField, ErrorAlert, FieldError, ThbPipe],
  template: `
    <form [formRoot]="payForm">
      <p>Pay <strong>{{ claim().approvedAmount | thb }}</strong> to the claimant, then record the transfer reference.</p>
      <label for="pay-reference">Payment reference</label>
      <input
        id="pay-reference"
        [formField]="payForm.paymentReference"
        [attr.aria-invalid]="ariaInvalid(payForm.paymentReference())"
        aria-describedby="pay-reference-error"
      />
      <app-field-error errorId="pay-reference-error" [field]="payForm.paymentReference()" />

      <app-error-alert [message]="formError()" />
      <button type="submit" [disabled]="payForm().submitting()" [attr.aria-busy]="payForm().submitting()">
        Record payment
      </button>
    </form>
  `,
})
export class PayForm {
  readonly claim = input.required<StaffClaimDetail>();
  readonly done = output<StaffClaimDetail>();

  private readonly api = inject(StaffApi);
  private readonly model = signal({ paymentReference: '' });

  protected readonly payForm = form(
    this.model,
    (path) => required(path.paymentReference, { message: 'Enter the payment reference.' }),
    {
      submission: {
        action: async (field) => {
          try {
            this.done.emit(
              await this.api.pay(this.claim().id, { paymentReference: field().value().paymentReference.trim() }),
            );
          } catch (error) {
            return { kind: 'server', message: errorMessage(error) };
          }
          return undefined;
        },
      },
    },
  );

  protected readonly formError = computed(() => this.payForm().errors()[0]?.message);
  protected readonly ariaInvalid = ariaInvalid;
}
