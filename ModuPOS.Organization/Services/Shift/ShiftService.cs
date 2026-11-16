using Microsoft.EntityFrameworkCore;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Organization.DTOs;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

namespace ModuPOS.Organization.Services.Shift;

/// <summary>
/// Defines business operations for managing work shifts and staff shift assignments.
/// </summary>
public interface IShiftService
{
    /// <summary>
    /// Retrieves a paginated list of operational shifts.
    /// </summary>
    /// <param name="request">The pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of shift response DTOs.</returns>
    Task<PagedResult<ShiftResponseDto>> GetShiftsAsync(PaginationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single shift by its identifier.
    /// </summary>
    /// <param name="id">The shift primary key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The shift response DTO if found; otherwise failure.</returns>
    Task<Result<ShiftResponseDto>> GetShiftByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new operational shift.
    /// </summary>
    /// <param name="request">The shift creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created shift response DTO if successful; otherwise failure.</returns>
    Task<Result<ShiftResponseDto>> CreateShiftAsync(CreateShiftRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing operational shift.
    /// </summary>
    /// <param name="id">The shift primary key.</param>
    /// <param name="request">The shift update details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated shift response DTO if successful; otherwise failure.</returns>
    Task<Result<ShiftResponseDto>> UpdateShiftAsync(int id, UpdateShiftRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a staff member to an operational work shift.
    /// </summary>
    /// <param name="request">The staff shift assignment request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The assignment response DTO if successful; otherwise failure.</returns>
    Task<Result<StaffShiftResponseDto>> AssignStaffShiftAsync(AssignStaffShiftRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of staff shift assignments with an optional shift filter.
    /// </summary>
    /// <param name="shiftCode">Optional shift code filter.</param>
    /// <param name="request">The pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of staff shift assignment DTOs.</returns>
    Task<PagedResult<StaffShiftResponseDto>> GetStaffShiftsAsync(string? shiftCode = null, PaginationRequest? request = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides EF Core implementation for <see cref="IShiftService"/>.
/// </summary>
public class ShiftService(AppDbContext context) : IShiftService
{
    /// <inheritdoc />
    public async Task<PagedResult<ShiftResponseDto>> GetShiftsAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var query = context.Shifts.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.ShiftId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new ShiftResponseDto(s.ShiftId, s.ShiftCode, s.ShiftType, s.ShiftStartTime, s.ShiftEndTime, s.CreatedAt))
            .ToListAsync(cancellationToken);

        var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
        return PagedResult<ShiftResponseDto>.Success(items, pagination);
    }

    /// <inheritdoc />
    public async Task<Result<ShiftResponseDto>> GetShiftByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var s = await context.Shifts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ShiftId == id, cancellationToken);

        if (s is null)
            return Result<ShiftResponseDto>.Failure("Shift not found.");

        return Result<ShiftResponseDto>.Success(new ShiftResponseDto(s.ShiftId, s.ShiftCode, s.ShiftType, s.ShiftStartTime, s.ShiftEndTime, s.CreatedAt));
    }

    /// <inheritdoc />
    public async Task<Result<ShiftResponseDto>> CreateShiftAsync(CreateShiftRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ShiftCode))
            return Result<ShiftResponseDto>.Failure("Shift code is required.");

        var exists = await context.Shifts.AnyAsync(s => s.ShiftCode == request.ShiftCode, cancellationToken);
        if (exists)
            return Result<ShiftResponseDto>.Failure($"Shift with code '{request.ShiftCode}' already exists.");

        var entity = new ModuPOS.Database.AppDbContextModels.Shift
        {
            ShiftCode = request.ShiftCode,
            ShiftType = request.ShiftType,
            ShiftStartTime = request.ShiftStartTime,
            ShiftEndTime = request.ShiftEndTime,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.CreatedBy
        };

        context.Shifts.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Result<ShiftResponseDto>.Success(new ShiftResponseDto(entity.ShiftId, entity.ShiftCode, entity.ShiftType, entity.ShiftStartTime, entity.ShiftEndTime, entity.CreatedAt));
    }

    /// <inheritdoc />
    public async Task<Result<ShiftResponseDto>> UpdateShiftAsync(int id, UpdateShiftRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await context.Shifts.FirstOrDefaultAsync(s => s.ShiftId == id, cancellationToken);
        if (entity is null)
            return Result<ShiftResponseDto>.Failure("Shift not found.");

        entity.ShiftType = request.ShiftType;
        entity.ShiftStartTime = request.ShiftStartTime;
        entity.ShiftEndTime = request.ShiftEndTime;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = request.UpdatedBy;

        await context.SaveChangesAsync(cancellationToken);

        return Result<ShiftResponseDto>.Success(new ShiftResponseDto(entity.ShiftId, entity.ShiftCode, entity.ShiftType, entity.ShiftStartTime, entity.ShiftEndTime, entity.CreatedAt));
    }

    /// <inheritdoc />
    public async Task<Result<StaffShiftResponseDto>> AssignStaffShiftAsync(AssignStaffShiftRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.StaffShiftCode) || string.IsNullOrWhiteSpace(request.StaffCode) || string.IsNullOrWhiteSpace(request.ShiftCode))
            return Result<StaffShiftResponseDto>.Failure("StaffShiftCode, StaffCode, and ShiftCode are required.");

        var staffExists = await context.Staff.AnyAsync(s => s.StaffCode == request.StaffCode && !s.DeleteFlag, cancellationToken);
        if (!staffExists)
            return Result<StaffShiftResponseDto>.Failure($"Staff with code '{request.StaffCode}' not found.");

        var shiftExists = await context.Shifts.AnyAsync(s => s.ShiftCode == request.ShiftCode, cancellationToken);
        if (!shiftExists)
            return Result<StaffShiftResponseDto>.Failure($"Shift with code '{request.ShiftCode}' not found.");

        var assignmentExists = await context.StaffShifts.AnyAsync(ss => ss.StaffShiftCode == request.StaffShiftCode, cancellationToken);
        if (assignmentExists)
            return Result<StaffShiftResponseDto>.Failure($"Staff shift assignment '{request.StaffShiftCode}' already exists.");

        var entity = new ModuPOS.Database.AppDbContextModels.StaffShift
        {
            StaffShiftCode = request.StaffShiftCode,
            StaffCode = request.StaffCode,
            ShiftCode = request.ShiftCode
        };

        context.StaffShifts.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Result<StaffShiftResponseDto>.Success(new StaffShiftResponseDto(entity.StaffShiftId, entity.StaffShiftCode, entity.StaffCode, entity.ShiftCode));
    }

    /// <inheritdoc />
    public async Task<PagedResult<StaffShiftResponseDto>> GetStaffShiftsAsync(string? shiftCode = null, PaginationRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new PaginationRequest();

        var query = context.StaffShifts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(shiftCode))
            query = query.Where(ss => ss.ShiftCode == shiftCode);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await (
            from ss in query
            join s in context.Staff.Where(x => !x.DeleteFlag) on ss.StaffCode equals s.StaffCode into staffGroup
            from st in staffGroup.DefaultIfEmpty()
            orderby ss.StaffShiftId
            select new StaffShiftResponseDto(ss.StaffShiftId, ss.StaffShiftCode, ss.StaffCode, ss.ShiftCode, st != null ? st.StaffName : null)
        )
        .Skip((request.PageNumber - 1) * request.PageSize)
        .Take(request.PageSize)
        .ToListAsync(cancellationToken);

        var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
        return PagedResult<StaffShiftResponseDto>.Success(items, pagination);
    }
}
