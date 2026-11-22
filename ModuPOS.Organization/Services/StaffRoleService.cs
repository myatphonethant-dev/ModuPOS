namespace ModuPOS.Organization.Services;

using Microsoft.EntityFrameworkCore;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Organization.DTOs;
using ModuPOS.Organization.Interfaces;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

public class StaffRoleService : IStaffRoleService
{
    private readonly AppDbContext _context;

    public StaffRoleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<StaffRoleResponseDto>> GetStaffRolesAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) return PagedResult<StaffRoleResponseDto>.Failure("Request cannot be null");

        var query = _context.StaffRoles
            .AsNoTracking()
            .Where(r => !r.DeleteFlag);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(r => r.StaffRoleId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new StaffRoleResponseDto
            {
                StaffRoleId = r.StaffRoleId,
                StaffRoleCode = r.StaffRoleCode,
                StaffRoleName = r.StaffRoleName,
                Salary = r.Salary,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
        return PagedResult<StaffRoleResponseDto>.Success(items, pagination);
    }

    public async Task<Result<StaffRoleResponseDto>> GetStaffRoleByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var r = await _context.StaffRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.StaffRoleId == id && !x.DeleteFlag, cancellationToken);

        if (r is null)
            return Result<StaffRoleResponseDto>.Failure("Staff role not found.");

        return Result<StaffRoleResponseDto>.Success(new StaffRoleResponseDto
        {
            StaffRoleId = r.StaffRoleId,
            StaffRoleCode = r.StaffRoleCode,
            StaffRoleName = r.StaffRoleName,
            Salary = r.Salary,
            CreatedAt = r.CreatedAt
        });
    }

    public async Task<Result<StaffRoleResponseDto>> CreateStaffRoleAsync(CreateStaffRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.StaffRoleCode) || string.IsNullOrWhiteSpace(request.StaffRoleName))
            return Result<StaffRoleResponseDto>.Failure("Staff role code and name are required.");

        var exists = await _context.StaffRoles.AnyAsync(r => r.StaffRoleCode == request.StaffRoleCode && !r.DeleteFlag, cancellationToken);
        if (exists)
            return Result<StaffRoleResponseDto>.Failure($"Staff role code '{request.StaffRoleCode}' already exists.");

        var entity = new StaffRole
        {
            StaffRoleCode = request.StaffRoleCode,
            StaffRoleName = request.StaffRoleName,
            Salary = request.Salary,
            DeleteFlag = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.CreatedBy
        };

        _context.StaffRoles.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<StaffRoleResponseDto>.Success(new StaffRoleResponseDto
        {
            StaffRoleId = entity.StaffRoleId,
            StaffRoleCode = entity.StaffRoleCode,
            StaffRoleName = entity.StaffRoleName,
            Salary = entity.Salary,
            CreatedAt = entity.CreatedAt
        });
    }

    public async Task<Result<StaffRoleResponseDto>> UpdateStaffRoleAsync(int id, UpdateStaffRoleRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _context.StaffRoles.FirstOrDefaultAsync(r => r.StaffRoleId == id && !r.DeleteFlag, cancellationToken);
        if (entity is null)
            return Result<StaffRoleResponseDto>.Failure("Staff role not found.");

        entity.StaffRoleName = request.StaffRoleName;
        entity.Salary = request.Salary;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = request.UpdatedBy;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<StaffRoleResponseDto>.Success(new StaffRoleResponseDto
        {
            StaffRoleId = entity.StaffRoleId,
            StaffRoleCode = entity.StaffRoleCode,
            StaffRoleName = entity.StaffRoleName,
            Salary = entity.Salary,
            CreatedAt = entity.CreatedAt
        });
    }

    public async Task<Result> DeleteStaffRoleAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.StaffRoles.FirstOrDefaultAsync(r => r.StaffRoleId == id && !r.DeleteFlag, cancellationToken);
        if (entity is null)
            return Result.Failure("Staff role not found.");

        entity.DeleteFlag = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success("Staff role deleted successfully.");
    }
}
