import { Component, computed, inject, input, output, signal } from '@angular/core';
import { form, FormField, FormRoot, required } from '@angular/forms/signals';
import { ClaimDetail, InformationRequest } from '../core/api/models';
import { errorMessage } from '../core/api/problem';
import { ErrorAlert } from '../core/ui/error-alert';
import { ariaInvalid, FieldError } from '../core/ui/field-error';
import { ClaimantApi } from './claimant-api';

/** The claimant's answer to an open information request. Emits the updated claim once sent. */
@Component({
  selector: 'app-answer-request',
  imports: [FormRoot, FormField, ErrorAlert, FieldError],
  template: `
    <form [formRoot]="answerForm">
      <label [for]="inputId()">Your answer</label>
      <textarea
        [id]="inputId()"
        rows="3"
        [formField]="answerForm.response"
        [attr.aria-invalid]="ariaInvalid(answerForm.response())"
        [attr.aria-describedby]="inputId() + '-error'"
      ></textarea>
      <app-field-error [errorId]="inputId() + '-error'" [field]="answerForm.response()" />

      <app-error-alert [message]="formError()" />
      <button type="submit" [disabled]="answerForm().submitting()" [attr.aria-busy]="answerForm().submitting()">
        Send answer
      </button>
    </form>
  `,
})
export class AnswerRequest {
  readonly claimId = input.required<string>();
  readonly request = input.required<InformationRequest>();
  readonly answered = output<ClaimDetail>();

  private readonly api = inject(ClaimantApi);

  protected readonly inputId = computed(() => `answer-${this.request().id}`);

  private readonly model = signal({ response: '' });
  protected readonly answerForm = form(
    this.model,
    (path) => required(path.response, { message: 'Write an answer before sending.' }),
    {
      submission: {
        action: async (field) => {
          try {
            const claim = await this.api.answerInformationRequest(
              this.claimId(),
              this.request().id,
              field().value().response.trim(),
            );
            this.answered.emit(claim);
          } catch (error) {
            return { kind: 'server', message: errorMessage(error) };
          }
          return undefined;
        },
      },
    },
  );

  protected readonly formError = computed(() => this.answerForm().errors()[0]?.message);
  protected readonly ariaInvalid = ariaInvalid;
}
