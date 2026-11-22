namespace ModuPOS.Organization.DTOs;

public class CreateBranchRequest
{
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? CreatedBy { get; set; }
}

public class UpdateBranchRequest
{
    public string BranchName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? UpdatedBy { get; set; }
}

public sealed record BranchResponseDto
{
    public int BranchId { get; init; }
    public string BranchCode { get; init; } = string.Empty;
    public string BranchName { get; init; } = string.Empty;
    public string? Address { get; init; }
    public DateTime CreatedAt { get; init; }
}
