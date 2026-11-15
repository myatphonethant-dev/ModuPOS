namespace ModuPOS.Auth.Authorization;

/// <summary>Endpoint needs a valid access token and an active session (pipeline step 1 only).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireSignedInAttribute : Attribute;

/// <summary>Endpoint needs the given business permission in the caller's effective permissions.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(string code) : RequireSignedInAttribute
{
    public string Code { get; } = code;
}

/// <summary>Endpoint acts on the active branch; the branch is re-validated per request (US6).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class BranchScopedAttribute : Attribute;

/// <summary>Endpoint needs the given platform permission; business checks do not apply.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequirePlatformPermissionAttribute(string code) : RequireSignedInAttribute
{
    public string Code { get; } = code;
}
