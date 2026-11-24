# Take-Home-Assessment-Claim-System

Insurance claim system for **Claimants** and **Claims staff**.

| Part | Stack | Status |
|---|---|---|
| `backend/` | .NET 10 Minimal API, SQLite, Kafka | In progress — see [docs/backend-plan.md](docs/backend-plan.md) |
| `frontend/` | Angular 22 | Not started |

## Run the backend
Requires the .NET 10 SDK.

```bash
cd backend
dotnet build
dotnet test
dotnet run --project src/ClaimSystem.Api
```

- Health: http://localhost:5106/health
- API docs (Scalar): http://localhost:5106/scalar
