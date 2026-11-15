using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModuPOS.Auth.Accounts;
using ModuPOS.Auth.Auditing;
using ModuPOS.Auth.Roles;
using ModuPOS.Auth.Sessions;
using ModuPOS.Organization.Entities;

namespace ModuPOS.Database.Configurations;

internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> b)
    {
        b.ToTable("UserAccounts", t =>
            t.HasCheckConstraint("CK_UserAccounts_Username_Length", "LEN([Username]) BETWEEN 3 AND 50"));
        b.Property(x => x.Username).HasMaxLength(50).IsRequired();
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        b.HasIndex(x => x.Username).IsUnique();
        b.HasIndex(x => x.Email).IsUnique();
        b.HasMany(x => x.Profiles).WithOne().HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> b)
    {
        b.ToTable("Profiles", t => t.HasCheckConstraint(
            "CK_Profiles_Business",
            "([Type] = 'PlatformAdmin' AND [BusinessId] IS NULL) OR ([Type] <> 'PlatformAdmin' AND [BusinessId] IS NOT NULL)"));
        // One profile of each type per account and business; NULL BusinessId counts as a value.
        b.HasIndex(x => new { x.UserAccountId, x.Type, x.BusinessId }).IsUnique().HasFilter(null);
        b.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> b)
    {
        b.ToTable("Sessions");
        b.Property(x => x.SourceAddress).HasMaxLength(64);
        b.Property(x => x.DeviceDescription).HasMaxLength(512);
        b.HasIndex(x => new { x.UserAccountId, x.Status });
        b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");
        b.Property(x => x.TokenHash).HasMaxLength(32).IsFixedLength().IsRequired();
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasOne(x => x.Session).WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.ToTable("Permissions");
        b.HasKey(x => x.Code);
        b.Property(x => x.Code).HasMaxLength(100);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Group).HasMaxLength(100).IsRequired();
        b.Property(x => x.FeatureCode).HasMaxLength(100);
        b.PrimitiveCollection(x => x.DependsOn);
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("Roles", t =>
            t.HasCheckConstraint("CK_Roles_Name_Length", "LEN([Name]) BETWEEN 2 AND 60"));
        b.Property(x => x.Name).HasMaxLength(60).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.Version).IsRowVersion();
        b.HasIndex(x => new { x.BusinessId, x.Name }).IsUnique();
        // At most one Owner role per business.
        b.HasIndex(x => x.BusinessId).IsUnique().HasFilter("[IsOwner] = 1").HasDatabaseName("UX_Roles_OneOwnerPerBusiness");
        b.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Permissions).WithOne().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("RolePermissions");
        b.HasKey(x => new { x.RoleId, x.PermissionCode });
        b.Property(x => x.PermissionCode).HasMaxLength(100);
        b.HasOne<Permission>().WithMany().HasForeignKey(x => x.PermissionCode).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RoleAssignmentConfiguration : IEntityTypeConfiguration<RoleAssignment>
{
    public void Configure(EntityTypeBuilder<RoleAssignment> b)
    {
        b.ToTable("RoleAssignments");
        // NULL BranchId (business scope) must also be unique, so no default IS NOT NULL filter.
        b.HasIndex(x => new { x.StaffProfileId, x.RoleId, x.BranchId }).IsUnique().HasFilter(null);
        b.HasIndex(x => new { x.BusinessId, x.RoleId });
        b.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Profile>().WithMany().HasForeignKey(x => x.StaffProfileId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AuthorizationVersionConfiguration : IEntityTypeConfiguration<AuthorizationVersion>
{
    public void Configure(EntityTypeBuilder<AuthorizationVersion> b)
    {
        b.ToTable("AuthorizationVersions");
        b.HasKey(x => x.BusinessId);
        b.Property(x => x.BusinessId).ValueGeneratedNever();
        b.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AuditEntryRecordConfiguration : IEntityTypeConfiguration<AuditEntryRecord>
{
    public void Configure(EntityTypeBuilder<AuditEntryRecord> b)
    {
        b.ToTable("AuditEntries", t =>
        {
            t.HasCheckConstraint("CK_AuditEntries_Details_Json", "ISJSON([Details]) = 1");
            t.HasCheckConstraint("CK_AuditEntries_RepeatCount", "[RepeatCount] >= 1");
        });
        b.Property(x => x.Code).HasMaxLength(64).IsRequired();
        b.Property(x => x.TargetType).HasMaxLength(64);
        b.Property(x => x.TargetId).HasMaxLength(128);
        b.Property(x => x.SourceAddress).HasMaxLength(64);
        b.Property(x => x.DeviceDescription).HasMaxLength(512);
        b.Property(x => x.DedupKey).HasMaxLength(128);
        b.Property(x => x.Details).HasColumnType("nvarchar(max)").IsRequired();
        b.HasIndex(x => new { x.BusinessId, x.OccurredAt }).IsDescending(false, true);
        b.HasIndex(x => new { x.Code, x.OccurredAt });
        b.HasIndex(x => new { x.ActorUserId, x.OccurredAt });
    }
}
