using Microsoft.EntityFrameworkCore;
using ModuPOS.Auth.DTOs;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

namespace ModuPOS.Auth.Services.Role;

/// <summary>
/// Defines business operations for managing security roles and permission mappings.
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Retrieves a paginated list of non-deleted roles.
    /// </summary>
    /// <param name="request">The pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of role DTOs.</returns>
    Task<PagedResult<RoleDto>> GetRolesAsync(PaginationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a role by its identifier including its permissions.
    /// </summary>
    /// <param name="id">The role primary key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The role DTO if found; otherwise failure.</returns>
    Task<Result<RoleDto>> GetRoleByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new role with optional permission assignments.
    /// </summary>
    /// <param name="request">The role creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created role DTO if successful; otherwise failure.</returns>
    Task<Result<RoleDto>> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing role and its permission assignments.
    /// </summary>
    /// <param name="id">The role primary key.</param>
    /// <param name="request">The role update details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated role DTO if successful; otherwise failure.</returns>
    Task<Result<RoleDto>> UpdateRoleAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a role and its permission mappings.
    /// </summary>
    /// <param name="id">The role primary key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> DeleteRoleAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides EF Core implementation for <see cref="IRoleService"/>.
/// </summary>
public class RoleService(AppDbContext context) : IRoleService
{
    /// <inheritdoc />
    public async Task<PagedResult<RoleDto>> GetRolesAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var query = context.Roles
            .AsNoTracking()
            .Where(r => !r.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedRoles = await query
            .OrderBy(r => r.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var pagedRoleIds = pagedRoles.Select(r => r.Id).ToList();

        var rolePermissions = await (
            from rp in context.RolePermissions.AsNoTracking().Where(x => !x.IsDeleted)
            join p in context.Permissions.AsNoTracking().Where(x => !x.IsDeleted) on rp.PermissionId equals p.Id
            where pagedRoleIds.Contains(rp.RoleId)
            select new { rp.RoleId, p.Id, p.Name }
        ).ToListAsync(cancellationToken);

        var list = pagedRoles.Select(r =>
        {
            var perms = rolePermissions.Where(rp => rp.RoleId == r.Id).ToList();
            return new RoleDto(
                r.Id,
                r.Name,
                perms.Select(p => p.Id).ToList(),
                perms.Select(p => p.Name).ToList());
        }).ToList();

        var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
        return PagedResult<RoleDto>.Success(list, pagination);
    }

    /// <inheritdoc />
    public async Task<Result<RoleDto>> GetRoleByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var r = await context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (r is null)
            return Result<RoleDto>.Failure("Role not found.");

        var perms = await (
            from rp in context.RolePermissions.AsNoTracking().Where(x => x.RoleId == id && !x.IsDeleted)
            join p in context.Permissions.AsNoTracking().Where(x => !x.IsDeleted) on rp.PermissionId equals p.Id
            select new { p.Id, p.Name }
        ).ToListAsync(cancellationToken);

        return Result<RoleDto>.Success(new RoleDto(
            r.Id,
            r.Name,
            perms.Select(p => p.Id).ToList(),
            perms.Select(p => p.Name).ToList()));
    }

    /// <inheritdoc />
    public async Task<Result<RoleDto>> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<RoleDto>.Failure("Role name is required.");

        var exists = await context.Roles.AnyAsync(r => r.Name == request.Name && !r.IsDeleted, cancellationToken);
        if (exists)
            return Result<RoleDto>.Failure($"Role '{request.Name}' already exists.");

        var role = new ModuPOS.Database.AppDbContextModels.Role
        {
            Name = request.Name,
            IsDeleted = false
        };

        context.Roles.Add(role);
        await context.SaveChangesAsync(cancellationToken);

        var permissionIds = new List<int>();
        var permissionNames = new List<string>();

        if (request.PermissionIds is { Count: > 0 })
        {
            var validPerms = await context.Permissions
                .Where(p => request.PermissionIds.Contains(p.Id) && !p.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var perm in validPerms)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = perm.Id,
                    IsDeleted = false
                });
                permissionIds.Add(perm.Id);
                permissionNames.Add(perm.Name);
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        return Result<RoleDto>.Success(new RoleDto(role.Id, role.Name, permissionIds, permissionNames));
    }

    /// <inheritdoc />
    public async Task<Result<RoleDto>> UpdateRoleAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
        if (role is null)
            return Result<RoleDto>.Failure("Role not found.");

        var nameInUse = await context.Roles.AnyAsync(r => r.Id != id && r.Name == request.Name && !r.IsDeleted, cancellationToken);
        if (nameInUse)
            return Result<RoleDto>.Failure($"Role '{request.Name}' already exists.");

        role.Name = request.Name;

        // Soft-delete existing role permissions
        var existingPerms = await context.RolePermissions
            .Where(rp => rp.RoleId == id && !rp.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var rp in existingPerms)
        {
            rp.IsDeleted = true;
        }

        var permissionIds = new List<int>();
        var permissionNames = new List<string>();

        if (request.PermissionIds is { Count: > 0 })
        {
            var validPerms = await context.Permissions
                .Where(p => request.PermissionIds.Contains(p.Id) && !p.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var perm in validPerms)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = perm.Id,
                    IsDeleted = false
                });
                permissionIds.Add(perm.Id);
                permissionNames.Add(perm.Name);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result<RoleDto>.Success(new RoleDto(role.Id, role.Name, permissionIds, permissionNames));
    }

    /// <inheritdoc />
    public async Task<Result> DeleteRoleAsync(int id, CancellationToken cancellationToken = default)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
        if (role is null)
            return Result.Failure("Role not found.");

        var inUse = await context.Users.AnyAsync(u => u.RoleId == id && !u.IsDeleted, cancellationToken);
        if (inUse)
            return Result.Failure("Cannot delete role that is currently assigned to users.");

        role.IsDeleted = true;

        var existingPerms = await context.RolePermissions
            .Where(rp => rp.RoleId == id && !rp.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var rp in existingPerms)
        {
            rp.IsDeleted = true;
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success("Role deleted successfully.");
    }
}
