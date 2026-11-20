using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EduTrack.Application.Auth;
using EduTrack.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EduTrack.Tests.Api;

/// <summary>
/// Authentication and role-based access through the real HTTP pipeline.
/// Covers FR-01 and FR-02 and the first negative-access tests for AT-05 / AT-06.
/// </summary>
public sealed class AuthEndpointsTests : IClassFixture<AuthEndpointsTests.Fixture>
{
    public sealed class Fixture : IDisposable
    {
        public EduTrackApiFactory Factory { get; } = new EduTrackApiFactory().WithSeedData();
        public void Dispose() => Factory.Dispose();
    }

    private readonly EduTrackApiFactory _factory;

    public AuthEndpointsTests(Fixture fixture) => _factory = fixture.Factory;

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password = EduTrackApiFactory.Password) =>
        await client.PostAsJsonAsync("/api/auth/login", new { email, password });

    private async Task<HttpClient> SignedInAsync(string email)
    {
        var client = _factory.CreateBrowserClient();
        var response = await LoginAsync(client, email);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    [Fact]
    public async Task Valid_login_returns_the_user_and_an_http_only_cookie()
    {
        var client = _factory.CreateBrowserClient();

        var response = await LoginAsync(client, "student@example.test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Student", body.GetProperty("role").GetString());
        Assert.Equal("Sam Student", body.GetProperty("displayName").GetString());
        Assert.False(body.TryGetProperty("passwordHash", out _));

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("student@example.test", "wrong-password")]
    [InlineData("nobody@example.test", EduTrackApiFactory.Password)]
    [InlineData("inactive@example.test", EduTrackApiFactory.Password)]
    public async Task Bad_credentials_all_get_the_same_401_so_accounts_cannot_be_discovered(string email, string password)
    {
        var client = _factory.CreateBrowserClient();

        var response = await LoginAsync(client, email, password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("The email or password is incorrect.", body.GetProperty("message").GetString());
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Login_with_missing_fields_is_a_validation_error()
    {
        var client = _factory.CreateBrowserClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Me_requires_a_session_and_then_returns_the_signed_in_user()
    {
        var anonymous = _factory.CreateBrowserClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/auth/me")).StatusCode);

        var client = await SignedInAsync("lecturer@example.test");
        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");

        Assert.Equal("Lecturer", me.GetProperty("role").GetString());
        Assert.Equal("lecturer@example.test", me.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Logout_ends_the_session()
    {
        var client = await SignedInAsync("student@example.test");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    // ---- Role-based access (FR-02): the backend decides, regardless of what the UI shows. ----

    [Fact]
    public async Task Anonymous_requests_to_protected_endpoints_get_401_not_a_redirect()
    {
        var client = _factory.CreateBrowserClient();

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("student@example.test", HttpStatusCode.Forbidden)]
    [InlineData("lecturer@example.test", HttpStatusCode.Forbidden)]
    [InlineData("admin@example.test", HttpStatusCode.OK)]
    public async Task Only_administrators_can_list_users(string email, HttpStatusCode expected)
    {
        var client = await SignedInAsync(email);

        var response = await client.GetAsync("/api/users");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task User_list_never_exposes_password_hashes()
    {
        var client = await SignedInAsync("admin@example.test");

        var json = await client.GetStringAsync("/api/users");

        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AQAAAA", json); // prefix of ASP.NET Core Identity hashes
        Assert.Contains("student@example.test", json);
    }

    [Fact]
    public async Task Health_check_stays_public()
    {
        var client = _factory.CreateBrowserClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---- Session is re-checked on every request ----

    [Fact]
    public async Task Deactivating_an_account_ends_its_existing_session_immediately()
    {
        var client = await SignedInAsync("student@example.test");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EduTrackDbContext>();
            db.Users.Single(u => u.Email == "student@example.test").IsActive = false;
            db.SaveChanges();
        }

        try
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        }
        finally
        {
            // Put the account back so other tests in this class are unaffected.
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EduTrackDbContext>();
            db.Users.Single(u => u.Email == "student@example.test").IsActive = true;
            db.SaveChanges();
        }
    }

    // ---- CSRF defence ----

    [Fact]
    public async Task State_changing_requests_without_the_client_header_are_refused()
    {
        var client = _factory.CreateBrowserClient(sendClientHeader: false);

        var response = await LoginAsync(client, "student@example.test");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }
}

/// <summary>Separate class, separate factory: this one runs with a tiny rate limit.</summary>
public sealed class LoginRateLimitTests
{
    [Fact]
    public async Task Repeated_sign_in_attempts_are_rate_limited()
    {
        using var factory = new RateLimitedFactory().WithSeedData();
        var client = factory.CreateBrowserClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++)
            statuses.Add((await client.PostAsJsonAsync("/api/auth/login", new { email = "student@example.test", password = "wrong" })).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, statuses[0]);
        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
    }

    private sealed class RateLimitedFactory : EduTrackApiFactory
    {
        protected override int LoginAttemptsPerMinute => 3;
    }
}
