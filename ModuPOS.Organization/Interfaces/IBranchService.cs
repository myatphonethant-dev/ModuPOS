namespace ModuPOS.Organization.Interfaces;

using ModuPOS.Organization.DTOs;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

public interface IBranchService
{
    Task<PagedResult<BranchResponseDto>> GetBranchesAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<Result<BranchResponseDto>> GetBranchByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<BranchResponseDto>> CreateBranchAsync(CreateBranchRequest request, CancellationToken cancellationToken = default);
    Task<Result<BranchResponseDto>> UpdateBranchAsync(int id, UpdateBranchRequest request, CancellationToken cancellationToken = default);
}
