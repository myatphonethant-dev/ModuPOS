using Microsoft.EntityFrameworkCore;
using ModuPOS.Organization.Entities;

namespace ModuPOS.Organization.Services;

public enum BranchAccess
{
    Allowed,
    NotAssigned,
    Inactive,
    OtherBusiness
}

public sealed record BranchSummary(Guid BranchId, string Name);

public interface IBranchAccessService
{
    /// <summary>Active branches the staff member is assigned to, by name.</summary>
    Task<IReadOnlyList<BranchSummary>> ActiveAssignedAsync(Guid staffProfileId, Guid businessId, CancellationToken ct = default);

    /// <summary>Whether the branch is in the business, active, and assigned to the staff member (FR-030).</summary>
    Task<BranchAccess> CheckAsync(Guid staffProfileId, Guid businessId, Guid branchId, CancellationToken ct = default);
}

/// <summary>
/// Branch rules for staff. Queries filter by business explicitly so they work before the tenant
/// context is set; callers cache results against the business's authorization version.
/// </summary>
internal sealed class BranchAccessService(DbContext db) : IBranchAccessService
{
    public async Task<IReadOnlyList<BranchSummary>> ActiveAssignedAsync(Guid staffProfileId, Guid businessId, CancellationToken ct = default) =>
        await (
                from s in db.Set<StaffBranchAssignment>().IgnoreQueryFilters().AsNoTracking()
                where s.StaffProfileId == staffProfileId && s.BusinessId == businessId
                join b in db.Set<Branch>().IgnoreQueryFilters().AsNoTracking() on s.BranchId equals b.Id
                where b.BusinessId == businessId && b.Status == BranchStatus.Active
                orderby b.Name
                select new BranchSummary(b.Id, b.Name))
            .ToListAsync(ct);

    public async Task<BranchAccess> CheckAsync(Guid staffProfileId, Guid businessId, Guid branchId, CancellationToken ct = default)
    {
        var branch = await db.Set<Branch>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => b.Id == branchId)
            .Select(b => new { b.BusinessId, b.Status })
            .FirstOrDefaultAsync(ct);

        if (branch is null)
            return BranchAccess.NotAssigned;

        if (branch.BusinessId != businessId)
            return BranchAccess.OtherBusiness;

        if (branch.Status != BranchStatus.Active)
            return BranchAccess.Inactive;

        var assigned = await db.Set<StaffBranchAssignment>()
            .IgnoreQueryFilters()
            .AnyAsync(s => s.StaffProfileId == staffProfileId && s.BranchId == branchId && s.BusinessId == businessId, ct);

        return assigned ? BranchAccess.Allowed : BranchAccess.NotAssigned;
    }
}
