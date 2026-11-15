namespace ModuPOS.Shared.Tenancy;

/// <summary>Who is calling and in which business/branch, populated by the access pipeline.</summary>
public interface ITenantContext
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    Guid? ProfileId { get; }

    string? ProfileType { get; }

    Guid? BusinessId { get; }

    Guid? BranchId { get; }

    Guid? SessionId { get; }

    IReadOnlySet<string> Permissions { get; }
}

/// <summary>Marks an entity owned by one business; filtered by the tenant query filter.</summary>
public interface ITenantOwned
{
    Guid BusinessId { get; }
}
