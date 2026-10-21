using EduTrack.Infrastructure;
using EduTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEduTrackInfrastructure(builder.Configuration);

// The React app is served from a different origin, so allowed origins are explicit config.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Local development convenience: create/update the SQLite file on startup so `dotnet run` just works.
// Never done for SQL Server; production schema changes are applied deliberately from reviewed migrations.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EduTrackDbContext>();
    if (db.Database.IsSqlite())
    {
        if (db.Database.GetMigrations().Any())
            db.Database.Migrate();
        else
            app.Logger.LogWarning(
                "No EF Core migrations found, so the database has no tables. " +
                "Run: dotnet ef migrations add InitialCreate --project ../EduTrack.Infrastructure --startup-project .");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");

// Authentication and authorisation middleware are added in the security step.
app.UseAuthorization();

app.MapControllers();

// Public liveness/readiness probe. Reports connectivity only; never exposes internals.
app.MapGet("/api/health", async (EduTrackDbContext db, CancellationToken ct) =>
{
    var databaseUp = await db.Database.CanConnectAsync(ct);
    var body = new { status = databaseUp ? "healthy" : "degraded", database = databaseUp };
    return databaseUp ? Results.Ok(body) : Results.Json(body, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.Run();

// Exposed so integration tests can use WebApplicationFactory<Program>.
public partial class Program { }
