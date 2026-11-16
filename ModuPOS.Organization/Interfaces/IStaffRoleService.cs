namespace ModuPOS.Organization.Interfaces;

using ModuPOS.Organization.DTOs;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

public interface IStaffRoleService
{
    Task<PagedResult<StaffRoleResponseDto>> GetStaffRolesAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<Result<StaffRoleResponseDto>> GetStaffRoleByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<StaffRoleResponseDto>> CreateStaffRoleAsync(CreateStaffRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result<StaffRoleResponseDto>> UpdateStaffRoleAsync(int id, UpdateStaffRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteStaffRoleAsync(int id, CancellationToken cancellationToken = default);
}
