using System;
using System.Collections.Generic;

namespace ModuPOS.Database.AppDbContextModels;

public partial class CustomerOrderItem
{
    public string Id { get; set; } = null!;

    public int CustomerOrderId { get; set; }

    public string ProductCode { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public decimal UnitPrice { get; set; }

    public decimal Quantity { get; set; }

    public decimal LineTotal { get; set; }
}
