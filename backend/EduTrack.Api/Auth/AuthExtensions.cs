using System.Security.Claims;
using System.Threading.RateLimiting;
using EduTrack.Application.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace EduTrack.Api.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddEduTrackAuth(
        this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IPasswordService, IdentityPasswordService>();
        services.AddScoped<AuthService>();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "edutrack.auth";
                options.Cookie.HttpOnly = true;                       // not readable by JavaScript
                options.Cookie.SameSite = SameSiteMode.Lax;
                // Always require HTTPS outside development. Development runs on plain http://localhost.
                options.Cookie.SecurePolicy = environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;

                // An API answers with status codes; it never redirects a fetch() call to a login page.
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };

                // Re-check the account on every request: a deactivated account, or one whose role changed,
                // loses access immediately instead of when its cookie expires.
                options.Events.OnValidatePrincipal = async context =>
                {
                    var idClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                    var roleClaim = context.Principal?.FindFirstValue(ClaimTypes.Role);
                    var store = context.HttpContext.RequestServices.GetRequiredService<IUserStore>();

                    var status = int.TryParse(idClaim, out var userId)
                        ? await store.GetStatusAsync(userId, context.HttpContext.RequestAborted)
                        : null;

                    if (status is null || !status.IsActive || status.Role.ToString() != roleClaim)
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                };
            });

        // Secure by default: every endpoint requires a signed-in user unless it opts out with [AllowAnonymous].
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.Administrator, p => p.RequireRole(Roles.Administrator))
            .AddPolicy(Policies.Lecturer, p => p.RequireRole(Roles.Lecturer))
            .AddPolicy(Policies.Student, p => p.RequireRole(Roles.Student))
            .AddPolicy(Policies.Staff, p => p.RequireRole(Roles.Administrator, Roles.Lecturer));

        // Slow down password guessing: limited sign-in attempts per client address per minute.
        // The limit is read per request from the final configuration so it can be tuned (and overridden in tests).
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicies.Login, httpContext =>
            {
                var attemptsPerMinute = httpContext.RequestServices
                    .GetRequiredService<IConfiguration>()
                    .GetValue("Auth:LoginAttemptsPerMinute", 10);

                return RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = attemptsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    });
            });
        });

        return services;
    }

    /// <summary>Adds rate limiting, the CSRF header check, authentication and authorisation, in the right order.</summary>
    public static IApplicationBuilder UseEduTrackSecurity(this IApplicationBuilder app)
    {
        app.UseRateLimiter();
        app.UseMiddleware<RequireClientHeaderMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    public static ClaimsPrincipal CreatePrincipal(AuthUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role.ToString()),
        };
        if (user.StudentId is { } studentId) claims.Add(new Claim(ClaimNames.StudentId, studentId.ToString()));
        if (user.LecturerId is { } lecturerId) claims.Add(new Claim(ClaimNames.LecturerId, lecturerId.ToString()));

        return new ClaimsPrincipal(new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role));
    }
}
