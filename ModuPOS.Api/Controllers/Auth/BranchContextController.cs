using Microsoft.AspNetCore.Mvc;
using ModuPOS.Auth.Authorization;
using ModuPOS.Auth.Http;
using ModuPOS.Auth.Sessions;

namespace ModuPOS.Api.Controllers.Auth;

public sealed record SelectBranchRequest(Guid BranchId);

/// <summary>GET /api/auth/branches and POST /api/auth/branch — see contracts/auth-api.md.</summary>
[ApiController]
[Route("api/auth")]
[RequireSignedIn]
public sealed class BranchContextController(BranchContextService branches) : ControllerBase
{
    [HttpGet("branches")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await branches.ListAsync(Info(), ct);
        return result.IsSuccess ? Ok(result.Value) : result.Error!.ToActionResult();
    }

    [HttpPost("branch")]
    public async Task<IActionResult> Select([FromBody] SelectBranchRequest body, CancellationToken ct)
    {
        var result = await branches.SelectAsync(body.BranchId, Info(), ct);
        return result.IsSuccess ? Ok(result.Value) : result.Error!.ToActionResult();
    }

    private RequestInfo Info() =>
        new(HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString() is { Length: > 512 } a ? a[..512] : Request.Headers.UserAgent.ToString());
}
