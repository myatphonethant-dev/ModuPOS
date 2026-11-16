using Microsoft.EntityFrameworkCore;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Organization.DTOs;
using ModuPOS.Shared.Results;

namespace ModuPOS.Organization.Services.Shift;

public interface IShiftService
{
    Task<Result<List<ShiftResponseDto>>> GetShiftsAsync(CancellationToken cancellationToken = default);
    Task<Result<ShiftResponseDto>> CreateShiftAsync(CreateShiftRequest request, CancellationToken cancellationToken = default);
    Task<Result<ShiftResponseDto>> UpdateShiftAsync(int id, UpdateShiftRequest request, CancellationToken cancellationToken = default);
    Task<Result<StaffShiftResponseDto>> AssignStaffShiftAsync(AssignStaffShiftRequest request, CancellationToken cancellationToken = default);
    Task<Result<List<StaffShiftResponseDto>>> GetStaffShiftsAsync(string? shiftCode = null, CancellationToken cancellationToken = default);
}

public class ShiftService(AppDbContext context) : IShiftService
{
    public async Task<Result<List<ShiftResponseDto>>> GetShiftsAsync(CancellationToken cancellationToken = default)
    {
        var shifts = await context.Shifts
            .AsNoTracking()
            .Select(s => new ShiftResponseDto(s.ShiftId, s.ShiftCode, s.ShiftType, s.ShiftStartTime, s.ShiftEndTime, s.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(shifts);
    }

    public async Task<Result<ShiftResponseDto>> CreateShiftAsync(CreateShiftRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ShiftCode))
            return Result.Failure<ShiftResponseDto>("Shift code is required.");

        var exists = await context.Shifts.AnyAsync(s => s.ShiftCode == request.ShiftCode, cancellationToken);
        if (exists)
            return Result.Failure<ShiftResponseDto>($"Shift with code '{request.ShiftCode}' already exists.");

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

        return Result.Success(new ShiftResponseDto(entity.ShiftId, entity.ShiftCode, entity.ShiftType, entity.ShiftStartTime, entity.ShiftEndTime, entity.CreatedAt));
    }

    public async Task<Result<ShiftResponseDto>> UpdateShiftAsync(int id, UpdateShiftRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await context.Shifts.FirstOrDefaultAsync(s => s.ShiftId == id, cancellationToken);
        if (entity is null)
            return Result.Failure<ShiftResponseDto>("Shift not found.");

        entity.ShiftType = request.ShiftType;
        entity.ShiftStartTime = request.ShiftStartTime;
        entity.ShiftEndTime = request.ShiftEndTime;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = request.UpdatedBy;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new ShiftResponseDto(entity.ShiftId, entity.ShiftCode, entity.ShiftType, entity.ShiftStartTime, entity.ShiftEndTime, entity.CreatedAt));
    }

    public async Task<Result<StaffShiftResponseDto>> AssignStaffShiftAsync(AssignStaffShiftRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.StaffShiftCode) || string.IsNullOrWhiteSpace(request.StaffCode) || string.IsNullOrWhiteSpace(request.ShiftCode))
            return Result.Failure<StaffShiftResponseDto>("StaffShiftCode, StaffCode, and ShiftCode are required.");

        var staffExists = await context.Staff.AnyAsync(s => s.StaffCode == request.StaffCode && !s.DeleteFlag, cancellationToken);
        if (!staffExists)
            return Result.Failure<StaffShiftResponseDto>($"Staff with code '{request.StaffCode}' not found.");

        var shiftExists = await context.Shifts.AnyAsync(s => s.ShiftCode == request.ShiftCode, cancellationToken);
        if (!shiftExists)
            return Result.Failure<StaffShiftResponseDto>($"Shift with code '{request.ShiftCode}' not found.");

        var entity = new ModuPOS.Database.AppDbContextModels.StaffShift
        {
            StaffShiftCode = request.StaffShiftCode,
            StaffCode = request.StaffCode,
            ShiftCode = request.ShiftCode
        };

        context.StaffShifts.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new StaffShiftResponseDto(entity.StaffShiftId, entity.StaffShiftCode, entity.StaffCode, entity.ShiftCode));
    }

    public async Task<Result<List<StaffShiftResponseDto>>> GetStaffShiftsAsync(string? shiftCode = null, CancellationToken cancellationToken = default)
    {
        var query = context.StaffShifts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(shiftCode))
            query = query.Where(ss => ss.ShiftCode == shiftCode);

        var list = await (
            from ss in query
            join s in context.Staff.Where(x => !x.DeleteFlag) on ss.StaffCode equals s.StaffCode into staffGroup
            from st in staffGroup.DefaultIfEmpty()
            select new StaffShiftResponseDto(ss.StaffShiftId, ss.StaffShiftCode, ss.StaffCode, ss.ShiftCode, st != null ? st.StaffName : null)
        ).ToListAsync(cancellationToken);

        return Result.Success(list);
    }
}
