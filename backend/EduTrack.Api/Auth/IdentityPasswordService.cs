using EduTrack.Application.Auth;
using Microsoft.AspNetCore.Identity;

namespace EduTrack.Api.Auth;

/// <summary>
/// Password hashing using ASP.NET Core Identity's PasswordHasher (PBKDF2 with a per-password random salt and
/// versioned format, so the work factor can be raised later and old hashes upgraded). Passwords are never stored.
/// </summary>
public sealed class IdentityPasswordService : IPasswordService
{
    private readonly PasswordHasher<object> _hasher = new();
    private readonly Lazy<string> _throwawayHash;

    public IdentityPasswordService()
    {
        _throwawayHash = new Lazy<string>(() => _hasher.HashPassword(null!, Guid.NewGuid().ToString("N")));
    }

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string hash, string password)
    {
        if (string.IsNullOrEmpty(hash)) return false;
        var result = _hasher.VerifyHashedPassword(null!, hash, password);
        return result != PasswordVerificationResult.Failed;
    }

    public void BurnVerification(string password) =>
        _hasher.VerifyHashedPassword(null!, _throwawayHash.Value, password);
}
