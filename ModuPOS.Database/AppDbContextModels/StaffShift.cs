using System;
using System.Collections.Generic;

namespace ModuPOS.Database.AppDbContextModels;

public partial class StaffShift
{
    public int StaffShiftId { get; set; }

    public string StaffShiftCode { get; set; } = null!;

    public string StaffCode { get; set; } = null!;

    public string ShiftCode { get; set; } = null!;
}
