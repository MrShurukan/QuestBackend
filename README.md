# QuestBackend

Backend for the Enigma quest game.

## Stack

- `.NET 10`
- `ASP.NET Core 10`
- `EF Core 10`
- `PostgreSQL`

## Quick Start

1. Restore local tools:

```powershell
dotnet tool restore
```

2. Copy `.env.example` to `.env`.
3. Start PostgreSQL:

```powershell
docker compose up -d
```

4. Apply migrations:

```powershell
dotnet ef database update --project src/QuestBackend.Infrastructure --startup-project src/QuestBackend.Api
```

5. Restore and build:

```powershell
dotnet restore
dotnet build QuestBackend.sln
```

6. Run the API:

```powershell
dotnet run --project src/QuestBackend.Api
```

## Development Seed

To auto-seed a small sample configuration on startup, set:

```powershell
$env:Bootstrap__SeedSampleData="true"
dotnet run --project src/QuestBackend.Api
```

The dev seed creates:

- sample tags
- sample questions
- sample pools
- sample QR codes
- default routing profile
- default enigma profile

## Test Suite

```powershell
dotnet test QuestBackend.sln
```

## Runbook

Operational notes live in `docs/runbook.md`.

## Solution

- `src/QuestBackend.Api`
- `src/QuestBackend.Application`
- `src/QuestBackend.Domain`
- `src/QuestBackend.Infrastructure`
- `src/QuestBackend.Contracts`
- `tests/QuestBackend.UnitTests`
- `tests/QuestBackend.IntegrationTests`
- `tests/QuestBackend.ArchitectureTests`
