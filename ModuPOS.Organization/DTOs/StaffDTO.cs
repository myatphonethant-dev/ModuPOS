namespace ModuPOS.Organization.DTOs;

public class CreateStaffRequest
{
    public string StaffCode { get; set; } = string.Empty;
    public string StaffName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? BranchCode { get; set; }
    public string? CreatedBy { get; set; }
}

public class UpdateStaffRequest
{
    public string StaffName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? BranchCode { get; set; }
    public bool IsActive { get; set; } = true;
    public string? UpdatedBy { get; set; }
}

public sealed record StaffResponseDto
{
    public int StaffId { get; init; }
    public string StaffCode { get; init; } = string.Empty;
    public string StaffName { get; init; } = string.Empty;
    public DateTime? DateOfBirth { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? BranchCode { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class CreateStaffRoleRequest
{
    public string StaffRoleCode { get; set; } = string.Empty;
    public string StaffRoleName { get; set; } = string.Empty;
    public decimal? Salary { get; set; }
    public string? CreatedBy { get; set; }
}

public class UpdateStaffRoleRequest
{
    public string StaffRoleName { get; set; } = string.Empty;
    public decimal? Salary { get; set; }
    public string? UpdatedBy { get; set; }
}

public sealed record StaffRoleResponseDto
{
    public int StaffRoleId { get; init; }
    public string StaffRoleCode { get; init; } = string.Empty;
    public string StaffRoleName { get; init; } = string.Empty;
    public decimal? Salary { get; init; }
    public DateTime CreatedAt { get; init; }
}
