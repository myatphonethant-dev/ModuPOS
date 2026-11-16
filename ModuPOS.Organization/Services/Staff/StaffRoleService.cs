using Microsoft.EntityFrameworkCore;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Organization.DTOs;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

namespace ModuPOS.Organization.Services.Staff;

/// <summary>
/// Defines business operations for managing organizational staff roles and compensation bands.
/// </summary>
public interface IStaffRoleService
{
    /// <summary>
    /// Retrieves a paginated list of non-deleted staff roles.
    /// </summary>
    /// <param name="request">The pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of staff role response DTOs.</returns>
    Task<PagedResult<StaffRoleResponseDto>> GetStaffRolesAsync(PaginationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a staff role by identifier.
    /// </summary>
    /// <param name="id">The staff role primary key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The staff role response DTO if found; otherwise failure.</returns>
    Task<Result<StaffRoleResponseDto>> GetStaffRoleByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new staff role definition.
    /// </summary>
    /// <param name="request">The staff role creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created staff role response DTO if successful; otherwise failure.</returns>
    Task<Result<StaffRoleResponseDto>> CreateStaffRoleAsync(CreateStaffRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing staff role definition.
    /// </summary>
    /// <param name="id">The staff role primary key.</param>
    /// <param name="request">The staff role update details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated staff role response DTO if successful; otherwise failure.</returns>
    Task<Result<StaffRoleResponseDto>> UpdateStaffRoleAsync(int id, UpdateStaffRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a staff role by setting DeleteFlag to true.
    /// </summary>
    /// <param name="id">The staff role primary key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> DeleteStaffRoleAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides EF Core implementation for <see cref="IStaffRoleService"/>.
/// </summary>
public class StaffRoleService(AppDbContext context) : IStaffRoleService
{
    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<Result<StaffRoleResponseDto>> GetStaffRoleByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var r = await context.StaffRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.StaffRoleId == id && !x.DeleteFlag, cancellationToken);

        if (r is null)
            return Result<StaffRoleResponseDto>.Failure("Staff role not found.");

        return Result<StaffRoleResponseDto>.Success(new StaffRoleResponseDto(r.StaffRoleId, r.StaffRoleCode, r.StaffRoleName, r.Salary, r.CreatedAt));
    }

    /// <inheritdoc />
    public async Task<Result<StaffRoleResponseDto>> CreateStaffRoleAsync(CreateStaffRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.StaffRoleCode) || string.IsNullOrWhiteSpace(request.StaffRoleName))
            return Result<StaffRoleResponseDto>.Failure("Staff role code and name are required.");

        var exists = await context.StaffRoles.AnyAsync(r => r.StaffRoleCode == request.StaffRoleCode && !r.DeleteFlag, cancellationToken);
        if (exists)
            return Result<StaffRoleResponseDto>.Failure($"Staff role code '{request.StaffRoleCode}' already exists.");

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

        return Result<StaffRoleResponseDto>.Success(new StaffRoleResponseDto(entity.StaffRoleId, entity.StaffRoleCode, entity.StaffRoleName, entity.Salary, entity.CreatedAt));
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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
