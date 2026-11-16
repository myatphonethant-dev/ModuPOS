using Microsoft.EntityFrameworkCore;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Organization.DTOs;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

namespace ModuPOS.Organization.Services.Branch;

/// <summary>
/// Defines business operations for managing store branches.
/// </summary>
public interface IBranchService
{
    /// <summary>
    /// Retrieves a paginated list of branches.
    /// </summary>
    /// <param name="request">The pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of branch response DTOs.</returns>
    Task<PagedResult<BranchResponseDto>> GetBranchesAsync(PaginationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a branch by its identifier.
    /// </summary>
    /// <param name="id">The branch primary key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The branch response DTO if found; otherwise failure.</returns>
    Task<Result<BranchResponseDto>> GetBranchByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new branch.
    /// </summary>
    /// <param name="request">The branch creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created branch response DTO if successful; otherwise failure.</returns>
    Task<Result<BranchResponseDto>> CreateBranchAsync(CreateBranchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing branch.
    /// </summary>
    /// <param name="id">The branch primary key.</param>
    /// <param name="request">The branch update details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated branch response DTO if successful; otherwise failure.</returns>
    Task<Result<BranchResponseDto>> UpdateBranchAsync(int id, UpdateBranchRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides EF Core implementation for <see cref="IBranchService"/>.
/// </summary>
public class BranchService(AppDbContext context) : IBranchService
{
    /// <inheritdoc />
    public async Task<PagedResult<BranchResponseDto>> GetBranchesAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var query = context.Branches.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(b => b.BranchId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(b => new BranchResponseDto(b.BranchId, b.BranchCode, b.BranchName, b.Address, b.CreatedAt))
            .ToListAsync(cancellationToken);

        var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
        return PagedResult<BranchResponseDto>.Success(items, pagination);
    }

    /// <inheritdoc />
    public async Task<Result<BranchResponseDto>> GetBranchByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var b = await context.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.BranchId == id, cancellationToken);

        if (b is null)
            return Result<BranchResponseDto>.Failure("Branch not found.");

        return Result<BranchResponseDto>.Success(new BranchResponseDto(b.BranchId, b.BranchCode, b.BranchName, b.Address, b.CreatedAt));
    }

    /// <inheritdoc />
    public async Task<Result<BranchResponseDto>> CreateBranchAsync(CreateBranchRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.BranchCode) || string.IsNullOrWhiteSpace(request.BranchName))
            return Result<BranchResponseDto>.Failure("Branch code and name are required.");

        var exists = await context.Branches.AnyAsync(b => b.BranchCode == request.BranchCode, cancellationToken);
        if (exists)
            return Result<BranchResponseDto>.Failure($"Branch with code '{request.BranchCode}' already exists.");

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

        return Result<BranchResponseDto>.Success(new BranchResponseDto(entity.BranchId, entity.BranchCode, entity.BranchName, entity.Address, entity.CreatedAt));
    }

    /// <inheritdoc />
    public async Task<Result<BranchResponseDto>> UpdateBranchAsync(int id, UpdateBranchRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await context.Branches.FirstOrDefaultAsync(b => b.BranchId == id, cancellationToken);
        if (entity is null)
            return Result<BranchResponseDto>.Failure("Branch not found.");

        entity.BranchName = request.BranchName;
        entity.Address = request.Address;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = request.UpdatedBy;

        await context.SaveChangesAsync(cancellationToken);

        return Result<BranchResponseDto>.Success(new BranchResponseDto(entity.BranchId, entity.BranchCode, entity.BranchName, entity.Address, entity.CreatedAt));
    }
}
