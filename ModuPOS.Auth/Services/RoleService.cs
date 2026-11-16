namespace ModuPOS.Auth.Services;

using Microsoft.EntityFrameworkCore;
using ModuPOS.Auth.DTOs;
using ModuPOS.Auth.Interfaces;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

public class RoleService(AppDbContext context) : IRoleService
{
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

    public async Task<Result<RoleDto>> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<RoleDto>.Failure("Role name is required.");

        var exists = await context.Roles.AnyAsync(r => r.Name == request.Name && !r.IsDeleted, cancellationToken);
        if (exists)
            return Result<RoleDto>.Failure($"Role '{request.Name}' already exists.");

        var role = new Role
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

    public async Task<Result<RoleDto>> UpdateRoleAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
        if (role is null)
            return Result<RoleDto>.Failure("Role not found.");

        var idsToAdd = (request.PermissionIdsToAdd ?? [])
            .Distinct()
            .ToList();
        var idsToRemove = (request.PermissionIdsToRemove ?? [])
            .Distinct()
            .ToList();

        var overlappingIds = idsToAdd.Intersect(idsToRemove).ToList();
        if (overlappingIds.Count > 0)
            return Result<RoleDto>.Failure("Permission IDs cannot appear in both add and remove lists.");

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Result<RoleDto>.Failure("Role name is required.");

            var name = request.Name.Trim();
            var nameInUse = await context.Roles.AnyAsync(
                r => r.Id != id && r.Name == name && !r.IsDeleted,
                cancellationToken);
            if (nameInUse)
                return Result<RoleDto>.Failure($"Role '{name}' already exists.");

            role.Name = name;
        }

        if (idsToAdd.Count > 0 || idsToRemove.Count > 0)
        {
            if (idsToAdd.Count > 0)
            {
                var validAddCount = await context.Permissions
                    .CountAsync(p => idsToAdd.Contains(p.Id) && !p.IsDeleted, cancellationToken);
                if (validAddCount != idsToAdd.Count)
                    return Result<RoleDto>.Failure("One or more permissions to add were not found.");
            }

            if (idsToRemove.Count > 0)
            {
                var validRemoveCount = await context.Permissions
                    .CountAsync(p => idsToRemove.Contains(p.Id) && !p.IsDeleted, cancellationToken);
                if (validRemoveCount != idsToRemove.Count)
                    return Result<RoleDto>.Failure("One or more permissions to remove were not found.");
            }

            var existingLinks = await context.RolePermissions
                .Where(rp => rp.RoleId == id)
                .ToListAsync(cancellationToken);

            foreach (var permissionId in idsToRemove)
            {
                foreach (var link in existingLinks.Where(rp => rp.PermissionId == permissionId && !rp.IsDeleted))
                {
                    link.IsDeleted = true;
                }
            }

            foreach (var permissionId in idsToAdd)
            {
                var activeLink = existingLinks.FirstOrDefault(rp => rp.PermissionId == permissionId && !rp.IsDeleted);
                if (activeLink is not null)
                    continue;

                var softDeletedLink = existingLinks.FirstOrDefault(rp => rp.PermissionId == permissionId && rp.IsDeleted);
                if (softDeletedLink is not null)
                {
                    softDeletedLink.IsDeleted = false;
                    continue;
                }

                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permissionId,
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
