using EduTrack.Application.Auth;
using EduTrack.Domain;
using EduTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Infrastructure.Persistence;

/// <summary>
/// Creates three fictional demo accounts in an empty local development database so there is something to sign
/// in with. It never runs in production (Program.cs only calls it for SQLite in Development). The shared password
/// below protects fictional data on a developer's own machine and must never be used for a real account.
/// Richer synthetic data (courses, enrolments, marks) is added in the core-workflow step.
/// </summary>
public static class DevSeeder
{
    public const string DemoPassword = "Dev-Passw0rd!";

    public static async Task SeedAsync(EduTrackDbContext db, IPasswordService passwords, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct)) return;

        var hash = passwords.Hash(DemoPassword);

        db.Users.Add(new User { Email = "admin@edutrack.test", PasswordHash = hash, Role = UserRole.Administrator });

        db.Lecturers.Add(new Lecturer
        {
            EmployeeNumber = "E1001",
            FirstName = "Leo",
            LastName = "Lecturer",
            User = new User { Email = "lecturer@edutrack.test", PasswordHash = hash, Role = UserRole.Lecturer },
        });

        db.Students.Add(new Student
        {
            StudentNumber = "S2026001",
            FirstName = "Sam",
            LastName = "Student",
            User = new User { Email = "student@edutrack.test", PasswordHash = hash, Role = UserRole.Student },
        });

        await db.SaveChangesAsync(ct);
    }
}
