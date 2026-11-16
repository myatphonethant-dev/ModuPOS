using Microsoft.EntityFrameworkCore;
using ModuPOS.Auth.DTOs;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

namespace ModuPOS.Auth.Services.Permission;

/// <summary>
/// Defines business operations for managing functional access permissions.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Retrieves a paginated list of non-deleted permissions.
    /// </summary>
    /// <param name="request">The pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of permission DTOs.</returns>
    Task<PagedResult<PermissionDto>> GetPermissionsAsync(PaginationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new permission.
    /// </summary>
    /// <param name="name">The unique permission identifier name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created permission DTO if successful; otherwise failure.</returns>
    Task<Result<PermissionDto>> CreatePermissionAsync(string name, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides EF Core implementation for <see cref="IPermissionService"/>.
/// </summary>
public class PermissionService(AppDbContext context) : IPermissionService
{
    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<Result<PermissionDto>> CreatePermissionAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<PermissionDto>.Failure("Permission name is required.");

        var exists = await context.Permissions.AnyAsync(p => p.Name == name && !p.IsDeleted, cancellationToken);
        if (exists)
            return Result<PermissionDto>.Failure($"Permission '{name}' already exists.");

        var entity = new ModuPOS.Database.AppDbContextModels.Permission
        {
            Name = name,
            IsDeleted = false
        };

        context.Permissions.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Result<PermissionDto>.Success(new PermissionDto(entity.Id, entity.Name));
    }
}
