using EduTrack.Api.Auth;
using EduTrack.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduTrack.Api.Controllers;

/// <summary>Account administration. Administrators only; password hashes are never returned.</summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = Policies.Administrator)]
public sealed class UsersController(IUserStore users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserSummary>>> List(CancellationToken ct) =>
        Ok(await users.ListAsync(ct));
}
