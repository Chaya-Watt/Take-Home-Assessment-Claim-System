import { Component, computed, inject, input, output, signal } from '@angular/core';
import { form, FormField, FormRoot, required } from '@angular/forms/signals';
import { StaffClaimDetail } from '../../core/api/models';
import { errorMessage } from '../../core/api/problem';
import { ErrorAlert } from '../../core/ui/error-alert';
import { ariaInvalid, FieldError } from '../../core/ui/field-error';
import { StaffApi } from '../staff-api';

/** Reject the claim. The reason is shown to the claimant. */
@Component({
  selector: 'app-reject-form',
  imports: [FormRoot, FormField, ErrorAlert, FieldError],
  template: `
    <form [formRoot]="rejectForm">
      <label for="reject-reason">Reason (shown to the claimant)</label>
      <textarea
        id="reject-reason"
        rows="3"
        [formField]="rejectForm.reason"
        [attr.aria-invalid]="ariaInvalid(rejectForm.reason())"
        aria-describedby="reject-reason-error"
      ></textarea>
      <app-field-error errorId="reject-reason-error" [field]="rejectForm.reason()" />

      <app-error-alert [message]="formError()" />
      <button
        type="submit"
        class="contrast"
        [disabled]="rejectForm().submitting()"
        [attr.aria-busy]="rejectForm().submitting()"
      >
        Reject claim
      </button>
    </form>
  `,
})
export class RejectForm {
  readonly claim = input.required<StaffClaimDetail>();
  readonly done = output<StaffClaimDetail>();

  private readonly api = inject(StaffApi);
  private readonly model = signal({ reason: '' });

  protected readonly rejectForm = form(
    this.model,
    (path) => required(path.reason, { message: 'Explain why the claim is rejected.' }),
    {
      submission: {
        action: async (field) => {
          try {
            this.done.emit(await this.api.reject(this.claim().id, { reason: field().value().reason.trim() }));
          } catch (error) {
            return { kind: 'server', message: errorMessage(error) };
          }
          return undefined;
        },
      },
    },
  );

  protected readonly formError = computed(() => this.rejectForm().errors()[0]?.message);
  protected readonly ariaInvalid = ariaInvalid;
}
