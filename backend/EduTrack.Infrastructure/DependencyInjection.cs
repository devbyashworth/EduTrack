using EduTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEduTrackInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Database:Provider is "Sqlite" (local development) or "SqlServer" (production).
        // The base appsettings.json says SqlServer so a deployment can never silently fall back to SQLite;
        // appsettings.Development.json switches development to a local SQLite file.
        var provider = configuration["Database:Provider"];
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(provider))
            throw new InvalidOperationException("Database:Provider is not configured (use Sqlite or SqlServer).");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. See README.md and docs/deployment.md.");

        services.AddDbContext<EduTrackDbContext>(options =>
        {
            switch (provider.Trim().ToLowerInvariant())
            {
                case "sqlite":
                    options.UseSqlite(connectionString);
                    break;
                case "sqlserver":
                    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unknown Database:Provider '{provider}'. Use Sqlite or SqlServer.");
            }
        });

        return services;
    }
}
