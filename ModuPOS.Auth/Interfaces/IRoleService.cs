namespace ModuPOS.Auth.Interfaces;

using ModuPOS.Auth.DTOs;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

public interface IRoleService
{
    Task<PagedResult<RoleDto>> GetRolesAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<Result<RoleDto>> GetRoleByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<RoleDto>> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result<RoleDto>> UpdateRoleAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteRoleAsync(int id, CancellationToken cancellationToken = default);
}
