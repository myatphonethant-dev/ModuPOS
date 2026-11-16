using Microsoft.EntityFrameworkCore;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Organization.DTOs;
using ModuPOS.Shared.Results;

namespace ModuPOS.Organization.Services.Branch;

public interface IBranchService
{
    Task<Result<List<BranchResponseDto>>> GetBranchesAsync(CancellationToken cancellationToken = default);
    Task<Result<BranchResponseDto>> GetBranchByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<BranchResponseDto>> CreateBranchAsync(CreateBranchRequest request, CancellationToken cancellationToken = default);
    Task<Result<BranchResponseDto>> UpdateBranchAsync(int id, UpdateBranchRequest request, CancellationToken cancellationToken = default);
}

public class BranchService(AppDbContext context) : IBranchService
{
    public async Task<Result<List<BranchResponseDto>>> GetBranchesAsync(CancellationToken cancellationToken = default)
    {
        var branches = await context.Branches
            .AsNoTracking()
            .Select(b => new BranchResponseDto(b.BranchId, b.BranchCode, b.BranchName, b.Address, b.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(branches);
    }

    public async Task<Result<BranchResponseDto>> GetBranchByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var b = await context.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.BranchId == id, cancellationToken);

        if (b is null)
            return Result.Failure<BranchResponseDto>("Branch not found.");

        return Result.Success(new BranchResponseDto(b.BranchId, b.BranchCode, b.BranchName, b.Address, b.CreatedAt));
    }

    public async Task<Result<BranchResponseDto>> CreateBranchAsync(CreateBranchRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.BranchCode) || string.IsNullOrWhiteSpace(request.BranchName))
            return Result.Failure<BranchResponseDto>("Branch code and name are required.");

        var exists = await context.Branches.AnyAsync(b => b.BranchCode == request.BranchCode, cancellationToken);
        if (exists)
            return Result.Failure<BranchResponseDto>($"Branch with code '{request.BranchCode}' already exists.");

        var entity = new ModuPOS.Database.AppDbContextModels.Branch
        {
            BranchCode = request.BranchCode,
            BranchName = request.BranchName,
            Address = request.Address,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.CreatedBy
        };

        context.Branches.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new BranchResponseDto(entity.BranchId, entity.BranchCode, entity.BranchName, entity.Address, entity.CreatedAt));
    }

    public async Task<Result<BranchResponseDto>> UpdateBranchAsync(int id, UpdateBranchRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await context.Branches.FirstOrDefaultAsync(b => b.BranchId == id, cancellationToken);
        if (entity is null)
            return Result.Failure<BranchResponseDto>("Branch not found.");

        entity.BranchName = request.BranchName;
        entity.Address = request.Address;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = request.UpdatedBy;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new BranchResponseDto(entity.BranchId, entity.BranchCode, entity.BranchName, entity.Address, entity.CreatedAt));
    }
}
