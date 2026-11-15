using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ModuPOS.Auth.Accounts;
using ModuPOS.Auth.Auditing;
using ModuPOS.Auth.Roles;
using ModuPOS.Auth.Sessions;
using ModuPOS.Organization.Entities;
using ModuPOS.Shared.Auditing;
using ModuPOS.Shared.Tenancy;

namespace ModuPOS.Database;

/// <summary>
/// Single database for all modules. Tenant-owned entities are filtered to the caller's business;
/// a request with no business sees no tenant rows. Background jobs use IgnoreQueryFilters explicitly.
/// </summary>
public sealed class ModuPOSDbContext(
    DbContextOptions<ModuPOSDbContext> options,
    ITenantContext tenant) : DbContext(options)
{
    public const string CaseInsensitiveCollation = "SQL_Latin1_General_CP1_CI_AS";

    /// <summary>Read by query filters; EF parameterizes it per query.</summary>
    private Guid? CurrentBusinessId => tenant.BusinessId;

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        builder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();

        builder.Properties<AccountStatus>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<ProfileType>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<ProfileStatus>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<SessionStatus>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<SessionRevokedReason>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<PermissionScope>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<BusinessStatus>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<BranchStatus>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<AuditSeverity>().HaveConversion<string>().HaveMaxLength(16);
        builder.Properties<AuditOutcome>().HaveConversion<string>().HaveMaxLength(16);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation(CaseInsensitiveCollation);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ModuPOSDbContext).Assembly);

        modelBuilder.Entity<Branch>().HasQueryFilter(e => e.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<StaffBranchAssignment>().HasQueryFilter(e => e.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<Role>().HasQueryFilter(e => e.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<RoleAssignment>().HasQueryFilter(e => e.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<AuthorizationVersion>().HasQueryFilter(e => e.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<AuditEntryRecord>().HasQueryFilter(e => e.BusinessId == CurrentBusinessId);
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private sealed class NullableUtcDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
        v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v : v.Value.ToUniversalTime()) : v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
}
