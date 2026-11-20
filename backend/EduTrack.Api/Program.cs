using System.Text.Json.Serialization;
using EduTrack.Api.Auth;
using EduTrack.Application.Auth;
using EduTrack.Infrastructure;
using EduTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    // Send enums such as roles as readable strings ("Administrator"), not numbers.
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEduTrackInfrastructure();
builder.Services.AddEduTrackAuth(builder.Environment);

// The React app may be served from a different origin, so allowed origins are explicit config.
// Credentials (the session cookie) are only ever allowed for those listed origins.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

// Fail fast: creating a DbContext reads and validates the database settings, so a missing or wrong
// connection string stops the app at startup instead of on the first request.
using (var startupCheck = app.Services.CreateScope())
{
    _ = startupCheck.ServiceProvider.GetRequiredService<EduTrackDbContext>();
}

// Local development convenience: create/update the SQLite file on startup and add demo accounts so
// `dotnet run` just works. Never done for SQL Server; production schema changes are applied deliberately
// from reviewed migrations.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EduTrackDbContext>();
    if (db.Database.IsSqlite())
    {
        if (db.Database.GetMigrations().Any())
        {
            db.Database.Migrate();
            await DevSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordService>());
        }
        else
        {
            app.Logger.LogWarning(
                "No EF Core migrations found, so the database has no tables. " +
                "Run: dotnet ef migrations add InitialCreate --project ../EduTrack.Infrastructure --startup-project .");
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseEduTrackSecurity();   // rate limiting, CSRF header check, authentication, authorisation

app.MapControllers();

// Public liveness/readiness probe. Reports connectivity only; never exposes internals.
app.MapGet("/api/health", async (EduTrackDbContext db, CancellationToken ct) =>
{
    var databaseUp = await db.Database.CanConnectAsync(ct);
    var body = new { status = databaseUp ? "healthy" : "degraded", database = databaseUp };
    return databaseUp ? Results.Ok(body) : Results.Json(body, statusCode: StatusCodes.Status503ServiceUnavailable);
}).AllowAnonymous();

app.Run();

// Exposed so integration tests can use WebApplicationFactory<Program>.
public partial class Program { }
