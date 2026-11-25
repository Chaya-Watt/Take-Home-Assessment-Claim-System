import { HttpErrorResponse } from '@angular/common/http';

/** RFC 9457 error body returned by the backend for every failed request. */
interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  code?: string;
}

function problemOf(error: unknown): ProblemDetails | null {
  if (error instanceof HttpErrorResponse && typeof error.error === 'object' && error.error !== null) {
    return error.error as ProblemDetails;
  }
  return null;
}

/** A message that can be shown to the user for any error thrown by an API call. */
export function errorMessage(error: unknown): string {
  const problem = problemOf(error);
  if (problem?.detail || problem?.title) {
    return problem.detail ?? problem.title ?? '';
  }
  if (error instanceof HttpErrorResponse) {
    return error.status === 0
      ? 'Cannot reach the server. Is the backend running?'
      : `Request failed (${error.status}).`;
  }
  return 'Something went wrong. Please try again.';
}

/** The backend's machine-readable error code (e.g. "claim.invalid_transition"), if any. */
export function errorCode(error: unknown): string | undefined {
  return problemOf(error)?.code;
}
