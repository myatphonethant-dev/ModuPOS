using Microsoft.EntityFrameworkCore;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Organization.DTOs;
using ModuPOS.Shared.Results;

namespace ModuPOS.Organization.Services.Staff;

public interface IStaffRoleService
{
    Task<Result<List<StaffRoleResponseDto>>> GetStaffRolesAsync(CancellationToken cancellationToken = default);
    Task<Result<StaffRoleResponseDto>> CreateStaffRoleAsync(CreateStaffRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result<StaffRoleResponseDto>> UpdateStaffRoleAsync(int id, UpdateStaffRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteStaffRoleAsync(int id, CancellationToken cancellationToken = default);
}

public class StaffRoleService(AppDbContext context) : IStaffRoleService
{
    public async Task<Result<List<StaffRoleResponseDto>>> GetStaffRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await context.StaffRoles
            .AsNoTracking()
            .Where(r => !r.DeleteFlag)
            .Select(r => new StaffRoleResponseDto(r.StaffRoleId, r.StaffRoleCode, r.StaffRoleName, r.Salary, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(roles);
    }

    public async Task<Result<StaffRoleResponseDto>> CreateStaffRoleAsync(CreateStaffRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.StaffRoleCode) || string.IsNullOrWhiteSpace(request.StaffRoleName))
            return Result.Failure<StaffRoleResponseDto>("Staff role code and name are required.");

        var exists = await context.StaffRoles.AnyAsync(r => r.StaffRoleCode == request.StaffRoleCode && !r.DeleteFlag, cancellationToken);
        if (exists)
            return Result.Failure<StaffRoleResponseDto>($"Staff role code '{request.StaffRoleCode}' already exists.");

        var entity = new ModuPOS.Database.AppDbContextModels.StaffRole
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

        return Result.Success(new StaffRoleResponseDto(entity.StaffRoleId, entity.StaffRoleCode, entity.StaffRoleName, entity.Salary, entity.CreatedAt));
    }

    public async Task<Result<StaffRoleResponseDto>> UpdateStaffRoleAsync(int id, UpdateStaffRoleRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await context.StaffRoles.FirstOrDefaultAsync(r => r.StaffRoleId == id && !r.DeleteFlag, cancellationToken);
        if (entity is null)
            return Result.Failure<StaffRoleResponseDto>("Staff role not found.");

        entity.StaffRoleName = request.StaffRoleName;
        entity.Salary = request.Salary;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = request.UpdatedBy;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new StaffRoleResponseDto(entity.StaffRoleId, entity.StaffRoleCode, entity.StaffRoleName, entity.Salary, entity.CreatedAt));
    }

    public async Task<Result> DeleteStaffRoleAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await context.StaffRoles.FirstOrDefaultAsync(r => r.StaffRoleId == id && !r.DeleteFlag, cancellationToken);
        if (entity is null)
            return Result.Failure("Staff role not found.");

        entity.DeleteFlag = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
