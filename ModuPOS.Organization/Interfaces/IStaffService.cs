namespace ModuPOS.Organization.Interfaces;

using ModuPOS.Organization.DTOs;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

public interface IStaffService
{
    Task<PagedResult<StaffResponseDto>> GetStaffListAsync(string? branchCode = null, PaginationRequest? request = null, CancellationToken cancellationToken = default);
    Task<Result<StaffResponseDto>> GetStaffByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<StaffResponseDto>> CreateStaffAsync(CreateStaffRequest request, CancellationToken cancellationToken = default);
    Task<Result<StaffResponseDto>> UpdateStaffAsync(int id, UpdateStaffRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteStaffAsync(int id, CancellationToken cancellationToken = default);
}
