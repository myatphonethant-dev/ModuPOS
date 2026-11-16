using System;
using System.Collections.Generic;

namespace ModuPOS.Database.AppDbContextModels;

public partial class Permission
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsDeleted { get; set; }
}
