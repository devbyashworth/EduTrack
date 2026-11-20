using EduTrack.Application.Auth;
using EduTrack.Domain;

namespace EduTrack.Tests.Auth;

public class AuthServiceTests
{
    private sealed class FakePasswords : IPasswordService
    {
        public int BurnCalls { get; private set; }
        public string Hash(string password) => "hash:" + password;
        public bool Verify(string hash, string password) => hash == "hash:" + password;
        public void BurnVerification(string password) => BurnCalls++;
    }

    private sealed class FakeStore(params AuthUser[] users) : IUserStore
    {
        public Task<AuthUser?> FindByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<UserStatus?> GetStatusAsync(int userId, CancellationToken ct = default) =>
            Task.FromResult(users.Where(u => u.UserId == userId).Select(u => new UserStatus(u.IsActive, u.Role)).FirstOrDefault());

        public Task<IReadOnlyList<UserSummary>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<UserSummary>>(users.Select(u => new UserSummary(u.UserId, u.Email, u.Role, u.IsActive)).ToList());
    }

    private static AuthUser User(bool active = true) =>
        new(1, "sam@example.test", "hash:correct-password", UserRole.Student, active, 7, null, "Sam Student");

    [Fact]
    public async Task Correct_credentials_sign_in()
    {
        var service = new AuthService(new FakeStore(User()), new FakePasswords());

        var result = await service.LoginAsync("sam@example.test", "correct-password");

        Assert.NotNull(result);
        Assert.Equal(1, result!.UserId);
    }

    [Fact]
    public async Task Email_matching_ignores_case_and_surrounding_spaces()
    {
        var service = new AuthService(new FakeStore(User()), new FakePasswords());

        Assert.NotNull(await service.LoginAsync("  SAM@Example.TEST ", "correct-password"));
    }

    [Fact]
    public async Task Wrong_password_is_rejected()
    {
        var service = new AuthService(new FakeStore(User()), new FakePasswords());

        Assert.Null(await service.LoginAsync("sam@example.test", "wrong"));
    }

    [Fact]
    public async Task Deactivated_accounts_cannot_sign_in_even_with_the_right_password()
    {
        var service = new AuthService(new FakeStore(User(active: false)), new FakePasswords());

        Assert.Null(await service.LoginAsync("sam@example.test", "correct-password"));
    }

    [Fact]
    public async Task Unknown_email_burns_a_verification_so_timing_does_not_reveal_which_emails_exist()
    {
        var passwords = new FakePasswords();
        var service = new AuthService(new FakeStore(User()), passwords);

        var result = await service.LoginAsync("nobody@example.test", "anything");

        Assert.Null(result);
        Assert.Equal(1, passwords.BurnCalls);
    }

    [Fact]
    public async Task Known_email_does_not_burn_a_verification()
    {
        var passwords = new FakePasswords();
        var service = new AuthService(new FakeStore(User()), passwords);

        await service.LoginAsync("sam@example.test", "wrong");

        Assert.Equal(0, passwords.BurnCalls);
    }
}
