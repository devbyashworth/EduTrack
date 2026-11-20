using System.ComponentModel.DataAnnotations;
using EduTrack.Api.Auth;
using EduTrack.Application.Auth;
using EduTrack.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EduTrack.Api.Controllers;

public sealed class LoginRequest
{
    [Required, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    // Capped so an enormous "password" cannot be used to burn CPU in the hasher.
    [Required, StringLength(128)]
    public string Password { get; init; } = string.Empty;
}

public sealed record CurrentUserDto(int Id, string Email, string DisplayName, UserRole Role);

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService auth, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ActionResult<CurrentUserDto>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await auth.LoginAsync(request.Email, request.Password, ct);
        if (user is null)
            return Unauthorized(new { message = "The email or password is incorrect." });

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            AuthExtensions.CreatePrincipal(user),
            new AuthenticationProperties { IsPersistent = false });

        return Ok(new CurrentUserDto(user.UserId, user.Email, user.DisplayName, user.Role));
    }

    /// <summary>Safe to call when already signed out.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    /// <summary>Who am I? The frontend calls this on load to restore the session.</summary>
    [HttpGet("me")]
    public ActionResult<CurrentUserDto> Me() =>
        Ok(new CurrentUserDto(currentUser.UserId, currentUser.Email, currentUser.DisplayName, currentUser.Role));
}
