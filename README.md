# EduTrack — Student Management System

A role-based web application for managing student records, course enrolments, assessment results and
attendance. Built to demonstrate the full software development lifecycle: requirements, architecture,
relational design, security, automated testing and deployment.

**Stack:** React + TypeScript + Tailwind CSS · ASP.NET Core (.NET 10) Web API · Entity Framework Core · SQLite (development) / SQL Server (production) · xUnit · GitHub Actions

## Status

| Step | State |
| --- | --- |
| Foundation (repo, solution, app shell, CI) | Done |
| Database model and constraints | Written; needs your first migration and test run (see below) |
| Business rules (grading, marks, attendance) | Done, verified |
| Authentication and role-based access | Done: API, tests and login UI (see Verification below) |
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

### Demo accounts (local development only)

On first start, with an empty SQLite database, the API creates three fictional accounts, all with the password
`Dev-Passw0rd!`: `admin@edutrack.test`, `lecturer@edutrack.test`, `student@edutrack.test`. The login page shows
shortcut buttons for them in development builds only. They are never created outside Development.

### How sign-in works

- The API issues an **HttpOnly session cookie** (`edutrack.auth`). JavaScript cannot read it and nothing is kept in
  browser storage; the app asks `GET /api/auth/me` who is signed in.
- Every endpoint requires sign-in unless it opts out (`[AllowAnonymous]`), and role policies
  (Administrator, Lecturer, Student, Staff) decide the rest. Hiding links in React is cosmetic only.
- The account is re-checked on every request, so deactivating a user ends their session immediately.
- Failed sign-ins are indistinguishable (unknown email, wrong password, deactivated), and sign-in is rate limited
  (`Auth:LoginAttemptsPerMinute`, default 10 per client address).
- State-changing requests must send `X-Requested-With: EduTrack` (a CSRF defence); the frontend client does this.

**Known limitations**

- Logging out clears the browser's cookie but does not revoke it on the server: a copied cookie keeps working until it
  expires (8 hours). Fix before release by adding a per-user security stamp that is checked on every request and
  changed on logout and password change.
- There is no account lockout beyond the rate limit, and no password-change or password-reset flow yet.

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

## Verification

Run on the build machine, which had .NET 8 but no NuGet access, so the full solution could not be compiled:

| Check | Result |
| --- | --- |
| Real `AuthController`, auth extensions, middleware, `AuthService` compiled in a scratch host (in-memory user store) | Builds with warnings as errors |
| 22 HTTP checks against that host: CSRF header, cookie flags, generic 401s, role matrix, deactivation, rate limit, `Secure` cookie in Production | All pass |
| Real `GradeCalculatorTests`, `AttendanceCalculatorTests`, `AuthServiceTests` (33 tests) via a minimal xUnit stand-in | All pass |
| 24 browser checks (real React app and that host): redirects, role navigation, session restore, sign-out, access denied, revoked session, mobile layout | All pass |
| Frontend `npm run lint` and `npm run build` | Pass |
| **Not run:** EF Core store, seeder, `DatabaseConstraintTests`, `AuthEndpointsTests`, `LoginRateLimitTests` | Need `dotnet test` on a machine with NuGet |
