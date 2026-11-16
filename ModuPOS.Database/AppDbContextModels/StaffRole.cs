using System;
using System.Collections.Generic;

namespace ModuPOS.Database.AppDbContextModels;

public partial class StaffRole
{
    public int StaffRoleId { get; set; }

    public string StaffRoleCode { get; set; } = null!;

    public string StaffRoleName { get; set; } = null!;

    public decimal? Salary { get; set; }

    public bool DeleteFlag { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }
}
