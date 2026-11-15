using Microsoft.EntityFrameworkCore;
using ModuPOS.FeatureManagement.Features.CustomerFeatures;
using ModuPOS.FeatureManagement.Interfaces;

namespace ModuPOS.Database;

internal sealed class CustomerFeatureRepository(ModuPOSDbContext db) : ICustomerFeatureRepository
{
    public Task<CustomerFeature?> GetAsync(
        Guid customerId,
        string featureCode,
        CancellationToken cancellationToken = default) =>
        db.Set<CustomerFeature>()
            .AsNoTracking()
            .Include(x => x.Feature)
            .FirstOrDefaultAsync(
                x => x.CustomerId == customerId && x.Feature!.Code == featureCode,
                cancellationToken);
}
