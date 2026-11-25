# Take-Home-Assessment-Claim-System

Insurance claim system for two groups of users:

- **Claimants** report incidents, track their claims, answer requests for more information, and see the decision and payment.
- **Claims staff** pick up claims, assess them, approve or reject them, and record the payment. **Supervisors** can do everything staff can, plus see a team dashboard.

| Part | Stack | Status |
|---|---|---|
| `backend/` | .NET 10 Minimal API, EF Core + SQLite, JWT | Done |
| `frontend/` | Angular 22 | Not started |

## Run the backend

### 1. Prerequisites

Only the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) is needed. There's no Docker, no database server and no environment variables to set; SQLite is a file created automatically.

```bash
dotnet --version   # should print 10.0.x
```

### 2. Start the API

```bash
cd backend
dotnet run --project src/ClaimSystem.Api
```

On the first start it restores packages and builds, then:

- creates the database file `backend/src/ClaimSystem.Api/claims.db`
- applies the EF Core migrations
- seeds the [demo accounts](#demo-accounts) and their policies

It's ready when the console shows `Now listening on: http://localhost:5106`. Stop it with `Ctrl+C`.

From an IDE, open `backend/ClaimSystem.slnx` and run the `ClaimSystem.Api` project (`http` profile).

### 3. Run the tests (optional)

```bash
cd backend
dotnet build -warnaserror   # builds with warnings treated as errors
dotnet test                 # unit tests + integration tests (real HTTP calls)
```

Integration tests start the API in memory and use the same `claims.db`. They only add their own claims, so they can be run repeatedly.

### Reset the data

Stop the API, delete the database files, and start it again. It recreates and reseeds the database.

```bash
rm backend/src/ClaimSystem.Api/claims.db*
```

### Troubleshooting

| Problem | Fix |
|---|---|
| `dotnet` is not found or the version is below 10 | Install the .NET 10 SDK (link above) and open a new terminal |
| Port 5106 is already in use | Stop the other process, or run with `--urls http://localhost:5200` |
| Login fails with 401 for a demo account | Your `claims.db` is from an older build; reset the data as above |

### Useful URLs (while the API is running)

| URL | |
|---|---|
| http://localhost:5106/scalar | Interactive API docs |
| http://localhost:5106/openapi/v1.json | OpenAPI document |
| http://localhost:5106/health | Health check |
| [`ClaimSystem.Api.http`](backend/src/ClaimSystem.Api/ClaimSystem.Api.http) | Ready-made requests for the whole flow (VS Code REST Client / Rider / Visual Studio) |

### Demo accounts

All passwords are `Password123!`.

| Email | Role | Policies |
|---|---|---|
| `claimant1@example.com` | Claimant | `POL-MTR-0001` Motor, `POL-PRP-0001` Property |
| `claimant2@example.com` | Claimant | `POL-HLT-0001` Health |
| `staff1@example.com`, `staff2@example.com` | Staff | |
| `supervisor@example.com` | Supervisor | |

New claimants can register via `POST /api/auth/register`. Staff accounts cannot be created through the API.

### Try the whole flow

1. Log in as `claimant1` → `GET /api/policies/mine` → `POST /api/claims`.
2. Log in as `staff1` → `GET /api/staff/claims?queue=Unassigned` → `POST .../assign` → `POST .../information-requests`.
3. As `claimant1` → `POST /api/claims/{id}/information-requests/{requestId}/response`.
4. As `staff1` → `POST .../assessment` → `POST .../approve` → `POST .../pay`.
5. As `claimant1` → `GET /api/claims/{id}` shows status `Paid`, the approved amount, the payment reference and the timeline.
6. As `supervisor` → `GET /api/supervisor/dashboard`.

## Claim lifecycle

```
Submitted ──staff picks up──▶ UnderReview ──request info──▶ PendingInformation
    │                            ▲    │                            │
    │                            │    └──────claimant answers◀─────┘
    │                            │
    │                 UnderReview ──assess──▶ Assessed ──approve──▶ Approved ──pay──▶ Paid
    │                 UnderReview / Assessed ──reject (reason required)──▶ Rejected
    └──claimant withdraws──▶ Withdrawn
```

`Paid`, `Rejected` and `Withdrawn` are final.

Business rules (all enforced in the `Claim` aggregate and covered by unit tests):

- A claimant can only claim on **their own** policy, for an incident date within the policy period and not in the future.
- The incident type must match the policy's product (e.g. `Collision` for Motor, `Hospitalization` for Health).
- `claimed ≤ sum insured`, `assessed ≤ claimed`, `approved ≤ assessed`.
- Only the staff member who picked up the claim can act on it. Any staff member can view any claim.
- Each action is only allowed from specific statuses. Everything else is refused, and every status change is recorded in the claim's history.

## API overview

All endpoints except register/login need `Authorization: Bearer <token>`.
Amounts are integers in **satang** (1 THB = 100). Timestamps are **UTC** (ISO 8601 with `Z`); the client converts them for display.

| Who | Method & path | What it does |
|---|---|---|
| Anyone | `POST /api/auth/register` | Create a claimant account (returns a token) |
| | `POST /api/auth/login` | Get a JWT (60 min) |
| Signed in | `GET /api/auth/me` | Current user |
| Claimant | `GET /api/policies/mine` | My policies and the incident types each covers |
| | `POST /api/claims` | Report an incident |
| | `GET /api/claims/mine?status=&page=&pageSize=` | My claims, newest first |
| | `GET /api/claims/{id}` | Detail, timeline, information requests, decision, payment |
| | `POST /api/claims/{id}/withdraw` | Withdraw (only while `Submitted`) |
| | `POST /api/claims/{id}/information-requests/{requestId}/response` | Answer a staff question |
| Staff / Supervisor | `GET /api/staff/claims?queue=Unassigned\|Mine&status=&page=` | Work queue |
| | `GET /api/staff/claims/{id}` | Full detail incl. claimant, policy and assessment |
| | `POST /api/staff/claims/{id}/assign` | Pick up the claim |
| | `POST /api/staff/claims/{id}/information-requests` | Ask the claimant for more information |
| | `POST /api/staff/claims/{id}/assessment` | Record assessed amount and summary |
| | `POST /api/staff/claims/{id}/approve` · `/reject` · `/pay` | Decide, then record the payment reference |
| Supervisor | `GET /api/supervisor/dashboard` | Claims per status, unassigned claims, longest-waiting claims, each staff member's open claims |

Errors are [RFC 9457 ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457) with a machine-readable `code`:

| HTTP | `code` | When |
|---|---|---|
| 400 | `validation` | Input breaks a rule (amount, dates, incident type, empty text) |
| 401 | | Missing/invalid token, or wrong email/password |
| 403 | `not_allowed` | Wrong role, someone else's policy, or not the assigned staff member |
| 404 | `not_found` | Not found, **including another claimant's claim** (so its existence isn't revealed) |
| 409 | `claim.invalid_transition` | Action not allowed in the claim's current status |
| 409 | `conflict` / `concurrency_conflict` | Duplicate email / two people saved the same claim at the same time |

## Architecture

Clean Architecture, following Microsoft's [common web application architectures](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures) guidance; dependencies point inward.

```
backend/src/
  ClaimSystem.Domain           Claim aggregate (state machine + rules), Policy, User. No dependencies.
  ClaimSystem.Application      Use-case services per feature + ports (repositories, unit of work, token issuer).
  ClaimSystem.Infrastructure   EF Core/SQLite, migrations, seed, JWT, password hashing.
  ClaimSystem.Api              Composition root, Minimal API endpoint groups, error handling.
backend/tests/
  ClaimSystem.UnitTests        Domain rules, incl. every action × every disallowed status.
  ClaimSystem.IntegrationTests Real HTTP calls through WebApplicationFactory against SQLite.
```

A request flows **endpoint → service → domain method → save**. Endpoints read the user id from the token and pass it in. The domain decides whether the action is allowed and throws a `DomainException` with a code, which the API maps to the table above.

## Design decisions

- **Money as `long` satang.** It's exact, and SQLite can't `SUM`/`ORDER BY` `decimal` server-side.
- **UTC timestamps, Bangkok calendar for date rules.** "Today" (incident date not in the future, due dates, claim-number year) uses `Asia/Bangkok`, so a claim reported at 06:00 Thai time for the same day isn't rejected while UTC is still on the previous date.
- **Optimistic concurrency.** `Claim` has a `Version` column that changes on every save. If two staff pick up the same claim at once, one wins and the other gets `409` instead of silently overwriting.
- **Claim numbers** (`CLM-2026-000123`) come from a per-year counter, incremented in one atomic SQL statement, so concurrent submissions never get the same number. Numbers may skip but never repeat.
- **Self-registration always creates a Claimant.** A `role` field in the request is ignored.
- **The supervisor dashboard is computed live** from the claims and history tables, with no extra tables. Open claims are grouped in memory, which is fine at team scale and can move to SQL aggregates if needed.
- **Integration tests use the same `claims.db` as `dotnet run`.** This keeps setup to zero. Tests only assert on data they create, so they can be re-run; delete the file to reset.

## Out of scope / next steps

Kept out on purpose to match the brief:

- **File attachments.** Claimants answer information requests with text.
- **Statistics/performance reports.** The supervisor gets the operational dashboard only.
- **Real payments.** Staff record the reference of a transfer made outside the system.

**If there is time: notify claimants asynchronously via Kafka.** Today a claimant sees decisions and payments by opening the claim. The next step:

1. On every status change, publish `ClaimStatusChanged` to a Kafka topic `claims.events`, keyed by claim id so each claim's events stay in order.
2. A consumer creates notifications for the claimant: more information requested, approved/rejected, **paid (with the payment reference)**. Expose them via `GET /api/notifications`.
3. This doesn't need to be real-time. Decoupling means the claim API never waits on email/SMS/push, and new channels can be added without touching the claim flow.
4. To guarantee no event is lost if Kafka is down, write events to an **outbox table** in the same transaction as the claim and publish them from a background worker.

The detailed phase-by-phase plan and decisions are in [docs/backend-plan.md](docs/backend-plan.md) (Thai).
