# Deployment notes

Work in progress; completed in the deployment step.

## Configuration

Secrets are never committed. The API reads configuration from environment variables or user-secrets:

| Setting | Variable | Notes |
| --- | --- | --- |
| Database provider | `Database__Provider` | `SqlServer` in production (the default in `appsettings.json`). `Sqlite` is for local development only. |
| Database | `ConnectionStrings__DefaultConnection` | Required. The API refuses to start without it. |
| Frontend origin | `Cors__AllowedOrigins__0` | The deployed React origin, e.g. `https://app.example.com`. |
| API base URL (frontend build) | `VITE_API_BASE_URL` | Set at build time for production. |

## Sequence (from the project plan)

1. Create separate development and production configurations.
2. Provision SQL Server with a restricted login (not `sa`).
   Generate a SQL Server migration set first: development migrations are created against SQLite and are not portable.
3. Configure HTTPS and CORS.
4. Apply reviewed EF Core migrations.
5. Deploy the API and the frontend.
6. Run smoke tests against the live deployment.
7. Verify role permissions, grade calculations and persistence.
8. Record the deployed version, known limitations, backup procedure and rollback plan.

Check current hosting prices and free-tier conditions before choosing a provider.
