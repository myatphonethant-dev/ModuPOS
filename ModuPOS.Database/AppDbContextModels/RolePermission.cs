using System;
using System.Collections.Generic;

namespace ModuPOS.Database.AppDbContextModels;

public partial class RolePermission
{
    public int Id { get; set; }

    public int RoleId { get; set; }

    public int PermissionId { get; set; }

    public bool IsDeleted { get; set; }
}
