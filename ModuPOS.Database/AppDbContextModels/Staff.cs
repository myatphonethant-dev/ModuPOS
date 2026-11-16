using System;
using System.Collections.Generic;

namespace ModuPOS.Database.AppDbContextModels;

public partial class Staff
{
    public int StaffId { get; set; }

    public string StaffCode { get; set; } = null!;

    public string StaffName { get; set; } = null!;

    public DateTime? DateOfBirth { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? BranchCode { get; set; }

    public bool IsActive { get; set; }

    public bool DeleteFlag { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }
}
