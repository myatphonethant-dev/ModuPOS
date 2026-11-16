using Microsoft.EntityFrameworkCore;
using ModuPOS.Auth.DTOs;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Shared.Results;

namespace ModuPOS.Auth.Services.Role;

public interface IRoleService
{
    Task<Result<List<RoleDto>>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<Result<RoleDto>> GetRoleByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<RoleDto>> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result<RoleDto>> UpdateRoleAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteRoleAsync(int id, CancellationToken cancellationToken = default);
}

public class RoleService(AppDbContext context) : IRoleService
{
    public async Task<Result<List<RoleDto>>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await context.Roles
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .ToListAsync(cancellationToken);

        var rolePermissions = await (
            from rp in context.RolePermissions.AsNoTracking().Where(x => !x.IsDeleted)
            join p in context.Permissions.AsNoTracking().Where(x => !x.IsDeleted) on rp.PermissionId equals p.Id
            select new { rp.RoleId, p.Id, p.Name }
        ).ToListAsync(cancellationToken);

        var list = roles.Select(r =>
        {
            var perms = rolePermissions.Where(rp => rp.RoleId == r.Id).ToList();
            return new RoleDto(
                r.Id,
                r.Name,
                perms.Select(p => p.Id).ToList(),
                perms.Select(p => p.Name).ToList());
        }).ToList();

        return Result.Success(list);
    }

    public async Task<Result<RoleDto>> GetRoleByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var r = await context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (r is null)
            return Result.Failure<RoleDto>("Role not found.");

        var perms = await (
            from rp in context.RolePermissions.AsNoTracking().Where(x => x.RoleId == id && !x.IsDeleted)
            join p in context.Permissions.AsNoTracking().Where(x => !x.IsDeleted) on rp.PermissionId equals p.Id
            select new { p.Id, p.Name }
        ).ToListAsync(cancellationToken);

        return Result.Success(new RoleDto(
            r.Id,
            r.Name,
            perms.Select(p => p.Id).ToList(),
            perms.Select(p => p.Name).ToList()));
    }

    public async Task<Result<RoleDto>> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure<RoleDto>("Role name is required.");

        var exists = await context.Roles.AnyAsync(r => r.Name == request.Name && !r.IsDeleted, cancellationToken);
        if (exists)
            return Result.Failure<RoleDto>($"Role '{request.Name}' already exists.");

        var role = new ModuPOS.Database.AppDbContextModels.Role
        {
            Name = request.Name,
            IsDeleted = false
        };

        context.Roles.Add(role);
        await context.SaveChangesAsync(cancellationToken);

        if (request.PermissionIds != null && request.PermissionIds.Count > 0)
        {
            foreach (var permId in request.PermissionIds.Distinct())
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permId,
                    IsDeleted = false
                });
            }
            await context.SaveChangesAsync(cancellationToken);
        }

        return await GetRoleByIdAsync(role.Id, cancellationToken);
    }

    public async Task<Result<RoleDto>> UpdateRoleAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
        if (role is null)
            return Result.Failure<RoleDto>("Role not found.");

        var nameInUse = await context.Roles.AnyAsync(r => r.Id != id && r.Name == request.Name && !r.IsDeleted, cancellationToken);
        if (nameInUse)
            return Result.Failure<RoleDto>($"Role '{request.Name}' already exists.");

        role.Name = request.Name;

        if (request.PermissionIds != null)
        {
            // Remove existing role permissions
            var currentPerms = await context.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync(cancellationToken);
            context.RolePermissions.RemoveRange(currentPerms);

            // Add new permissions
            foreach (var permId in request.PermissionIds.Distinct())
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = id,
                    PermissionId = permId,
                    IsDeleted = false
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return await GetRoleByIdAsync(id, cancellationToken);
    }

    public async Task<Result> DeleteRoleAsync(int id, CancellationToken cancellationToken = default)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
        if (role is null)
            return Result.Failure("Role not found.");

        var userInRole = await context.Users.AnyAsync(u => u.RoleId == id && !u.IsDeleted, cancellationToken);
        if (userInRole)
            return Result.Failure("Cannot delete role because it is currently assigned to one or more active users.");

        role.IsDeleted = true;
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
