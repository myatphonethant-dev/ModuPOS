namespace ModuPOS.Organization.DTOs;

public class CreateShiftRequest
{
    public string ShiftCode { get; set; } = string.Empty;

    public string ShiftType { get; set; } = string.Empty;

    public DateTime? ShiftStartTime { get; set; }

    public DateTime? ShiftEndTime { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
}

public class UpdateShiftRequest
{
    public string ShiftType { get; set; } = string.Empty;

    public DateTime? ShiftStartTime { get; set; }

    public DateTime? ShiftEndTime { get; set; }

    public string UpdatedBy { get; set; } = string.Empty;
}

public class ShiftResponseDto
{
    public int ShiftId { get; set; }

    public string ShiftCode { get; set; } = string.Empty;

    public string ShiftType { get; set; } = string.Empty;

    public DateTime? ShiftStartTime { get; set; }

    public DateTime? ShiftEndTime { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class AssignStaffShiftRequest
{
    public string StaffShiftCode { get; set; } = string.Empty;

    public string StaffCode { get; set; } = string.Empty;

    public string ShiftCode { get; set; } = string.Empty;
}

public class StaffShiftResponseDto
{
    public int StaffShiftId { get; set; }

    public string StaffShiftCode { get; set; } = string.Empty;

    public string StaffCode { get; set; } = string.Empty;

    public string ShiftCode { get; set; } = string.Empty;

    public string StaffName { get; set; } = string.Empty;
}