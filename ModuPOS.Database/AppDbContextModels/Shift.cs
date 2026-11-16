using System;
using System.Collections.Generic;

namespace ModuPOS.Database.AppDbContextModels;

public partial class Shift
{
    public int ShiftId { get; set; }

    public string ShiftCode { get; set; } = null!;

    public string? ShiftType { get; set; }

    public DateTime? ShiftStartTime { get; set; }

    public DateTime? ShiftEndTime { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }
}
