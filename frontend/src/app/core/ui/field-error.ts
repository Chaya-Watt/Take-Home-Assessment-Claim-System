import { Component, computed, input } from '@angular/core';

/** The part of a Signal Forms field state this component needs (works for any field type). */
interface FieldStatus {
  touched(): boolean;
  invalid(): boolean;
  errors(): readonly { message?: string }[];
}

/**
 * Validation message under an input, shown once the user has touched the field.
 * Point the input's `aria-describedby` at `errorId` so screen readers read the message.
 */
@Component({
  selector: 'app-field-error',
  template: `<small [id]="errorId()">{{ message() }}</small>`,
})
export class FieldError {
  readonly field = input.required<FieldStatus>();
  readonly errorId = input.required<string>();

  protected readonly message = computed(() => {
    const field = this.field();
    return field.touched() && field.invalid() ? (field.errors()[0]?.message ?? 'Invalid value.') : '';
  });
}

/** `aria-invalid` value for an input: only after the user touched it, so an empty form doesn't start red. */
export function ariaInvalid(field: FieldStatus): 'true' | null {
  return field.touched() && field.invalid() ? 'true' : null;
}
