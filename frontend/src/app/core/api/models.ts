import type { components } from './schema';

/**
 * Short names for the API types generated from the backend's OpenAPI document (schema.d.ts).
 * Never write DTOs by hand: run `npm run gen:api` after backend changes and fix what `ng build` reports.
 */
type Schemas = components['schemas'];

// Auth
export type LoginRequest = Schemas['LoginRequest'];
export type RegisterRequest = Schemas['RegisterRequest'];
export type AuthResponse = Schemas['AuthResponse'];
export type UserResponse = Schemas['UserResponse'];
export type UserRole = Schemas['UserRole'];

// Policies
export type PolicyResponse = Schemas['PolicyResponse'];
export type ProductType = Schemas['ProductType'];

// Claims (claimant)
export type ClaimStatus = Schemas['ClaimStatus'];
export type IncidentType = Schemas['IncidentType'];
export type SubmitClaimRequest = Schemas['SubmitClaimRequest'];
export type ClaimSummary = Schemas['ClaimSummaryResponse'];
export type ClaimDetail = Schemas['ClaimDetailResponse'];
export type ClaimSummaryPage = Schemas['PagedResultOfClaimSummaryResponse'];
export type TimelineEntry = Schemas['TimelineEntryResponse'];
export type InformationRequest = Schemas['InformationRequestResponse'];
export type InformationResponseRequest = Schemas['InformationResponseRequest'];

// Claims (staff)
export type StaffClaimQueue = Schemas['StaffClaimQueue'];
export type StaffClaimSummary = Schemas['StaffClaimSummaryResponse'];
export type StaffClaimSummaryPage = Schemas['PagedResultOfStaffClaimSummaryResponse'];
export type StaffClaimDetail = Schemas['StaffClaimDetailResponse'];
export type RequestInformationRequest = Schemas['RequestInformationRequest'];
export type AssessClaimRequest = Schemas['AssessClaimRequest'];
export type ApproveClaimRequest = Schemas['ApproveClaimRequest'];
export type RejectClaimRequest = Schemas['RejectClaimRequest'];
export type PayClaimRequest = Schemas['PayClaimRequest'];

// Supervisor
export type SupervisorDashboard = Schemas['SupervisorDashboardResponse'];
export type DashboardClaim = Schemas['DashboardClaim'];
export type StaffWorkload = Schemas['StaffWorkload'];
