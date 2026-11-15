using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModuPOS.Shared.Results;

namespace ModuPOS.Auth.Http;

/// <summary>Stable error codes from contracts/auth-api.md and contracts/authorization-pipeline.md.</summary>
public static class AuthErrors
{
    public static readonly Error InvalidCredentials = new("invalid_credentials", "The username/email or password is incorrect.", StatusCodes.Status401Unauthorized);
    public static readonly Error AccountInactive = new("account_inactive", "This account is not active.", StatusCodes.Status403Forbidden);
    public static readonly Error RefreshInvalid = new("refresh_invalid", "The renewal credential is invalid or expired.", StatusCodes.Status401Unauthorized);
    public static readonly Error RefreshReused = new("refresh_reused", "The renewal credential was already used. The session has been ended.", StatusCodes.Status401Unauthorized);
    public static readonly Error NotSignedIn = new("not_signed_in", "You are not signed in.", StatusCodes.Status401Unauthorized);
    public static readonly Error PermissionDenied = new("permission_denied", "You do not have permission to do this.", StatusCodes.Status403Forbidden);
    public static readonly Error TooManyRequests = new("too_many_requests", "Too many attempts. Try again shortly.", StatusCodes.Status429TooManyRequests);

    public static readonly Error TenantInvalid = new("tenant_invalid", "Your business is not active.", StatusCodes.Status403Forbidden);
    public static readonly Error LicenseInvalid = new("license_invalid", "Your business does not have a valid license.", StatusCodes.Status403Forbidden);
    public static readonly Error FeatureUnavailable = new("feature_unavailable", "This feature is not available for your business.", StatusCodes.Status403Forbidden);
    public static readonly Error NotFound = new("not_found", "Not found.", StatusCodes.Status404NotFound);

    /// <summary>Reasons: not_assigned, inactive, other_business, no_active_branch.</summary>
    public static Error BranchDenied(string reason) => new(
        "branch_denied",
        "You cannot work in this branch.",
        StatusCodes.Status403Forbidden,
        new Dictionary<string, object?> { ["reason"] = reason });

    public static Error AccountLocked(DateTime lockedUntil) => new(
        "account_locked",
        "This account is temporarily locked after repeated failed sign-ins.",
        StatusCodes.Status423Locked,
        new Dictionary<string, object?> { ["lockedUntil"] = lockedUntil });
}

/// <summary>Maps <see cref="Error"/> to RFC 9457 problem details with a <c>code</c> extension.</summary>
public static class ProblemResponses
{
    public static ProblemDetails ToProblemDetails(this Error error)
    {
        var problem = new ProblemDetails
        {
            Status = error.Status,
            Title = error.Message,
            Type = $"https://modupos.dev/errors/{error.Code}"
        };

        problem.Extensions["code"] = error.Code;

        if (error.Extensions is not null)
        {
            foreach (var (key, value) in error.Extensions)
                problem.Extensions[key] = value;
        }

        return problem;
    }

    public static IActionResult ToActionResult(this Error error) =>
        new ObjectResult(error.ToProblemDetails())
        {
            StatusCode = error.Status,
            ContentTypes = { "application/problem+json" }
        };

    public static Task WriteAsync(HttpContext context, Error error)
    {
        context.Response.StatusCode = error.Status;
        return context.Response.WriteAsJsonAsync(error.ToProblemDetails(), options: null, contentType: "application/problem+json");
    }
}
