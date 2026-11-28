namespace ModuPOS.Organization.DTOs;

public class CreateBranchRequest
{
    public string BranchCode { get; set; } = string.Empty;

    public string BranchName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;
}

public class UpdateBranchRequest
{
    public string BranchName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string UpdatedBy { get; set; } = string.Empty;
}

public class BranchResponseDto
{
    public int BranchId { get; set; }

    public string BranchCode { get; set; } = string.Empty;

    public string BranchName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}