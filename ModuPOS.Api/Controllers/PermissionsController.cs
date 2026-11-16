using Microsoft.AspNetCore.Mvc;
using ModuPOS.Auth.Services.Permission;

namespace ModuPOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PermissionsController(IPermissionService permissionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await permissionService.GetPermissionsAsync(ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] string name, CancellationToken ct)
    {
        var result = await permissionService.CreatePermissionAsync(name, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
