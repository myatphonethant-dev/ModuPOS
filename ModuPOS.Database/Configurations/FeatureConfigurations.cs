using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModuPOS.FeatureManagement.Features.CustomerFeatures;
using ModuPOS.FeatureManagement.Features.FeatureRegistry;

namespace ModuPOS.Database.Configurations;

internal sealed class FeatureConfiguration : IEntityTypeConfiguration<Feature>
{
    public void Configure(EntityTypeBuilder<Feature> b)
    {
        b.ToTable("Features");
        b.Property(x => x.Code).HasMaxLength(100).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.HasIndex(x => x.Code).IsUnique();
    }
}

internal sealed class CustomerFeatureConfiguration : IEntityTypeConfiguration<CustomerFeature>
{
    public void Configure(EntityTypeBuilder<CustomerFeature> b)
    {
        b.ToTable("CustomerFeatures");
        b.HasIndex(x => new { x.CustomerId, x.FeatureId }).IsUnique();
        b.HasOne(x => x.Feature).WithMany().HasForeignKey(x => x.FeatureId).OnDelete(DeleteBehavior.Cascade);
    }
}
