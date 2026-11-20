using EduTrack.Application.Auth;
using EduTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EduTrack.Infrastructure.Auth;

public sealed class EfUserStore(EduTrackDbContext db) : IUserStore
{
    public async Task<AuthUser?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        // Email comparison is case-insensitive on both SQL Server (default collation) and SQLite (NOCASE).
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null) return null;

        var student = await db.Students.AsNoTracking()
            .Where(s => s.UserId == user.Id)
            .Select(s => new { s.Id, s.FirstName, s.LastName })
            .FirstOrDefaultAsync(ct);

        var lecturer = await db.Lecturers.AsNoTracking()
            .Where(l => l.UserId == user.Id)
            .Select(l => new { l.Id, l.FirstName, l.LastName })
            .FirstOrDefaultAsync(ct);

        var displayName = student is not null ? $"{student.FirstName} {student.LastName}"
            : lecturer is not null ? $"{lecturer.FirstName} {lecturer.LastName}"
            : user.Email;

        return new AuthUser(
            user.Id, user.Email, user.PasswordHash, user.Role, user.IsActive,
            student?.Id, lecturer?.Id, displayName);
    }

    public async Task<UserStatus?> GetStatusAsync(int userId, CancellationToken ct = default) =>
        await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserStatus(u.IsActive, u.Role))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<UserSummary>> ListAsync(CancellationToken ct = default) =>
        await db.Users.AsNoTracking()
            .OrderBy(u => u.Email)
            .Select(u => new UserSummary(u.Id, u.Email, u.Role, u.IsActive))
            .ToListAsync(ct);
}
