using ModuPOS.FeatureManagement.Features.FeatureRegistry;

namespace ModuPOS.FeatureManagement.Features.CustomerFeatures;

public class CustomerFeature
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CustomerId { get; set; }

    public Guid FeatureId { get; set; }

    public Feature? Feature { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? ExpiryDate { get; set; }
}