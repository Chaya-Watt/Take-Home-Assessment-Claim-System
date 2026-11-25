# Claim System

Insurance claim system (take-home assessment). Two user groups: **Claimants** (report incidents, track claims, answer info requests, receive decisions) and **Claims staff** (pick up, assess, approve/reject, pay; supervisors also see team workload/performance).

- `backend/` — .NET 10 Web API, SQLite (Kafka notifications only if time permits). Done.
- `frontend/` — Angular 22. Done on `feat/frontend` (plan + manual test checklist in `docs/frontend-plan.md`). Angular rules and commands live in `frontend/CLAUDE.md`; the Angular CLI MCP server is configured in `.mcp.json`.
- `docs/backend-plan.md` — approved backend plan, domain model, and phase status. Read it before starting a phase.

## Commands (run from `backend/`)
```bash
dotnet build -warnaserror   # must be clean
dotnet test                 # Microsoft.Testing.Platform runner (set in global.json)
dotnet run --project src/ClaimSystem.Api   # http://localhost:5106 — /health, /scalar, /openapi/v1.json

# after changing an entity/configuration (dotnet-ef is installed globally)
dotnet ef migrations add <Name> --project src/ClaimSystem.Infrastructure --startup-project src/ClaimSystem.Api --output-dir Persistence/Migrations
```
In Development the app applies migrations and seeds demo data on startup. The DB is `src/ClaimSystem.Api/claims.db` (gitignored; delete it to reset); integration tests use the same file.
Seeded accounts (password `Password123!`): `claimant1@`, `claimant2@`, `staff1@`, `staff2@`, `supervisor@example.com`. Requests to try are in `src/ClaimSystem.Api/ClaimSystem.Api.http`.

## Backend structure (Clean Architecture, dependencies point inward)
```
src/ClaimSystem.Domain          entities, state machine, domain events — no dependencies
src/ClaimSystem.Application     use cases by feature + ports (interfaces); DI abstractions only, no ASP.NET/EF
src/ClaimSystem.Infrastructure  EF Core SQLite, JWT, Kafka — implements Application ports
src/ClaimSystem.Api             composition root + Minimal API endpoints
tests/ClaimSystem.UnitTests          domain rules (ClaimFixtures builds claims in any status)
tests/ClaimSystem.IntegrationTests   WebApplicationFactory<Program>, shared via [Collection(ApiCollection.Name)]; TestApi.CreateClientForAsync(email) logs in
```
Domain methods take `now` (UTC) as a parameter instead of reading the clock, and throw `DomainException` with a `Code` from `DomainErrors`.
Timestamps are stored and returned as UTC (the frontend converts for display); date rules use the Bangkok calendar via `BusinessCalendar.Today(now)`.
Each layer exposes `AddApplication()` / `AddInfrastructure(config)` in its `DependencyInjection.cs`.

## Conventions
- **Minimal APIs** (not controllers): one file per feature, grouped with `MapGroup` + `RequireAuthorization`.
- Errors are ProblemDetails (`AddProblemDetails` + `UseExceptionHandler`); `DomainExceptionHandler` maps `DomainErrors` codes to 400/403/404/409 and `ConcurrencyExceptionHandler` maps `DbUpdateConcurrencyException` to 409.
- Use cases are services in `Application/<Feature>` that talk to the DB only through ports (`I...Repository`, `IUnitOfWork`). Endpoints pass the caller's id (`User.GetUserId()`) into them.
- Auth: JWT with claims `sub`/`role` (`AuthClaims`); endpoint groups use `AuthorizationPolicies.ClaimantOnly` / `StaffOrSupervisor` / `SupervisorOnly`.
- Money stored as `long` in satang (SQLite can't SUM/ORDER BY `decimal` server-side).
- EF: one `IEntityTypeConfiguration` per entity; enums stored as text; Ids are `ValueGeneratedNever` (created in the domain). `Claim` has a `Version` shadow property as its concurrency token.
- Package versions live in each `.csproj`; `TreatWarningsAsErrors` is on via `Directory.Build.props`.
- Tests: xUnit v3, names like `Method_Scenario_Expected`; use `TestContext.Current.CancellationToken`.

## Working agreement
- Work **one phase at a time** (see `docs/backend-plan.md`). Each phase adds its own tests and must pass `dotnet build -warnaserror` + `dotnet test` before it is done; then stop for review.
- Keep setup minimal: add config/tooling/packages only in the phase that needs them.
- Keep scope to what the brief asks (it is a take-home): no file uploads, no statistics reports (only the supervisor dashboard). Kafka notifications are deferred to "if time permits" in the plan.
- Edit files with editor tools so changes are visible as diffs; use the shell for build/test.
- Update the phase status table in `docs/backend-plan.md` when a phase completes.
