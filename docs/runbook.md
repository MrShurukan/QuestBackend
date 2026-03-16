# QuestBackend Runbook

## Local Environment

1. Restore tools:

```powershell
dotnet tool restore
```

2. Start PostgreSQL:

```powershell
docker compose up -d
```

3. Apply migrations:

```powershell
dotnet ef database update --project src/QuestBackend.Infrastructure --startup-project src/QuestBackend.Api
```

4. Start API:

```powershell
dotnet run --project src/QuestBackend.Api
```

## Default Admin

By default the bootstrap admin is:

- login: `admin`
- password: `admin123`

Override via configuration:

- `Bootstrap:Admin:Login`
- `Bootstrap:Admin:Password`

## Sample Data

For manual frontend or API verification, enable sample seed:

- `Bootstrap:SeedSampleData=true`

The seed is idempotent for an empty database and creates a small playable configuration.

## Common Commands

Build:

```powershell
dotnet build QuestBackend.sln
```

Tests:

```powershell
dotnet test QuestBackend.sln
```

Create migration:

```powershell
dotnet ef migrations add <Name> --project src/QuestBackend.Infrastructure --startup-project src/QuestBackend.Api --output-dir Persistence/Migrations
```

Apply migration:

```powershell
dotnet ef database update --project src/QuestBackend.Infrastructure --startup-project src/QuestBackend.Api
```

## Operational Notes

- `Start quest` and `Finish day` are server-side lifecycle operations and gate scans, answers and enigma attempts.
- Public QR entrypoint is `/q/{slug}`.
- Backend is the source of truth for cooldowns and timestamps.
- Support corrections and configuration changes are written to audit storage.

## Test Coverage

The repository includes:

- unit tests for answer evaluation, routing, lifecycle, cooldown and enigma evaluation
- architecture tests for layer dependency rules
- integration tests for auth, teams, QR flow, cooldown, rewards, lifecycle blocking, routing overrides and support actions
