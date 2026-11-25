import { HttpErrorResponse } from '@angular/common/http';
import { errorCode, errorMessage } from './problem';

describe('errorMessage / errorCode', () => {
  it('uses the ProblemDetails detail and code from the backend', () => {
    const error = new HttpErrorResponse({
      status: 409,
      error: { title: 'Conflict', detail: 'Cannot approve a claim that is Paid.', code: 'claim.invalid_transition' },
    });

    expect(errorMessage(error)).toBe('Cannot approve a claim that is Paid.');
    expect(errorCode(error)).toBe('claim.invalid_transition');
  });

  it('falls back to the title when there is no detail', () => {
    expect(errorMessage(new HttpErrorResponse({ status: 403, error: { title: 'Forbidden' } }))).toBe('Forbidden');
  });

  it('explains when the backend cannot be reached', () => {
    expect(errorMessage(new HttpErrorResponse({ status: 0 }))).toContain('Cannot reach the server');
  });

  it('handles non-HTTP errors', () => {
    expect(errorMessage(new Error('boom'))).toBe('Something went wrong. Please try again.');
    expect(errorCode(new Error('boom'))).toBeUndefined();
  });
});
