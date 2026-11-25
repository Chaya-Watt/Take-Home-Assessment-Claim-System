import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  ApproveClaimRequest,
  AssessClaimRequest,
  PayClaimRequest,
  RejectClaimRequest,
  RequestInformationRequest,
  StaffClaimDetail,
} from '../core/api/models';

/**
 * Staff (and supervisor) actions on a claim. Each returns the updated claim so the page can show it
 * without reloading. Reads use `httpResource` in the pages.
 */
@Service()
export class StaffApi {
  private readonly http = inject(HttpClient);

  assignToMe(claimId: string): Promise<StaffClaimDetail> {
    return this.post(claimId, 'assign', null);
  }

  requestInformation(claimId: string, request: RequestInformationRequest): Promise<StaffClaimDetail> {
    return this.post(claimId, 'information-requests', request);
  }

  assess(claimId: string, request: AssessClaimRequest): Promise<StaffClaimDetail> {
    return this.post(claimId, 'assessment', request);
  }

  approve(claimId: string, request: ApproveClaimRequest): Promise<StaffClaimDetail> {
    return this.post(claimId, 'approve', request);
  }

  reject(claimId: string, request: RejectClaimRequest): Promise<StaffClaimDetail> {
    return this.post(claimId, 'reject', request);
  }

  pay(claimId: string, request: PayClaimRequest): Promise<StaffClaimDetail> {
    return this.post(claimId, 'pay', request);
  }

  private post(claimId: string, action: string, body: unknown): Promise<StaffClaimDetail> {
    return firstValueFrom(this.http.post<StaffClaimDetail>(`/api/staff/claims/${claimId}/${action}`, body));
  }
}
