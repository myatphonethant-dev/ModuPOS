using Microsoft.AspNetCore.Mvc;
using ModuPOS.Auth.Authorization;
using ModuPOS.Auth.Permissions;
using ModuPOS.Shared.Tenancy;

namespace ModuPOS.Api.Controllers.Rbac;

public sealed record PermissionItem(string Code, string Name, string Group, string? FeatureCode, IReadOnlyList<string> DependsOn, bool Available);

/// <summary>
/// GET /api/permissions — the business permission catalog with availability for the caller's
/// business. Platform and retired permissions are never listed.
/// </summary>
[ApiController]
[Route("api/permissions")]
public sealed class PermissionsController : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCodes.RoleView)]
    public async Task<IActionResult> List(
        [FromServices] EffectivePermissionService permissions,
        [FromServices] ITenantContext caller,
        CancellationToken ct)
    {
        var availability = await permissions.CatalogForBusinessAsync(caller.BusinessId!.Value, ct);
        var definitions = PermissionCatalog.All.ToDictionary(p => p.Code, StringComparer.Ordinal);

        var items = availability
            .Where(a => definitions.ContainsKey(a.Permission.Code))
            .Select(a =>
            {
                var d = definitions[a.Permission.Code];
                return new PermissionItem(d.Code, d.Name, d.Group, d.FeatureCode, d.DependsOn, a.Available);
            })
            .OrderBy(p => p.Group).ThenBy(p => p.Code)
            .ToList();

        return Ok(items);
    }
}
