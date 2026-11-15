using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ModuPOS.Auth.Authorization;
using ModuPOS.Auth.Http;
using ModuPOS.Auth.Sessions;

namespace ModuPOS.Api.Controllers.Auth;

public sealed record SignInRequest(string Login, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record SignInResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    IReadOnlyList<ProfileSummary> Profiles,
    string Next);

/// <summary>Sign-in, renewal, sign-out and current user. See contracts/auth-api.md.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    public const string SignInRateLimitPolicy = "sign-in";

    [HttpPost("sign-in")]
    [EnableRateLimiting(SignInRateLimitPolicy)]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest body, [FromServices] SignInService service, CancellationToken ct)
    {
        var result = await service.SignInAsync(body.Login ?? string.Empty, body.Password ?? string.Empty, RequestInfo(), ct);
        if (!result.IsSuccess)
            return result.Error!.ToActionResult();

        var r = result.Value!;
        return Ok(new SignInResponse(
            r.Tokens.AccessToken, r.Tokens.AccessTokenExpiresAt,
            r.Tokens.RefreshToken, r.Tokens.RefreshTokenExpiresAt,
            r.Profiles, r.Next));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest body, [FromServices] RefreshService service, CancellationToken ct)
    {
        var result = await service.RefreshAsync(body.RefreshToken ?? string.Empty, RequestInfo(), ct);
        return result.IsSuccess ? Ok(result.Value) : result.Error!.ToActionResult();
    }

    [HttpPost("sign-out")]
    [RequireSignedIn]
    public async Task<IActionResult> SignOutCurrent([FromServices] SignOutService service, CancellationToken ct)
    {
        await service.SignOutAsync(RequestInfo(), ct);
        return NoContent();
    }

    [HttpPost("sign-out-all")]
    [RequireSignedIn]
    public async Task<IActionResult> SignOutAll([FromServices] SignOutService service, CancellationToken ct) =>
        Ok(new { sessionsEnded = await service.SignOutAllAsync(RequestInfo(), ct) });

    [HttpGet("me")]
    [RequireSignedIn]
    public async Task<IActionResult> Me([FromServices] CurrentUserService service, CancellationToken ct) =>
        await service.GetAsync(ct) is { } me ? Ok(me) : AuthErrors.NotSignedIn.ToActionResult();

    private RequestInfo RequestInfo()
    {
        var agent = Request.Headers.UserAgent.ToString();
        return new RequestInfo(
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            agent.Length > 512 ? agent[..512] : agent);
    }
}
