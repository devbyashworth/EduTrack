using EduTrack.Application.Auth;
using EduTrack.Domain;
using EduTrack.Domain.Entities;
using EduTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduTrack.Tests.Api;

/// <summary>
/// Starts the real API in memory against a private in-memory SQLite database, with three known accounts.
/// The schema is created straight from the model (EnsureCreated), so these tests do not depend on migrations.
/// </summary>
public class EduTrackApiFactory : WebApplicationFactory<Program>
{
    public const string Password = "Test-Passw0rd!";

    /// <summary>Generous by default so ordinary tests are never throttled; a test can override it to check the limit.</summary>
    protected virtual int LoginAttemptsPerMinute => 1000;

    private readonly string _connectionString = $"Data Source=edutrack-test-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAlive;

    public EduTrackApiFactory()
    {
        // A shared in-memory database disappears when its last connection closes; this one keeps it alive.
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["ConnectionStrings:DefaultConnection"] = _connectionString,
            ["Auth:LoginAttemptsPerMinute"] = LoginAttemptsPerMinute.ToString(),
        }));
    }

    /// <summary>Creates the schema and the standard test accounts. Call once after construction.</summary>
    public EduTrackApiFactory WithSeedData()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduTrackDbContext>();
        var hash = scope.ServiceProvider.GetRequiredService<IPasswordService>().Hash(Password);
        db.Database.EnsureCreated();

        db.Users.Add(new User { Email = "admin@example.test", PasswordHash = hash, Role = UserRole.Administrator });
        db.Lecturers.Add(new Lecturer
        {
            EmployeeNumber = "E1", FirstName = "Lee", LastName = "Lecturer",
            User = new User { Email = "lecturer@example.test", PasswordHash = hash, Role = UserRole.Lecturer },
        });
        db.Students.Add(new Student
        {
            StudentNumber = "S1", FirstName = "Sam", LastName = "Student",
            User = new User { Email = "student@example.test", PasswordHash = hash, Role = UserRole.Student },
        });
        db.Users.Add(new User { Email = "inactive@example.test", PasswordHash = hash, Role = UserRole.Student, IsActive = false });
        db.SaveChanges();
        return this;
    }

    /// <summary>A client that behaves like the browser app: HTTPS, cookies kept, and the required CSRF header sent.</summary>
    public HttpClient CreateBrowserClient(bool sendClientHeader = true)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
        if (sendClientHeader)
            client.DefaultRequestHeaders.Add("X-Requested-With", "EduTrack");
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _keepAlive.Dispose();
    }
}
