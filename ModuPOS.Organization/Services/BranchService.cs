using Microsoft.EntityFrameworkCore;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Organization.DTOs;
using ModuPOS.Organization.Interfaces;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

namespace ModuPOS.Organization.Services;

public class BranchService : IBranchService
{
    private readonly AppDbContext _context;

    public BranchService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<BranchResponseDto>> GetBranchesAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) return PagedResult<BranchResponseDto>.Failure("Request cannot be null");

        var query = _context.Branches.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(b => b.BranchId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(b => new BranchResponseDto
            {
                BranchId = b.BranchId,
                BranchCode = b.BranchCode,
                BranchName = b.BranchName,
                Address = b.Address,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
        return PagedResult<BranchResponseDto>.Success(items, pagination);
    }

    public async Task<Result<BranchResponseDto>> GetBranchByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var b = await _context.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.BranchId == id, cancellationToken);

        if (b is null)
            return Result<BranchResponseDto>.Failure("Branch not found.");

        return Result<BranchResponseDto>.Success(new BranchResponseDto
        {
            BranchId = b.BranchId,
            BranchCode = b.BranchCode,
            BranchName = b.BranchName,
            Address = b.Address,
            CreatedAt = b.CreatedAt
        });
    }

    public async Task<Result<BranchResponseDto>> CreateBranchAsync(CreateBranchRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.BranchCode) || string.IsNullOrWhiteSpace(request.BranchName))
            return Result<BranchResponseDto>.Failure("Branch code and name are required.");

        var exists = await _context.Branches.AnyAsync(b => b.BranchCode == request.BranchCode, cancellationToken);
        if (exists)
            return Result<BranchResponseDto>.Failure($"Branch with code '{request.BranchCode}' already exists.");

        var entity = new Branch
        {
            BranchCode = request.BranchCode,
            BranchName = request.BranchName,
            Address = request.Address,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.CreatedBy
        };

        _context.Branches.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<BranchResponseDto>.Success(new BranchResponseDto
        {
            BranchId = entity.BranchId,
            BranchCode = entity.BranchCode,
            BranchName = entity.BranchName,
            Address = entity.Address,
            CreatedAt = entity.CreatedAt
        });
    }

    public async Task<Result<BranchResponseDto>> UpdateBranchAsync(int id, UpdateBranchRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Branches.FirstOrDefaultAsync(b => b.BranchId == id, cancellationToken);
        if (entity is null)
            return Result<BranchResponseDto>.Failure("Branch not found.");

        entity.BranchName = request.BranchName;
        entity.Address = request.Address;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = request.UpdatedBy;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<BranchResponseDto>.Success(new BranchResponseDto
        {
            BranchId = entity.BranchId,
            BranchCode = entity.BranchCode,
            BranchName = entity.BranchName,
            Address = entity.Address,
            CreatedAt = entity.CreatedAt
        });
    }
}