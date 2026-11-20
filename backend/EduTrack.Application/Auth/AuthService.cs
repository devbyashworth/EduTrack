namespace EduTrack.Application.Auth;

/// <summary>Decides whether a set of credentials may sign in.</summary>
public sealed class AuthService(IUserStore store, IPasswordService passwords)
{
    /// <summary>
    /// Returns the account when the email and password are correct and the account is active; otherwise null.
    /// The caller cannot tell why it failed (unknown email, wrong password, deactivated), by design:
    /// distinguishing them would let an attacker discover which emails have accounts.
    /// </summary>
    public async Task<AuthUser?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await store.FindByEmailAsync(email.Trim(), ct);

        if (user is null)
        {
            passwords.BurnVerification(password);
            return null;
        }

        // Verify before checking IsActive so a deactivated account costs the same time as a wrong password.
        var passwordOk = passwords.Verify(user.PasswordHash, password);
        return passwordOk && user.IsActive ? user : null;
    }
}
