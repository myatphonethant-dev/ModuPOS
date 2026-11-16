using System;
using System.Collections.Generic;

namespace ModuPOS.Database.AppDbContextModels;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryCode { get; set; } = null!;

    public string CategoryName { get; set; } = null!;

    public bool DeleteFlag { get; set; }
}
