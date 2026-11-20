using System.Security.Claims;
using EduTrack.Domain;

namespace EduTrack.Api.Auth;

/// <summary>
/// The signed-in user for the current request. Record-level access rules (a student sees only their own
/// results, a lecturer only their own course offerings) are written against this, never against ids that
/// arrive in the URL or request body.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    int UserId { get; }
    string Email { get; }
    string DisplayName { get; }
    UserRole Role { get; }
    int? StudentId { get; }
    int? LecturerId { get; }
}

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User ?? new ClaimsPrincipal();

    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;

    public int UserId => int.Parse(Require(ClaimTypes.NameIdentifier));
    public string Email => Require(ClaimTypes.Email);
    public string DisplayName => Require(ClaimTypes.Name);
    public UserRole Role => Enum.Parse<UserRole>(Require(ClaimTypes.Role));
    public int? StudentId => OptionalInt(ClaimNames.StudentId);
    public int? LecturerId => OptionalInt(ClaimNames.LecturerId);

    private string Require(string type) =>
        Principal.FindFirstValue(type) ?? throw new InvalidOperationException("There is no authenticated user.");

    private int? OptionalInt(string type) =>
        int.TryParse(Principal.FindFirstValue(type), out var value) ? value : null;
}
