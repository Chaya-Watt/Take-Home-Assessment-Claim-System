# Claim System

Insurance claim system (take-home assessment). Two user groups: **Claimants** (report incidents, track claims, answer info requests, receive decisions) and **Claims staff** (pick up, assess, approve/reject, pay; supervisors also see team workload/performance).

- `backend/` — .NET 10 Web API, SQLite, Kafka. **Current focus.**
- `frontend/` — Angular 22. Not started; planned after the backend.
- `docs/backend-plan.md` — approved backend plan, domain model, and phase status. Read it before starting a phase.

## Commands (run from `backend/`)
```bash
dotnet build -warnaserror   # must be clean
dotnet test                 # Microsoft.Testing.Platform runner (set in global.json)
dotnet run --project src/ClaimSystem.Api   # http://localhost:5106 — /health, /scalar, /openapi/v1.json
```

## Backend structure (Clean Architecture, dependencies point inward)
```
src/ClaimSystem.Domain          entities, state machine, domain events — no dependencies
src/ClaimSystem.Application     use cases by feature + ports (interfaces); DI abstractions only, no ASP.NET/EF
src/ClaimSystem.Infrastructure  EF Core SQLite, JWT, Kafka, file storage — implements Application ports
src/ClaimSystem.Api             composition root + Minimal API endpoints
tests/ClaimSystem.IntegrationTests   WebApplicationFactory<Program>
```
Each layer exposes `AddApplication()` / `AddInfrastructure(config)` in its `DependencyInjection.cs`.

## Conventions
- **Minimal APIs** (not controllers): one file per feature, grouped with `MapGroup` + `RequireAuthorization`.
- Errors are ProblemDetails (`AddProblemDetails` + `UseExceptionHandler`).
- Money stored as `long` in satang (SQLite can't SUM/ORDER BY `decimal` server-side).
- Package versions live in each `.csproj`; `TreatWarningsAsErrors` is on via `Directory.Build.props`.
- Tests: xUnit v3, names like `Method_Scenario_Expected`; use `TestContext.Current.CancellationToken`.

## Working agreement
- Work **one phase at a time** (see `docs/backend-plan.md`). Each phase adds its own tests and must pass `dotnet build -warnaserror` + `dotnet test` before it is done; then stop for review.
- Keep setup minimal: add config/tooling/packages only in the phase that needs them.
- Edit files with editor tools so changes are visible as diffs; use the shell for build/test.
- Update the phase status table in `docs/backend-plan.md` when a phase completes.
