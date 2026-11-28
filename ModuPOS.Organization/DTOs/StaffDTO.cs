namespace ModuPOS.Organization.DTOs;

public class CreateStaffRequest
{
    public string StaffCode { get; set; } = string.Empty;

    public string StaffName { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string BranchCode { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;
}

public class UpdateStaffRequest
{
    public string StaffName { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string BranchCode { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public string UpdatedBy { get; set; } = string.Empty;
}

public class StaffResponseDto
{
    public int StaffId { get; set; }

    public string StaffCode { get; set; } = string.Empty;

    public string StaffName { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string BranchCode { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class CreateStaffRoleRequest
{
    public string StaffRoleCode { get; set; } = string.Empty;

    public string StaffRoleName { get; set; } = string.Empty;

    public decimal? Salary { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
}

public class UpdateStaffRoleRequest
{
    public string StaffRoleName { get; set; } = string.Empty;

    public decimal? Salary { get; set; }

    public string UpdatedBy { get; set; } = string.Empty;
}

public class StaffRoleResponseDto
{
    public int StaffRoleId { get; set; }

    public string StaffRoleCode { get; set; } = string.Empty;

    public string StaffRoleName { get; set; } = string.Empty;

    public decimal? Salary { get; set; }

    public DateTime CreatedAt { get; set; }
}