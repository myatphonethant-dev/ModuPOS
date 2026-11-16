namespace ModuPOS.Auth.Services;

using Microsoft.EntityFrameworkCore;
using ModuPOS.Auth.DTOs;
using ModuPOS.Auth.Interfaces;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

public class PermissionService(AppDbContext context) : IPermissionService
{
    public async Task<PagedResult<PermissionDto>> GetPermissionsAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var query = context.Permissions
            .AsNoTracking()
            .Where(p => !p.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var list = await query
            .OrderBy(p => p.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PermissionDto(p.Id, p.Name))
            .ToListAsync(cancellationToken);

        var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
        return PagedResult<PermissionDto>.Success(list, pagination);
    }

    public async Task<Result<PermissionDto>> CreatePermissionAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<PermissionDto>.Failure("Permission name is required.");

        var exists = await context.Permissions.AnyAsync(p => p.Name == name && !p.IsDeleted, cancellationToken);
        if (exists)
            return Result<PermissionDto>.Failure($"Permission '{name}' already exists.");

        var entity = new Permission
        {
            Name = name,
            IsDeleted = false
        };

        context.Permissions.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Result<PermissionDto>.Success(new PermissionDto(entity.Id, entity.Name));
    }
}
