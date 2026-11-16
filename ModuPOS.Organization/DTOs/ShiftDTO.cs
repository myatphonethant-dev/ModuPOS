namespace ModuPOS.Organization.DTOs;

public record CreateShiftRequest(
    string ShiftCode,
    string? ShiftType = null,
    DateTime? ShiftStartTime = null,
    DateTime? ShiftEndTime = null,
    string? CreatedBy = null);

public record UpdateShiftRequest(
    string? ShiftType = null,
    DateTime? ShiftStartTime = null,
    DateTime? ShiftEndTime = null,
    string? UpdatedBy = null);

public record ShiftResponseDto(
    int ShiftId,
    string ShiftCode,
    string? ShiftType,
    DateTime? ShiftStartTime,
    DateTime? ShiftEndTime,
    DateTime CreatedAt);

public record AssignStaffShiftRequest(
    string StaffShiftCode,
    string StaffCode,
    string ShiftCode);

public record StaffShiftResponseDto(
    int StaffShiftId,
    string StaffShiftCode,
    string StaffCode,
    string ShiftCode,
    string? StaffName = null);
