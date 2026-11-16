using Microsoft.EntityFrameworkCore;
using ModuPOS.Auth.DTOs;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Shared.Results;

namespace ModuPOS.Auth.Services.Permission;

public interface IPermissionService
{
    Task<Result<List<PermissionDto>>> GetPermissionsAsync(CancellationToken cancellationToken = default);
    Task<Result<PermissionDto>> CreatePermissionAsync(string name, CancellationToken cancellationToken = default);
}

public class PermissionService(AppDbContext context) : IPermissionService
{
    public async Task<Result<List<PermissionDto>>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var list = await context.Permissions
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Select(p => new PermissionDto(p.Id, p.Name))
            .ToListAsync(cancellationToken);

        return Result.Success(list);
    }

    public async Task<Result<PermissionDto>> CreatePermissionAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<PermissionDto>("Permission name is required.");

        var exists = await context.Permissions.AnyAsync(p => p.Name == name && !p.IsDeleted, cancellationToken);
        if (exists)
            return Result.Failure<PermissionDto>($"Permission '{name}' already exists.");

        var entity = new ModuPOS.Database.AppDbContextModels.Permission
        {
            Name = name,
            IsDeleted = false
        };

        context.Permissions.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new PermissionDto(entity.Id, entity.Name));
    }
}
