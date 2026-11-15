using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModuPOS.Auth.Accounts;
using ModuPOS.Organization.Entities;

namespace ModuPOS.Database.Configurations;

internal sealed class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> b)
    {
        b.ToTable("Businesses");
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
    }
}

internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> b)
    {
        b.ToTable("Branches");
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.HasIndex(x => new { x.BusinessId, x.Name }).IsUnique();
        b.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class StaffBranchAssignmentConfiguration : IEntityTypeConfiguration<StaffBranchAssignment>
{
    public void Configure(EntityTypeBuilder<StaffBranchAssignment> b)
    {
        b.ToTable("StaffBranchAssignments");
        b.HasKey(x => new { x.StaffProfileId, x.BranchId });
        b.HasIndex(x => x.BusinessId);
        b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Profile>().WithMany().HasForeignKey(x => x.StaffProfileId).OnDelete(DeleteBehavior.Restrict);
    }
}
