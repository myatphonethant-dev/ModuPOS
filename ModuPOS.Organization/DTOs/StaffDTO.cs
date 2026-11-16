namespace ModuPOS.Organization.DTOs;

public record CreateStaffRequest(
    string StaffCode,
    string StaffName,
    DateTime? DateOfBirth = null,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    string? BranchCode = null,
    string? CreatedBy = null);

public record UpdateStaffRequest(
    string StaffName,
    DateTime? DateOfBirth = null,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    string? BranchCode = null,
    bool IsActive = true,
    string? UpdatedBy = null);

public record StaffResponseDto(
    int StaffId,
    string StaffCode,
    string StaffName,
    DateTime? DateOfBirth,
    string? Phone,
    string? Email,
    string? Address,
    string? BranchCode,
    bool IsActive,
    DateTime CreatedAt);

public record CreateStaffRoleRequest(
    string StaffRoleCode,
    string StaffRoleName,
    decimal? Salary = null,
    string? CreatedBy = null);

public record UpdateStaffRoleRequest(
    string StaffRoleName,
    decimal? Salary = null,
    string? UpdatedBy = null);

public record StaffRoleResponseDto(
    int StaffRoleId,
    string StaffRoleCode,
    string StaffRoleName,
    decimal? Salary,
    DateTime CreatedAt);
