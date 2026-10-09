using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelVault.Identity.Dtos;
using PixelVault.Identity.Services;
using PixelVault.Shared.Auth;

namespace PixelVault.Identity.Controllers;

[ApiController]
[Route("auth")]
[Produces("application/json")]
public class AuthController(IAuthService auth) : ControllerBase
{
    /// <summary>Signs in a staff member and returns a JWT to send as <c>Authorization: Bearer &lt;token&gt;</c>.</summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await auth.LoginAsync(request, cancellationToken));

    /// <summary>The signed in staff member.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken) =>
        Ok(await auth.GetCurrentUserAsync(User.GetUsername(), cancellationToken));
}

[ApiController]
[Route("users")]
[Produces("application/json")]
[Authorize(Policy = Policies.AdminOnly)]
public class UsersController(IUserService users) : ControllerBase
{
    /// <summary>All staff accounts. Admin only.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StaffUserDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await users.GetAllAsync(cancellationToken));

    /// <summary>Unlocks an account that was locked after too many failed sign-ins. Admin only.</summary>
    [HttpPost("{username}/unlock")]
    public async Task<ActionResult<StaffUserDto>> Unlock(string username, CancellationToken cancellationToken) =>
        Ok(await users.UnlockAsync(username, cancellationToken));
}
