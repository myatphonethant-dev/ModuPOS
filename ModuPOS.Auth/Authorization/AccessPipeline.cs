using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ModuPOS.Auth.Accounts;
using ModuPOS.Auth.Auditing;
using ModuPOS.Auth.Http;
using ModuPOS.Auth.Permissions;
using ModuPOS.Auth.Sessions;
using ModuPOS.Auth.Tokens;
using ModuPOS.LicenseManagement.Interfaces;
using ModuPOS.Organization.Entities;
using ModuPOS.Organization.Services;
using ModuPOS.Shared.Auditing;
using ModuPOS.Shared.Results;
using ModuPOS.Shared.Tenancy;

namespace ModuPOS.Auth.Authorization;

/// <summary>
/// The single ordered access check from contracts/authorization-pipeline.md (FR-031). It stops at
/// the first failure and returns a distinct code for each step:
/// <list type="number">
/// <item>signed in with an active session — <c>not_signed_in</c></item>
/// <item>business active, and any business id in the route is the caller's — <c>tenant_invalid</c> / 404</item>
/// <item>account and active profile active — <c>account_inactive</c></item>
/// <item>business license valid — <c>license_invalid</c></item>
/// <item>the permission's feature enabled — <c>feature_unavailable</c></item>
/// <item>permission held — <c>permission_denied</c></item>
/// <item>for [BranchScoped]: active branch valid for the user — <c>branch_denied</c></item>
/// </list>
/// Steps 2–7 apply to [RequirePermission] endpoints. [RequirePlatformPermission] runs steps 1, 3
/// and the platform permission check. [RequireSignedIn] alone runs step 1.
/// </summary>
internal sealed class AccessPipelineMiddleware(RequestDelegate next)
{
    private static readonly IReadOnlyDictionary<string, string?> FeatureOf =
        PermissionCatalog.All.ToDictionary(p => p.Code, p => p.FeatureCode, StringComparer.Ordinal);

    public async Task InvokeAsync(HttpContext context, PipelineServices s)
    {
        var metadata = context.GetEndpoint()?.Metadata;
        if (metadata?.GetMetadata<RequireSignedInAttribute>() is null)
        {
            await next(context);
            return;
        }

        var error = await CheckAsync(context, metadata, s);
        if (error is not null)
        {
            await ProblemResponses.WriteAsync(context, error);
            return;
        }

        await next(context);
    }

    private static async Task<Error?> CheckAsync(HttpContext context, EndpointMetadataCollection metadata, PipelineServices s)
    {
        var ct = context.RequestAborted;
        var tenant = s.Tenant;

        // Step 1: valid access token and active session.
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true ||
            !TryGuid(user, AuthClaims.Subject, out var userId) ||
            !TryGuid(user, AuthClaims.Session, out var sessionId) ||
            !await s.Sessions.IsActiveAsync(sessionId, ct))
        {
            return AuthErrors.NotSignedIn;
        }

        tenant.UserId = userId;
        tenant.SessionId = sessionId;
        tenant.ProfileId = TryGuid(user, AuthClaims.Profile, out var pid) ? pid : null;
        tenant.ProfileType = user.FindFirstValue(AuthClaims.ProfileType);
        tenant.BusinessId = TryGuid(user, AuthClaims.Business, out var bid) ? bid : null;
        tenant.BranchId = TryGuid(user, AuthClaims.Branch, out var brid) ? brid : null;

        var required = metadata.GetOrderedMetadata<RequirePermissionAttribute>();
        if (required.Count > 0)
            return await CheckBusinessAsync(context, metadata, required, s, ct);

        if (metadata.GetMetadata<RequirePlatformPermissionAttribute>() is { } platform)
            return await CheckPlatformAsync(platform, s, ct);

        return null;
    }

    private static async Task<Error?> CheckBusinessAsync(
        HttpContext context,
        EndpointMetadataCollection metadata,
        IReadOnlyList<RequirePermissionAttribute> required,
        PipelineServices s,
        CancellationToken ct)
    {
        var tenant = s.Tenant;

        // Business permissions belong to staff of a business only (FR-032).
        if (tenant.ProfileType != nameof(ProfileType.Staff) || tenant.ProfileId is not { } profileId || tenant.BusinessId is not { } businessId)
            return Deny(s, required[0].Code, context);

        var version = await s.Versions.GetAsync(businessId, ct);
        var status = await s.Status.GetAsync(tenant.UserId!.Value, profileId, businessId, version, ct);

        // Step 2: tenant.
        if (context.GetRouteValue("businessId") is string routeBusiness &&
            (!Guid.TryParse(routeBusiness, out var routeId) || routeId != businessId))
        {
            s.CrossTenant.Report(Guid.TryParse(routeBusiness, out var target) ? target : Guid.Empty, "Business", target, context.Request.Path);
            return AuthErrors.NotFound;
        }

        if (status.Business != BusinessStatus.Active)
        {
            await s.Revocation.RevokeForBusinessAsync(businessId, status.Business ?? BusinessStatus.Deactivated, tenant.UserId, ct);
            return AuthErrors.TenantInvalid;
        }

        // Step 3: account and profile.
        if (status.Account != AccountStatus.Active)
        {
            await s.Revocation.RevokeForUserAsync(tenant.UserId.Value, status.Account ?? AccountStatus.Deactivated, ct);
            return AuthErrors.AccountInactive;
        }

        if (status.Profile != ProfileStatus.Active)
            return AuthErrors.AccountInactive;

        // Step 4: license.
        if (!await s.Licenses.IsValidAsync(businessId, ct))
            return AuthErrors.LicenseInvalid;

        // Step 5: feature behind each required permission.
        foreach (var feature in required.Select(r => FeatureOf.GetValueOrDefault(r.Code)).OfType<string>().Distinct())
        {
            if (!await s.Features.IsEnabledAsync(businessId, feature, ct))
                return AuthErrors.FeatureUnavailable;
        }

        // Step 6: permission.
        tenant.Permissions = await s.Permissions.GetForStaffAsync(profileId, businessId, tenant.BranchId, ct);
        foreach (var r in required)
        {
            if (!tenant.Permissions.Contains(r.Code))
                return Deny(s, r.Code, context);
        }

        // Step 7: active branch.
        if (metadata.GetMetadata<BranchScopedAttribute>() is not null)
        {
            var access = tenant.BranchId is { } branchId
                ? await s.Status.BranchAsync(profileId, businessId, branchId, version, ct)
                : (BranchAccess?)null;

            if (access != BranchAccess.Allowed)
            {
                var reason = access is null ? "no_active_branch" : BranchContextService.BranchDeniedReason(access.Value);
                s.Audit.Enqueue(Entry(s, AuditCodes.BranchDenied, access == BranchAccess.OtherBusiness ? AuditSeverity.High : AuditSeverity.Warning,
                    "Branch", tenant.BranchId?.ToString(), new Dictionary<string, object?>
                    {
                        ["branchId"] = tenant.BranchId,
                        ["reason"] = reason,
                        ["action"] = context.Request.Path.Value
                    }, $"{tenant.UserId:N}:{AuditCodes.BranchDenied}:{context.Request.Path}:{reason}"));
                return AuthErrors.BranchDenied(reason);
            }
        }

        return null;
    }

    private static async Task<Error?> CheckPlatformAsync(RequirePlatformPermissionAttribute platform, PipelineServices s, CancellationToken ct)
    {
        var tenant = s.Tenant;
        if (tenant.ProfileType != nameof(ProfileType.PlatformAdmin) || tenant.ProfileId is not { } profileId)
            return AuthErrors.PermissionDenied;

        // Step 3 for platform admins (no business, so no version; short cache).
        var status = await s.Status.GetAsync(tenant.UserId!.Value, profileId, null, 0, ct);
        if (status.Account != AccountStatus.Active)
        {
            await s.Revocation.RevokeForUserAsync(tenant.UserId.Value, status.Account ?? AccountStatus.Deactivated, ct);
            return AuthErrors.AccountInactive;
        }

        if (status.Profile != ProfileStatus.Active)
            return AuthErrors.AccountInactive;

        tenant.Permissions = await s.Permissions.GetForPlatformAdminAsync(ct);
        return tenant.Permissions.Contains(platform.Code) ? null : AuthErrors.PermissionDenied;
    }

    private static Error Deny(PipelineServices s, string permission, HttpContext context)
    {
        var tenant = s.Tenant;
        s.Audit.Enqueue(Entry(s, AuditCodes.PermissionDenied, AuditSeverity.Warning, null, null, new Dictionary<string, object?>
        {
            ["permissionRequired"] = permission,
            ["action"] = $"{context.Request.Method} {context.Request.Path}"
        }, $"{tenant.UserId:N}:{AuditCodes.PermissionDenied}:{context.Request.Method}:{context.Request.Path}:{permission}"));
        return AuthErrors.PermissionDenied;
    }

    private static AuditEntry Entry(
        PipelineServices s,
        string code,
        AuditSeverity severity,
        string? targetType,
        string? targetId,
        Dictionary<string, object?> details,
        string dedupKey) => new()
    {
        Code = code,
        Severity = severity,
        Outcome = AuditOutcome.Denied,
        OccurredAt = s.Time.GetUtcNow().UtcDateTime,
        ActorUserId = s.Tenant.UserId,
        ActorProfileId = s.Tenant.ProfileId,
        BusinessId = s.Tenant.BusinessId,
        BranchId = s.Tenant.BranchId,
        SessionId = s.Tenant.SessionId,
        TargetType = targetType,
        TargetId = targetId,
        Details = details,
        DedupKey = dedupKey
    };

    private static bool TryGuid(ClaimsPrincipal user, string claim, out Guid value) =>
        Guid.TryParse(user.FindFirstValue(claim), out value);
}

/// <summary>Scoped dependencies of the pipeline, bundled to keep the middleware signature small.</summary>
internal sealed record PipelineServices(
    TenantContext Tenant,
    SessionStatusCache Sessions,
    AuthorizationVersionService Versions,
    AccessStatusService Status,
    EffectivePermissionService Permissions,
    IFeatureAvailability Features,
    ITenantLicenseStatus Licenses,
    SessionRevocationService Revocation,
    CrossTenantGuard CrossTenant,
    IAuditWriter Audit,
    TimeProvider Time);
