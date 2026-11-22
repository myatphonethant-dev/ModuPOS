namespace ModuPOS.Organization.DTOs;

public class CreateShiftRequest
{
    public string ShiftCode { get; set; } = string.Empty;
    public string? ShiftType { get; set; }
    public DateTime? ShiftStartTime { get; set; }
    public DateTime? ShiftEndTime { get; set; }
    public string? CreatedBy { get; set; }
}

public class UpdateShiftRequest
{
    public string? ShiftType { get; set; }
    public DateTime? ShiftStartTime { get; set; }
    public DateTime? ShiftEndTime { get; set; }
    public string? UpdatedBy { get; set; }
}

public sealed record ShiftResponseDto
{
    public int ShiftId { get; init; }
    public string ShiftCode { get; init; } = string.Empty;
    public string? ShiftType { get; init; }
    public DateTime? ShiftStartTime { get; init; }
    public DateTime? ShiftEndTime { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class AssignStaffShiftRequest
{
    public string StaffShiftCode { get; set; } = string.Empty;
    public string StaffCode { get; set; } = string.Empty;
    public string ShiftCode { get; set; } = string.Empty;
}

public sealed record StaffShiftResponseDto
{
    public int StaffShiftId { get; init; }
    public string StaffShiftCode { get; init; } = string.Empty;
    public string StaffCode { get; init; } = string.Empty;
    public string ShiftCode { get; init; } = string.Empty;
    public string? StaffName { get; init; }
}
