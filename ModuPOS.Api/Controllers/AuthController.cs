using Microsoft.AspNetCore.Mvc;
using ModuPOS.Auth.DTOs;
using ModuPOS.Auth.Services.Auth;

namespace ModuPOS.Api.Controllers;

/// <summary>
/// REST API controller for authentication, session renewal, and token revocation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    /// Authenticates a user and issues JWT access and refresh tokens.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Renews an access token using an active refresh token.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await authService.RefreshTokenAsync(request, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Revokes a refresh token, terminating the active session.
    /// </summary>
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await authService.RevokeTokenAsync(request.RefreshToken, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
