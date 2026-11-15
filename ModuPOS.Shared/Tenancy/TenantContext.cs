namespace ModuPOS.Shared.Tenancy;

/// <summary>Scoped, request-level implementation filled in by the access pipeline.</summary>
public sealed class TenantContext : ITenantContext
{
    private static readonly IReadOnlySet<string> Empty = new HashSet<string>();

    public bool IsAuthenticated => UserId.HasValue && SessionId.HasValue;

    public Guid? UserId { get; set; }

    public Guid? ProfileId { get; set; }

    public string? ProfileType { get; set; }

    public Guid? BusinessId { get; set; }

    public Guid? BranchId { get; set; }

    public Guid? SessionId { get; set; }

    public IReadOnlySet<string> Permissions { get; set; } = Empty;
}
