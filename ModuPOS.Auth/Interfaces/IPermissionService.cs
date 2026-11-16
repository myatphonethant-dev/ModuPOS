namespace ModuPOS.Auth.Interfaces;

using ModuPOS.Auth.DTOs;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

public interface IPermissionService
{
    Task<PagedResult<PermissionDto>> GetPermissionsAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<Result<PermissionDto>> CreatePermissionAsync(string name, CancellationToken cancellationToken = default);
}
