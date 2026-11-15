using Microsoft.AspNetCore.Mvc;
using ModuPOS.Auth.Authorization;
using ModuPOS.Auth.Http;
using ModuPOS.Auth.Portals;

namespace ModuPOS.Api.Controllers.Auth;

public sealed record ChoosePortalRequest(Guid ProfileId);

/// <summary>POST /api/auth/portal — see contracts/auth-api.md.</summary>
[ApiController]
[Route("api/auth/portal")]
[RequireSignedIn]
public sealed class PortalController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Choose([FromBody] ChoosePortalRequest body, [FromServices] PortalService service, CancellationToken ct)
    {
        var result = await service.SelectAsync(body.ProfileId, ct);
        return result.IsSuccess ? Ok(result.Value) : result.Error!.ToActionResult();
    }
}
