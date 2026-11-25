import { Component, computed, inject, input, output, signal } from '@angular/core';
import { form, FormField, FormRoot, required, validate } from '@angular/forms/signals';
import { StaffClaimDetail } from '../../core/api/models';
import { errorMessage } from '../../core/api/problem';
import { ErrorAlert } from '../../core/ui/error-alert';
import { ariaInvalid, FieldError } from '../../core/ui/field-error';
import { todayIsoDate } from '../../core/ui/labels';
import { StaffApi } from '../staff-api';

/** Ask the claimant for more information; the claim waits in "PendingInformation" until they answer. */
@Component({
  selector: 'app-request-info-form',
  imports: [FormRoot, FormField, ErrorAlert, FieldError],
  template: `
    <form [formRoot]="requestForm">
      <label for="request-message">Question for the claimant</label>
      <textarea
        id="request-message"
        rows="3"
        [formField]="requestForm.message"
        [attr.aria-invalid]="ariaInvalid(requestForm.message())"
        aria-describedby="request-message-error"
      ></textarea>
      <app-field-error errorId="request-message-error" [field]="requestForm.message()" />

      <label for="request-due">Answer by (optional)</label>
      <input
        id="request-due"
        type="date"
        [formField]="requestForm.dueDate"
        [attr.aria-invalid]="ariaInvalid(requestForm.dueDate())"
        aria-describedby="request-due-error"
      />
      <app-field-error errorId="request-due-error" [field]="requestForm.dueDate()" />

      <app-error-alert [message]="formError()" />
      <button type="submit" [disabled]="requestForm().submitting()" [attr.aria-busy]="requestForm().submitting()">
        Send request
      </button>
    </form>
  `,
})
export class RequestInfoForm {
  readonly claim = input.required<StaffClaimDetail>();
  readonly done = output<StaffClaimDetail>();

  private readonly api = inject(StaffApi);
  private readonly model = signal({ message: '', dueDate: '' });

  protected readonly requestForm = form(
    this.model,
    (path) => {
      required(path.message, { message: 'Write what you need from the claimant.' });
      validate(path.dueDate, ({ value }) =>
        value() && value() < todayIsoDate()
          ? { kind: 'past', message: 'The due date cannot be in the past.' }
          : undefined,
      );
    },
    {
      submission: {
        action: async (field) => {
          const { message, dueDate } = field().value();
          try {
            this.done.emit(
              await this.api.requestInformation(this.claim().id, { message: message.trim(), dueDate: dueDate || null }),
            );
          } catch (error) {
            return { kind: 'server', message: errorMessage(error) };
          }
          return undefined;
        },
      },
    },
  );

  protected readonly formError = computed(() => this.requestForm().errors()[0]?.message);
  protected readonly ariaInvalid = ariaInvalid;
}
