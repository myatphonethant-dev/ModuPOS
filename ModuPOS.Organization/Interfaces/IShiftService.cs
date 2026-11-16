namespace ModuPOS.Organization.Interfaces;

using ModuPOS.Organization.DTOs;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

public interface IShiftService
{
    Task<PagedResult<ShiftResponseDto>> GetShiftsAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<Result<ShiftResponseDto>> GetShiftByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<ShiftResponseDto>> CreateShiftAsync(CreateShiftRequest request, CancellationToken cancellationToken = default);
    Task<Result<ShiftResponseDto>> UpdateShiftAsync(int id, UpdateShiftRequest request, CancellationToken cancellationToken = default);
    Task<Result<StaffShiftResponseDto>> AssignStaffShiftAsync(AssignStaffShiftRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<StaffShiftResponseDto>> GetStaffShiftsAsync(string? shiftCode = null, PaginationRequest? request = null, CancellationToken cancellationToken = default);
}
