# EduTrack — Student Management System

A role-based web application for managing student records, course enrolments, assessment results and
attendance. Built to demonstrate the full software development lifecycle: requirements, architecture,
relational design, security, automated testing and deployment.

**Stack:** React + TypeScript + Tailwind CSS · ASP.NET Core (.NET 10) Web API · Entity Framework Core · SQLite (development) / SQL Server (production) · xUnit · GitHub Actions

## Status

| Step | State |
| --- | --- |
| Foundation (repo, solution, app shell, CI) | Done |
| Database model and constraints | Written, needs first migration and a test run (see below) |
| Business rules (grading, marks, attendance) | Done, verified |
| Authentication and role-based access | Next |
| Student, enrolment, gradebook, attendance workflows | Planned |
| Dashboards, reports, end-to-end tests | Planned |
| Deployment and demo | Planned |

## Repository layout

```
backend/
  EduTrack.Domain/          Entities and enums (no dependencies)
  EduTrack.Application/     Business rules: grade calculation, mark validation, attendance
  EduTrack.Infrastructure/  EF Core DbContext, constraints, SQL Server wiring
  EduTrack.Api/             ASP.NET Core Web API
  EduTrack.Tests/           xUnit unit and database-constraint tests
frontend/                   React app (Vite)
tests/e2e/                  Playwright tests (planned)
docs/                       Proposal, requirements, architecture, test plan
```

Dependencies point inward: Api → Infrastructure → Application → Domain.

## Getting started

Prerequisites: .NET 10 SDK, Node 22+, and the EF tool (`dotnet tool install --global dotnet-ef --version 10.*`).
No database server or Docker is needed for development: the API uses a local SQLite file.

```bash
# 1. Backend (from backend/)
cd backend
dotnet restore
dotnet test

# 2. Create the first migration (once; commit the generated Migrations folder)
cd EduTrack.Api
dotnet ef migrations add InitialCreate --project ../EduTrack.Infrastructure --startup-project .

# 3. Run the API. In Development it creates/updates edutrack.dev.db automatically.
dotnet run --launch-profile http      # http://localhost:5080, health at /api/health

# 4. Frontend (from frontend/, in a second terminal)
cd ../../frontend
npm install
npm run dev                            # http://localhost:5173, /api is proxied to the API
```

To reset your local data, stop the API and delete `backend/EduTrack.Api/edutrack.dev.db*`.

### Choosing the database

`Database:Provider` selects the database. `appsettings.json` says `SqlServer`, so a deployment can never silently
fall back to SQLite; `appsettings.Development.json` switches development to `Sqlite`. The schema is applied
automatically only for SQLite in Development. SQL Server schema changes are always applied deliberately.

**SQLite vs SQL Server caveats**

- Migrations generated while running against SQLite contain SQLite-specific details (for example the `NOCASE`
  collation and SQLite column types), so they must **not** be applied to SQL Server. Before the first production
  deployment, generate a separate SQL Server migration set (we will set this up in the deployment step).
- EF Core stores `decimal` as text in SQLite, so `SUM`, `AVG` and `ORDER BY` on mark/weight columns can fail or
  misbehave there. Report queries should be tested against SQL Server before release.
- Text comparison is made case-insensitive on SQLite to match SQL Server, so unique checks behave the same.

## Design decisions

- **Grading is configurable.** `GradingPolicy` holds the pass mark and grade bands; nothing is hard-coded to one
  institution's scale. Weights are stored as percentage points and must total the policy's required weight.
- **Raw marks are stored**, never only the final grade, so any result can be recalculated and audited.
- **No cascading deletes.** Records are deactivated instead, preserving academic history.
- **Integrity lives in the database too:** unique student numbers, course codes, emails, one enrolment per
  student per offering, one attendance record per enrolment per session, one mark per student per assessment.
- **Authorisation is the API's job.** Hiding links in React is cosmetic; every endpoint will enforce role and
  record-level access.

## Data and privacy

Use fictional data only. A prototype with security controls is not automatically compliant with POPIA or any
other privacy law.
