namespace ModuPOS.Organization.Services;

using Microsoft.EntityFrameworkCore;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Organization.DTOs;
using ModuPOS.Organization.Interfaces;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

public class StaffRoleService(AppDbContext context) : IStaffRoleService
{
    public async Task<PagedResult<StaffRoleResponseDto>> GetStaffRolesAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var query = context.StaffRoles
            .AsNoTracking()
            .Where(r => !r.DeleteFlag);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(r => r.StaffRoleId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new StaffRoleResponseDto(r.StaffRoleId, r.StaffRoleCode, r.StaffRoleName, r.Salary, r.CreatedAt))
            .ToListAsync(cancellationToken);

        var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
        return PagedResult<StaffRoleResponseDto>.Success(items, pagination);
    }

    public async Task<Result<StaffRoleResponseDto>> GetStaffRoleByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var r = await context.StaffRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.StaffRoleId == id && !x.DeleteFlag, cancellationToken);

        if (r is null)
            return Result<StaffRoleResponseDto>.Failure("Staff role not found.");

        return Result<StaffRoleResponseDto>.Success(new StaffRoleResponseDto(r.StaffRoleId, r.StaffRoleCode, r.StaffRoleName, r.Salary, r.CreatedAt));
    }

    public async Task<Result<StaffRoleResponseDto>> CreateStaffRoleAsync(CreateStaffRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.StaffRoleCode) || string.IsNullOrWhiteSpace(request.StaffRoleName))
            return Result<StaffRoleResponseDto>.Failure("Staff role code and name are required.");

        var exists = await context.StaffRoles.AnyAsync(r => r.StaffRoleCode == request.StaffRoleCode && !r.DeleteFlag, cancellationToken);
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

        context.StaffRoles.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Result<StaffRoleResponseDto>.Success(new StaffRoleResponseDto(entity.StaffRoleId, entity.StaffRoleCode, entity.StaffRoleName, entity.Salary, entity.CreatedAt));
    }

    public async Task<Result<StaffRoleResponseDto>> UpdateStaffRoleAsync(int id, UpdateStaffRoleRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await context.StaffRoles.FirstOrDefaultAsync(r => r.StaffRoleId == id && !r.DeleteFlag, cancellationToken);
        if (entity is null)
            return Result<StaffRoleResponseDto>.Failure("Staff role not found.");

        entity.StaffRoleName = request.StaffRoleName;
        entity.Salary = request.Salary;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = request.UpdatedBy;

        await context.SaveChangesAsync(cancellationToken);

        return Result<StaffRoleResponseDto>.Success(new StaffRoleResponseDto(entity.StaffRoleId, entity.StaffRoleCode, entity.StaffRoleName, entity.Salary, entity.CreatedAt));
    }

    public async Task<Result> DeleteStaffRoleAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await context.StaffRoles.FirstOrDefaultAsync(r => r.StaffRoleId == id && !r.DeleteFlag, cancellationToken);
        if (entity is null)
            return Result.Failure("Staff role not found.");

        entity.DeleteFlag = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success("Staff role deleted successfully.");
    }
}
