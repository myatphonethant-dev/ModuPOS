namespace ModuPOS.Organization.DTOs;

public record CreateBranchRequest(
    string BranchCode,
    string BranchName,
    string? Address = null,
    string? CreatedBy = null);

public record UpdateBranchRequest(
    string BranchName,
    string? Address = null,
    string? UpdatedBy = null);

public record BranchResponseDto(
    int BranchId,
    string BranchCode,
    string BranchName,
    string? Address,
    DateTime CreatedAt);
