using EduTrack.Domain;

namespace EduTrack.Application.Auth;

/// <summary>Everything the login flow needs to know about an account. Never sent to clients.</summary>
public sealed record AuthUser(
    int UserId,
    string Email,
    string PasswordHash,
    UserRole Role,
    bool IsActive,
    int? StudentId,
    int? LecturerId,
    string DisplayName);

/// <summary>Current state of an account, re-checked on every request so deactivation takes effect immediately.</summary>
public sealed record UserStatus(bool IsActive, UserRole Role);

/// <summary>Safe-to-expose view of an account (no password hash).</summary>
public sealed record UserSummary(int Id, string Email, UserRole Role, bool IsActive);

public interface IUserStore
{
    Task<AuthUser?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<UserStatus?> GetStatusAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserSummary>> ListAsync(CancellationToken ct = default);
}

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string hash, string password);

    /// <summary>
    /// Performs a verification against a fixed throwaway hash. Called when the email is unknown so that
    /// "no such account" and "wrong password" take the same time and cannot be told apart.
    /// </summary>
    void BurnVerification(string password);
}
