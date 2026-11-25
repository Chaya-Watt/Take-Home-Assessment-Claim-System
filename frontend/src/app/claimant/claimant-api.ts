import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ClaimDetail, SubmitClaimRequest } from '../core/api/models';

/**
 * Claimant actions that change data (reads use `httpResource` in the pages).
 * Each action returns the updated claim so the page can show it without reloading.
 */
@Service()
export class ClaimantApi {
  private readonly http = inject(HttpClient);

  submit(request: SubmitClaimRequest): Promise<ClaimDetail> {
    return firstValueFrom(this.http.post<ClaimDetail>('/api/claims', request));
  }

  withdraw(claimId: string): Promise<ClaimDetail> {
    return firstValueFrom(this.http.post<ClaimDetail>(`/api/claims/${claimId}/withdraw`, null));
  }

  answerInformationRequest(claimId: string, requestId: string, response: string): Promise<ClaimDetail> {
    return firstValueFrom(
      this.http.post<ClaimDetail>(`/api/claims/${claimId}/information-requests/${requestId}/response`, { response }),
    );
  }
}
