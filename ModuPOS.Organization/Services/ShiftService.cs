using Microsoft.EntityFrameworkCore;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Organization.DTOs;
using ModuPOS.Organization.Interfaces;
using ModuPOS.Shared.Pagination;
using ModuPOS.Shared.Results;

namespace ModuPOS.Organization.Services;

public class ShiftService : IShiftService
{
    private readonly AppDbContext _context;

    public ShiftService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ShiftResponseDto>> GetShiftsAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) return PagedResult<ShiftResponseDto>.Failure("Request cannot be null");

        var query = _context.Shifts.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.ShiftId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new ShiftResponseDto
            {
                ShiftId = s.ShiftId,
                ShiftCode = s.ShiftCode,
                ShiftType = s.ShiftType,
                ShiftStartTime = s.ShiftStartTime,
                ShiftEndTime = s.ShiftEndTime,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
        return PagedResult<ShiftResponseDto>.Success(items, pagination);
    }

    public async Task<Result<ShiftResponseDto>> GetShiftByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var s = await _context.Shifts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ShiftId == id, cancellationToken);

        if (s is null)
            return Result<ShiftResponseDto>.Failure("Shift not found.");

        return Result<ShiftResponseDto>.Success(new ShiftResponseDto
        {
            ShiftId = s.ShiftId,
            ShiftCode = s.ShiftCode,
            ShiftType = s.ShiftType,
            ShiftStartTime = s.ShiftStartTime,
            ShiftEndTime = s.ShiftEndTime,
            CreatedAt = s.CreatedAt
        });
    }

    public async Task<Result<ShiftResponseDto>> CreateShiftAsync(CreateShiftRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ShiftCode))
            return Result<ShiftResponseDto>.Failure("Shift code is required.");

        var exists = await _context.Shifts.AnyAsync(s => s.ShiftCode == request.ShiftCode, cancellationToken);
        if (exists)
            return Result<ShiftResponseDto>.Failure($"Shift with code '{request.ShiftCode}' already exists.");

        var entity = new Shift
        {
            ShiftCode = request.ShiftCode,
            ShiftType = request.ShiftType,
            ShiftStartTime = request.ShiftStartTime,
            ShiftEndTime = request.ShiftEndTime,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.CreatedBy
        };

        _context.Shifts.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<ShiftResponseDto>.Success(new ShiftResponseDto
        {
            ShiftId = entity.ShiftId,
            ShiftCode = entity.ShiftCode,
            ShiftType = entity.ShiftType,
            ShiftStartTime = entity.ShiftStartTime,
            ShiftEndTime = entity.ShiftEndTime,
            CreatedAt = entity.CreatedAt
        });
    }

    public async Task<Result<ShiftResponseDto>> UpdateShiftAsync(int id, UpdateShiftRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Shifts.FirstOrDefaultAsync(s => s.ShiftId == id, cancellationToken);
        if (entity is null)
            return Result<ShiftResponseDto>.Failure("Shift not found.");

        entity.ShiftType = request.ShiftType;
        entity.ShiftStartTime = request.ShiftStartTime;
        entity.ShiftEndTime = request.ShiftEndTime;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = request.UpdatedBy;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<ShiftResponseDto>.Success(new ShiftResponseDto
        {
            ShiftId = entity.ShiftId,
            ShiftCode = entity.ShiftCode,
            ShiftType = entity.ShiftType,
            ShiftStartTime = entity.ShiftStartTime,
            ShiftEndTime = entity.ShiftEndTime,
            CreatedAt = entity.CreatedAt
        });
    }

    public async Task<Result<StaffShiftResponseDto>> AssignStaffShiftAsync(AssignStaffShiftRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.StaffShiftCode) || string.IsNullOrWhiteSpace(request.StaffCode) || string.IsNullOrWhiteSpace(request.ShiftCode))
            return Result<StaffShiftResponseDto>.Failure("StaffShiftCode, StaffCode, and ShiftCode are required.");

        var staffExists = await _context.Staff.AnyAsync(s => s.StaffCode == request.StaffCode && !s.DeleteFlag, cancellationToken);
        if (!staffExists)
            return Result<StaffShiftResponseDto>.Failure($"Staff with code '{request.StaffCode}' not found.");

        var shiftExists = await _context.Shifts.AnyAsync(s => s.ShiftCode == request.ShiftCode, cancellationToken);
        if (!shiftExists)
            return Result<StaffShiftResponseDto>.Failure($"Shift with code '{request.ShiftCode}' not found.");

        var assignmentExists = await _context.StaffShifts.AnyAsync(ss => ss.StaffShiftCode == request.StaffShiftCode, cancellationToken);
        if (assignmentExists)
            return Result<StaffShiftResponseDto>.Failure($"Staff shift assignment '{request.StaffShiftCode}' already exists.");

        var entity = new StaffShift
        {
            StaffShiftCode = request.StaffShiftCode,
            StaffCode = request.StaffCode,
            ShiftCode = request.ShiftCode
        };

        _context.StaffShifts.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<StaffShiftResponseDto>.Success(new StaffShiftResponseDto
        {
            StaffShiftId = entity.StaffShiftId,
            StaffShiftCode = entity.StaffShiftCode,
            StaffCode = entity.StaffCode,
            ShiftCode = entity.ShiftCode
        });
    }

    public async Task<PagedResult<StaffShiftResponseDto>> GetStaffShiftsAsync(string? shiftCode = null, PaginationRequest? request = null, CancellationToken cancellationToken = default)
    {
        if (request is null) return PagedResult<StaffShiftResponseDto>.Failure("Request cannot be null");

        var query = _context.StaffShifts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(shiftCode))
            query = query.Where(ss => ss.ShiftCode == shiftCode);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await (
            from ss in query
            join s in _context.Staff.Where(x => !x.DeleteFlag) on ss.StaffCode equals s.StaffCode into staffGroup
            from st in staffGroup.DefaultIfEmpty()
            orderby ss.StaffShiftId
            select new StaffShiftResponseDto
            {
                StaffShiftId = ss.StaffShiftId,
                StaffShiftCode = ss.StaffShiftCode,
                StaffCode = ss.StaffCode,
                ShiftCode = ss.ShiftCode,
                StaffName = st != null ? st.StaffName : null
            }
        )
        .Skip((request.PageNumber - 1) * request.PageSize)
        .Take(request.PageSize)
        .ToListAsync(cancellationToken);

        var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
        return PagedResult<StaffShiftResponseDto>.Success(items, pagination);
    }
}